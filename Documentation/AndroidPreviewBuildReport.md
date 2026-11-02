# Desi Merge: Android Preview Build Report

Date: 2026-11-01. Unity 6000.6.7f1, batch mode, on the development PC. Starting point: commit `19b1c11` with a clean working tree.

## 1. Cleanup (conservative, every removal proven unreferenced)

| Removed | What it was | Proof it was unused |
|---|---|---|
| `Assets/Welcome/` (6 files: `Welcome2DScript.cs`, `Unity.2D.Welcome.asmdef`, `NewWelcomePage.asset`, `TutorialProjectSettings.asset`, `Welcome2DScript.asset`, `2d-template.png`) | Unity 2D template "welcome page". Its assembly had no platform limit, so it **compiled into the APK**, including a web link (`Application.OpenURL`). | GUID search of every file across `Assets/`, `ProjectSettings/`, `UserSettings/`, `Packages/`: no references outside the folder; no project code mentions it |
| `Assets/Scenes/SampleScene.unity` (and the empty `Assets/Scenes/` folder) | Unity template sample scene | Only reference: a **disabled** Build Settings entry, which was also removed |

The removals were made with `git rm` and are staged, not committed, so `git restore --staged --worktree <path>` (or `git checkout`) brings them back.

**Kept on purpose:**
- `Assets/Settings/*`: the URP pipeline, 2D renderer, volume profile and global settings are in use.
- `InputSystem_Actions.inputactions`: registered as the Input System project-wide config in `EditorBuildSettings`.
- The 2D scene template, `Lit2DSceneTemplate` and `Settings/Scenes/URP2DSceneTemplate.unity`: Editor-only, not in builds.
- `DevelopmentServices`: used only in the Editor and development builds, behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Superseded Phase 3 UI sprites: still referenced by `UITheme` fields; tiny.

No debug scenes, test menus, cheat buttons or debug UI exist in the game, so there was nothing else to remove.

**Tests retained and excluded:** all EditMode and PlayMode tests stay in the repository. `DesiMerge.Tests.EditMode` is Editor-only; both test assemblies have the `UNITY_INCLUDE_TESTS` constraint, so they are not compiled into players. The APK file list was checked: it contains **0** test, NUnit or TestRunner entries. No production scene or runtime assembly references a test type.

## 2. Screens in the build

Scenes (Build Settings, in order): `Bootstrap` (splash) → `MainMenu` → `Gameplay`. The APK contains exactly `level0`–`level2`.

