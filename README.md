# Mycopunk ภาษาไทย

mod แปลเกม [Mycopunk](https://store.steampowered.com/app/3247750/) เป็นภาษาไทย (BepInEx plugin)

- แปลเมนู ตั้งค่า HUD เป้าหมายภารกิจ อาวุธ และอัปเกรดแล้ว บทพูดกับเนื้อเรื่องบางส่วนยังเป็นภาษาอังกฤษ
- ชื่อของต่างๆ (อัปเกรด ไอเทม ภารกิจ สถานที่ ศัตรู) และปุ่มกดคงเป็นภาษาอังกฤษ
- เป็น mod ฝั่งผู้เล่น เข้าห้องคนอื่นได้ตามปกติ คนในห้องไม่ต้องลง

## ติดตั้ง

1. ไปที่หน้า [Releases](../../releases) แล้วโหลด `MycopunkThai-vX.Y.Z.zip`
2. แตก zip ออกมา (คลิกขวา > Extract All)
3. ดับเบิลคลิก `Install.bat`
   - ถ้า Windows ขึ้น "Windows protected your PC" ให้กด **More info** แล้ว **Run anyway**
4. เปิดเกม ในเมนูตั้งค่าเลือกภาษา **English** ไว้ (mod แทนที่ภาษาอังกฤษด้วยภาษาไทย)

ตัวติดตั้งจะหาเกมใน Steam เอง ถ้ายังไม่มี BepInEx จะโหลด [BepInExPack_Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/) ให้

อัปเดตเวอร์ชันใหม่: โหลด zip ใหม่แล้วกด `Install.bat` อีกครั้ง

## ถอนการติดตั้ง

ดับเบิลคลิก `Uninstall.bat` จะลบเฉพาะ mod ภาษาไทย BepInEx ยังอยู่

ปิดภาษาไทยชั่วคราว: แก้ `Enabled = false` ใน `Mycopunk\BepInEx\config\th.mycopunk.thailanguage.cfg`

## ช่วยแปล

คำแปลอยู่ใน [`thai.csv`](thai.csv) คอลัมน์ `id,english,thai` แก้คอลัมน์ `thai` แล้วส่ง PR มาได้ แถวที่ `thai` ว่างจะแสดงภาษาอังกฤษ

ดูแนวทางแปลใน [`TRANSLATING.md`](TRANSLATING.md) ห้ามแก้ `<>`, tag `<...>` และ `[keyword]` ให้ผิดรูป

ทดสอบคำแปล: วาง `thai.csv` ทับใน `Mycopunk\BepInEx\plugins\ThaiLanguage\` แล้วเปิดเกมใหม่
ถ้าตั้ง `WriteFiles = true` ใน config จะได้ `thai_untranslated.csv` (แถวที่ยังไม่แปล) ในโฟลเดอร์นั้น

## Build

ต้องมี .NET SDK และลง Mycopunk ไว้ (build อ้าง DLL ของเกม)

```powershell
.\release.ps1                      # ได้ dist\MycopunkThai-vX.Y.Z.zip
.\release.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Mycopunk"
```

---
mod นี้ทำโดยแฟนเกม ไม่เกี่ยวข้องกับผู้พัฒนา Mycopunk
