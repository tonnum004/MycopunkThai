using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using CSVFile;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThaiLanguage
{
    [BepInPlugin(Guid, "Thai Language", Version)]
    // Only changes local text and fonts, so it's client-side and not sandbox: hosts without "Allow Modded Joiners" still accept us.
    [MycoMod("Thai Language", ModFlags.IsClientSide)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "th.mycopunk.thailanguage";
        public const string Version = "0.2.0";

        // Game's CSV: col 0 = id, col 1 = metadata, col 2 = English (we overwrite it with Thai).
        const int EnglishColumn = 2;

        static Plugin instance;
        static Dictionary<string, string> thai = new Dictionary<string, string>();
        static string thaiPath;
        static string untranslatedPath;
        static bool writeTranslatorFiles;

        // The game's CSV reader only splits rows on LineSeparator (\r\n by default); our own files use \n.
        static readonly CSVSettings LfSettings = new CSVSettings { LineSeparator = "\n" };

        ConfigEntry<bool> thaiEnabled;
        ConfigEntry<string> fontFamilies;
        Harmony harmony;

        void Awake()
        {
            instance = this;
            thaiEnabled = Config.Bind("General", "Enabled", true, "Show the game in Thai. Restart the game after changing.");
            fontFamilies = Config.Bind("Font", "Families", "Leelawadee UI,Tahoma",
                "Windows font families to use for Thai, tried in order.");
            writeTranslatorFiles = Config.Bind("Translators", "WriteFiles", false,
                "Write TextBlocks_original.csv and thai_untranslated.csv next to the plugin on every launch.").Value;
            if (!thaiEnabled.Value)
                return;

            string dir = Path.GetDirectoryName(Info.Location);
            thaiPath = Path.Combine(dir, "thai.csv");
            untranslatedPath = Path.Combine(dir, "thai_untranslated.csv");
            LoadThai();

            harmony = Harmony.CreateAndPatchAll(typeof(Plugin), Guid);

            // The game may have loaded its text before BepInEx started us; load it again so our patch applies.
            if (TextBlocks.strings.Count > 0)
                ReloadTextBlocks();
        }

        void Start()
        {
            // If this says True, another installed mod is sandbox and hosts may still reject joining.
            Logger.LogInfo($"Game treats installed mods as sandbox: {ModManager.IsSandbox()}");
            if (thaiEnabled.Value)
                AddThaiFont();
        }

        void OnDestroy() => harmony?.UnpatchSelf();

        [HarmonyPostfix, HarmonyPatch(typeof(TextAsset), nameof(TextAsset.text), MethodType.Getter)]
        static void TextAssetTextPostfix(TextAsset __instance, ref string __result)
        {
            if (__result == null || __instance.name != "TextBlocks")
                return;
            try
            {
                __result = ApplyThai(__result);
            }
            catch (Exception e)
            {
                // Fall back to the original English text rather than breaking the game.
                instance.Logger.LogError($"Could not apply Thai text: {e}");
            }
        }

        // Key prompts use the OS keyboard layout's key name, so a Thai layout turns "F" into "ด". Keep keys in English.
        [HarmonyPostfix, HarmonyPatch(typeof(global::PlayerInput), "GetBindingPathName")]
        static void BindingPathNamePostfix(string path, ref string __result)
        {
            if (__result != null && __result.Any(c => c >= '฀' && c <= '๿'))
                __result = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        static string ApplyThai(string csv)
        {
            string[][] rows;
            using (var reader = CSVReader.FromString(csv))
                rows = reader.Lines().ToArray();

            if (!File.Exists(thaiPath))
                WriteTemplate(thaiPath, rows);
            // Raw game CSV (with metadata column and every language) for translators to look things up in.
            if (writeTranslatorFiles)
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(thaiPath), "TextBlocks_original.csv"), csv, new UTF8Encoding(true));

            int translated = 0;
            var missing = new List<string[]>();
            foreach (string[] row in rows)
            {
                if (row.Length <= EnglishColumn || string.IsNullOrEmpty(row[0]))
                    continue;
                if (thai.TryGetValue(row[0], out string text) && text.Length > 0)
                {
                    row[EnglishColumn] = text;
                    translated++;
                }
                else
                {
                    missing.Add(row);
                }
            }
            if (writeTranslatorFiles)
                WriteTemplate(untranslatedPath, missing);
            instance.Logger.LogInfo($"Thai: {translated} lines translated, {missing.Count} still English");

            var sb = new StringBuilder(csv.Length * 2);
            foreach (string[] row in rows)
                sb.Append(row.ToCSVString()).Append(CSVSettings.CSV.LineSeparator);
            string result = sb.ToString();

            // Self-check: the game must parse our output into the same rows, or we hand back the original.
            int parsed;
            using (var reader = CSVReader.FromString(result))
                parsed = reader.Lines().Count();
            if (parsed != rows.Length)
                throw new InvalidDataException($"rewritten CSV has {parsed} rows, expected {rows.Length}");
            return result;
        }

        // Writes "id,english,thai" rows for translators; the thai column is left empty.
        static void WriteTemplate(string path, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.Append(new[] { "id", "english", "thai" }.ToCSVString(LfSettings)).Append('\n');
            foreach (string[] row in rows)
            {
                if (row.Length <= EnglishColumn || string.IsNullOrEmpty(row[0]))
                    continue;
                sb.Append(new[] { row[0], row[EnglishColumn], "" }.ToCSVString(LfSettings)).Append('\n');
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        void LoadThai()
        {
            thai.Clear();
            if (!File.Exists(thaiPath))
            {
                Logger.LogInfo($"No thai.csv yet; a template will be written to {thaiPath}");
                return;
            }
            // Editors may save CRLF or LF; normalize to LF.
            string text = File.ReadAllText(thaiPath).Replace("\r\n", "\n");
            using (var reader = CSVReader.FromString(text, LfSettings))
            {
                foreach (string[] row in reader.Lines().Skip(1))
                {
                    if (row.Length < 3 || string.IsNullOrEmpty(row[0]))
                        continue;
                    if (thai.ContainsKey(row[0]))
                        Logger.LogWarning($"thai.csv: duplicate id '{row[0]}', keeping the first one");
                    else
                        thai.Add(row[0], row[2]);
                }
            }
            Logger.LogInfo($"Loaded {thai.Count} rows from thai.csv");
        }

        void ReloadTextBlocks()
        {
            Logger.LogInfo("TextBlocks already loaded; reloading with Thai");
            // Initialize() clears `strings` but not `keyIDs`, so clear it or ids shift.
            ((List<string>)AccessTools.Field(typeof(TextBlocks), "keyIDs").GetValue(null)).Clear();
            TextBlocks.roachardChatResponses.Clear();
            AccessTools.Method(typeof(TextBlocks), "Initialize").Invoke(null, null);
        }

        void AddThaiFont()
        {
            TMP_FontAsset font = null;
            foreach (string family in fontFamilies.Value.Split(','))
            {
                font = TMP_FontAsset.CreateFontAsset(family.Trim(), "Regular");
                if (font != null)
                {
                    Logger.LogInfo($"Thai font: {family.Trim()}");
                    break;
                }
            }
            if (font == null)
            {
                Logger.LogError($"None of the fonts '{fontFamilies.Value}' were found; Thai will show as boxes");
                return;
            }
            font.name = "ThaiLanguage Fallback";

            // Global fallback: any game font missing a Thai glyph looks here.
            if (TMP_Settings.fallbackFontAssets == null)
                TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets.Add(font);
        }
    }
}
