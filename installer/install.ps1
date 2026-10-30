# Installs BepInEx (if missing) and the Thai Language plugin into Mycopunk.
param([switch]$Uninstall, [string]$GameDir)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$BepInExUrl = 'https://thunderstore.io/package/download/BepInEx/BepInExPack_Mycopunk/5.4.2403/'
$PluginSource = Join-Path $PSScriptRoot 'ThaiLanguage'

function Find-Game {
    $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    $libraries = @()
    if ($steam) {
        $libraries += $steam
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $libraries += Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' |
                ForEach-Object { $_.Matches[0].Groups[1].Value -replace '\\\\', '\' }
        }
    }
    $libraries += "${env:ProgramFiles(x86)}\Steam"
    foreach ($lib in $libraries | Select-Object -Unique) {
        $dir = Join-Path $lib 'steamapps\common\Mycopunk'
        if (Test-Path (Join-Path $dir 'Mycopunk.exe')) { return $dir }
    }

    Write-Host 'หาโฟลเดอร์เกมไม่เจอ เลือกโฟลเดอร์ที่มี Mycopunk.exe เอง'
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = 'เลือกโฟลเดอร์ Mycopunk (ที่มี Mycopunk.exe)'
    if ($dialog.ShowDialog() -eq 'OK' -and (Test-Path (Join-Path $dialog.SelectedPath 'Mycopunk.exe'))) {
        return $dialog.SelectedPath
    }
    throw 'ไม่พบ Mycopunk.exe'
}

try {
    $game = if ($GameDir) { $GameDir } else { Find-Game }
    Write-Host "เจอเกมที่: $game"

    while (Get-Process -Name Mycopunk -ErrorAction SilentlyContinue) {
        Read-Host 'ปิดเกม Mycopunk ก่อน แล้วกด Enter'
    }

    $pluginDir = Join-Path $game 'BepInEx\plugins\ThaiLanguage'

    if ($Uninstall) {
        Remove-Item $pluginDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $game 'BepInEx\config\th.mycopunk.thailanguage.cfg') -Force -ErrorAction SilentlyContinue
        Write-Host 'ลบ Thai Language แล้ว (BepInEx ยังอยู่ ถ้าไม่มี mod อื่นจะลบโฟลเดอร์ BepInEx, winhttp.dll, doorstop_config.ini เองก็ได้)'
        exit 0
    }

    if (-not (Test-Path (Join-Path $game 'BepInEx\core\BepInEx.Preloader.dll'))) {
        Write-Host 'กำลังดาวน์โหลด BepInEx...'
        $tmp = Join-Path $env:TEMP "MycopunkThai_$([guid]::NewGuid())"
        New-Item -ItemType Directory $tmp | Out-Null
        try {
            $zip = Join-Path $tmp 'bepinex.zip'
            Invoke-WebRequest $BepInExUrl -OutFile $zip -UseBasicParsing
            Expand-Archive $zip (Join-Path $tmp 'x')
            Copy-Item (Join-Path $tmp 'x\BepInExPack_Mycopunk\*') $game -Recurse -Force
        } finally {
            Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
        }
        Write-Host 'ติดตั้ง BepInEx แล้ว'
    } else {
        Write-Host 'มี BepInEx อยู่แล้ว ข้าม'
    }

    New-Item -ItemType Directory $pluginDir -Force | Out-Null
    Copy-Item (Join-Path $PluginSource '*') $pluginDir -Recurse -Force
    Write-Host ''
    Write-Host 'ติดตั้งภาษาไทยเสร็จแล้ว เปิดเกมได้เลย'
    Write-Host 'ในเมนูตั้งค่าให้เลือกภาษา English ไว้ (mod แทนที่ภาษาอังกฤษด้วยภาษาไทย)'
} catch {
    Write-Host ''
    Write-Host "ติดตั้งไม่สำเร็จ: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