Player-facing screens, all kept:
- Splash.
- How to Play (first game).
- Main menu with the rank card.
- Daily Rewards.
- Missions (Daily / Weekly / Completed, Today's Challenge).
- Ranks.
- Collection.
- Awards.
- Settings.
- Reward popup.
- Gameplay HUD.
- Pause with restart confirmation.
- Game Over with results.

## 3. Build settings (as built, read back from the APK)

| Setting | Value |
|---|---|
| Package (application ID) | `com.DefaultCompany.DesiMerge`: the existing one, **preserved**. It is valid, but uses Unity's placeholder company name; choose your permanent ID before the first Play Store upload (see Known limitations). |
| App name shown on the phone | DesiMerge |
| Version | versionName **1.0**, versionCode **1** |
| minSdk / targetSdk / compileSdk | 26 (Android 8.0) / 36 / 36 |
| Scripting backend / CPU | IL2CPP / **arm64-v8a** only (covers practically all phones since ~2017; no 32-bit-only phones) |
| Orientation | Portrait |
| Build type | Release configuration: `BuildOptions.None`, so no Development Build, no script debugging, no profiler auto-connect; the manifest is **not debuggable** |
| Artifact type | **APK** (`buildAppBundle = false`), not AAB |
| Permissions | No `INTERNET` (removed by `OfflinePermissionFilter`); the only entry is AndroidX's app-internal `…DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` |
| Install location | `preferExternal` (Unity default, unchanged) |

## 4. Build result

`DesiMerge.Editor.BuildCommands.BuildAndroidReleaseConfig`: **Build Finished, Result: Success**, **0 errors, 0 warnings**. The 2 URP terrain-shader warnings of earlier builds are gone. Build time 12 min 16 s. No compile errors.

## 5. The artifact (verified with Android build-tools 36.0.0)

| | |
|---|---|
| Path | `C:\Users\HP\DesiMerge\Builds\Android\DesiMerge-rc-unsigned-test.apk` |
| Size | 32,479,501 bytes (**31.0 MB**) |
| SHA-256 | `0f7d569894536b4f3c1c0ec321f8ed635cb381e15abba07af5dea12deae89de1` |
| `aapt dump badging` | package `com.DefaultCompany.DesiMerge`, version 1.0 (1), sdk 26, target 36, native code arm64-v8a, launch activity `com.unity3d.player.UnityPlayerGameActivity` |
| `apksigner verify` | **Verifies**: APK Signature Scheme v2, 1 signer, certificate `CN=Android Debug, O=Android, C=US` (SHA-256 `31f80e27…6498d10`) |
| Signing type | **Debug-signed preview.** This PC's Android debug key, not a production key. Fine for installing on your phone; **not accepted by Google Play**. The file name says "unsigned" for historical reasons; it is debug-signed. |

No production keystore exists in the project, and none was created, used or committed.

## 6. Known limitations

- **Not tested on a phone:** this build was only generated and inspected. Every item in the checklist below is untested until you run it.
- **Placeholder package ID:** a later switch to your permanent ID creates a *different* app on the phone; this preview's progress won't carry over.
- **Debug signature:** each PC has its own debug key. An APK built on another PC can't update this one (see "Uninstall" below).
- Open items from Phase 6.7: the 15-minute stress run after the final physics change has not been completed, and items can rarely press into a wall (PR-13). See `ProductionReadiness/BugRegister.md`.

## 7. Install on your phone

**Option A, USB cable (no extra apps):**
1. On the phone, go to Settings → About phone and tap **Build number** 7 times to enable Developer options. Then Settings → System → Developer options → turn on **USB debugging**.
2. Connect by USB, unlock the phone and accept "Allow USB debugging" for this PC.
3. In PowerShell, using the adb that came with scrcpy:
   ```
   & "C:\Users\HP\Downloads\scrcpy-win64-v5.0.1\scrcpy-win64-v5.0.1\adb.exe" install -r "C:\Users\HP\DesiMerge\Builds\Android\DesiMerge-rc-unsigned-test.apk"
   ```
   `Success` means it is installed. You will find **DesiMerge** in the app drawer.

**Option B, copy the file:**
1. Copy the APK to the phone (USB file transfer into `Download`, Google Drive, or WhatsApp "send to yourself" as a document).
2. Open it in the phone's **Files** app. When asked, allow "Install unknown apps" for that app (Files, Chrome or Drive), then tap **Install**.
3. If Play Protect warns about an unknown developer, choose **Install anyway**. This happens with debug-signed apps.

**Uninstall an older preview with a different signature:** if install fails with `INSTALL_FAILED_UPDATE_INCOMPATIBLE` (or "App not installed as package conflicts"), the phone has a DesiMerge signed by another key. Uninstalling **deletes its saved progress**:
- Phone: long-press the DesiMerge icon → App info → **Uninstall**; or
- PC: `adb uninstall com.DefaultCompany.DesiMerge`

Then install again.

## 8. Manual phone checklist (all NOT TESTED until you run them)

| # | Check | How |
|---|---|---|
| 1 | Installs | Option A or B above |
| 2 | Splash | Mango, "Desi Merge" plate, about 1 s, then the menu |
| 3 | Main menu loads | Rank card, Play, 4 tiles, settings gear |
| 4–5 | Play starts a game | First time: *How to Play* appears; Next or Skip |
| 6 | Drop and merge | Drag to aim, release; two identical items merge into the next one |
| 7 | Score | The number rises on each merge (+popup) |
| 8 | Best score persists | Game over → Main Menu shows the best score; still there after reopening |
| 9 | Pause and resume | Pause button; music pauses; Resume continues |
| 10 | Hammer and Shuffle | Hammer: tap an item to remove it. Shuffle: items rearrange. The counts go down. |
| 11 | Daily Rewards | Menu → Daily Rewards → Claim → popup; the Hammer count goes up; "See you tomorrow" |
| 12 | Missions | Play, then Menu → Missions: progress bars moved; Claim when done |
| 13 | Collection and unlocks | A new item shows a toast with its picture and appears in Collection |
| 14 | **Audio audible** | Menu music, button taps, drop and merge sounds (phone not on silent, media volume up) |
| 15 | Settings persist | Move the sliders, mute; close and reopen the app: values kept |
| 16 | Game Over | Stack past the dashed line: card with score, best and summary |
| 17 | Restart | Play Again starts an empty board with score 0 |
| 18 | Navigation | Every menu tile opens and closes; Main Menu from pause and game over |
| 19 | Progress survives | Swipe the app away from recent apps, reopen: best score, collection, claimed rewards kept |
| 20 | Back button | Closes the popup → panel → (menu) exits; in a game it pauses and resumes; it closes the tutorial |

Please note the phone model and Android version with the results.
