# Google Play release kit — Desi Merge

| Item | File | Play Console field |
|---|---|---|
| App ID | `com.desimerge.game` (set in `BuildCommands.ApplicationId`, applied to Player Settings) | permanent after first upload |
| Version | 1.0.0, versionCode 1 (`BuildCommands.Version` / `VersionCode`) | raise `VersionCode` for every upload |
| App icon | `icon-512.png` (512 × 512, opaque) | Store listing → App icon |
| Feature graphic | `feature-graphic-1024x500.png` (1024 × 500, opaque) — source `feature-graphic.mjs` | Store listing → Feature graphic |
| Phone screenshots | `../StoreScreenshots/google-play/*.png` (1080 × 1920) | Store listing → Phone screenshots |
| Privacy policy | `privacy-policy.html` — fill the 3 `[...]` placeholders, host it, paste the URL | App content → Privacy policy |
| Listing text | `listing.md` | Store listing |

## 1. Create the upload keystore (once, by you)

Run in PowerShell. It asks for a password and your name/organisation; **choose the password yourself and write it
down**. Nothing here stores it.

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\DesiMergeKeys"
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe" `
  -genkeypair -v -keystore "$env:USERPROFILE\DesiMergeKeys\desimerge-upload.keystore" `
  -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

Back up `desimerge-upload.keystore` and its password in two safe places (e.g. USB drive + Google Drive). It is outside
the project folder and `.gitignore` also blocks `*.keystore` / `*.jks`.

## 2. Build the signed AAB

In the **same** PowerShell window (the password lives only in this window and disappears when you close it):

```powershell
$s = Read-Host "Keystore password" -AsSecureString   # typed as ****, never shown
$env:DESIMERGE_KEYSTORE_PASS = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -nographics -quit `
  -projectPath "C:\Users\HP\DesiMerge" -buildTarget Android `
  -executeMethod DesiMerge.Editor.BuildCommands.BuildGooglePlayBundle -logFile "$env:TEMP\desimerge-aab.log"
Remove-Item Env:DESIMERGE_KEYSTORE_PASS
```

Output: `Builds/GooglePlay/DesiMerge-1.0.0-1.aab` plus a `*.symbols.zip` (upload it under the release's
"App bundle explorer → Native debug symbols" to remove Play Console's symbols warning). If the key password differs
from the keystore password, also set `DESIMERGE_KEY_PASS`. The build clears the passwords from Player Settings
afterwards. Or use the Editor menu **Desi Merge → Release → Google Play AAB (signed)** after setting the variable.

## 3. Note about the phone

`com.desimerge.game` is a different app from the test builds (`com.DefaultCompany.DesiMerge`). Installing it doesn't
replace them, and progress doesn't carry over: it starts fresh. Uninstall the old test app when you no longer need it.
