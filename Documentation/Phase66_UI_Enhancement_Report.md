# Desi Merge: UI Enhancement Report (Navigation, Records, Profile, Launcher Icon)

Date: 2026-11-02. Unity 6000.6.7f1, batch mode. **No Android device was connected** (`adb devices` empty), so nothing below was checked on a phone. The brief mentioned two reference screenshots, but **no images were attached**; the work follows the written description and the existing Desi Merge style, and nothing was copied from another game.

## Implemented

| Feature | Status | Details |
|---|---|---|
| Android launcher icon | **Done**, verified in the APK | Adaptive icon (background + foreground layers). `AndroidLauncherIcon.md` |
| App label | **Done**, verified in the APK | "Desi Merge" (with a space) |
| Hamburger menu (gameplay, top-left) | **Done** | Pauses the game; 8 real items + Return to Game. `GameNavigation.md` |
| Records screen (Achievements / Score tabs) | **Done** | Real achievements; local personal records; new score history. `RecordsAndAchievementsUI.md` |
| Player profile | **Done**, within the supported data | Avatar = biggest item, name line = real rank, progress to the next rank, NEW badge. No name entry, XP or login (not invented). `PlayerProfileAndHUD.md` |
| HUD layout | **Done** | Menu button added; score and best moved, not shrunk; safe area and short-screen scaling as before |

**Menu items connected:** Records (Score tab), Awards (Records, Achievements tab), Collection, Daily Rewards, Missions, Settings, How to Play (the tutorial), Home (main menu, saves first), Return to Game. On the main menu the **Awards** tile became **Records**; the old Awards panel is no longer in any scene. Its class and prefab are kept for an existing isolated test.

**Icons added (original, same style, 128 px, transparent):** Menu (hamburger), Podium (records), Profile (default avatar), Help, Chart (score tab). Reused: Trophy, Grid, Gift, Missions, Gear, Home, Close, Back, Medal, Crown, Star, Calendar, Play. 28 icons in total.

## Files changed

- **New runtime:** `UI/RecordsPanel.cs`, `UI/ScoreRecordRow.cs`, `UI/GameplayMenu.cs`
- **Changed runtime:**
  - `Gameplay/GameplayLoop.cs`: `OpenMenu`, `MenuOpen`.
  - `Gameplay/IGameplayView.cs`: `MenuRequested`, `ShowMenu`, `HideMenu`.
  - `Gameplay/GameplayEntryPoint.cs`: menu binding; back closes the top screen.
  - `UI/GameplayHud.cs`, `UI/MainMenuEntryPoint.cs` (profile avatar and badge, Records), `UI/ProgressCard.cs` (optional leading icon).
  - `Progression/PlayerProgressData.cs`, `PlayerProgressService.cs`, `IPlayerProgressService.cs`: score history, save schema 3.
- **New editor:** `Editor/Content/NavigationScreensBuilder.cs`, `Editor/Content/LauncherIconBuilder.cs` (also contains `AndroidAppLabel`)
- **Changed editor:** `GameArtGenerator.cs` (5 icons), `GameplaySceneBuilder.cs` (menu button, layout, menu), `MainMenuSceneBuilder.cs` (profile card, Records tile), `RewardScreensBuilder.cs` (row with icon), `ExperienceContentSetup.cs` (runs the icon setup)
- **Assets:** `Art/Icon/LauncherBackground.png`, `Art/Icon/LauncherForeground.png`, `Art/UI/Icons/{Menu,Podium,Profile,Help,Chart}.png`; regenerated `MainMenu.unity`, `Gameplay.unity` and the UI prefabs; `ProjectSettings.asset` (Android adaptive icons)
- **Tests:**
  - New: `EditMode/NavigationAndProfileTests.cs` (5), `PlayMode/NavigationFlowTests.cs` (4).
  - Updated for the Awards → Records change: `SceneFlowTests.cs`, `ProductionStressTests.cs`. The fake view in `GameplayTestRig.cs` gained the menu methods.

Gameplay rules, scoring, physics and existing save data are unchanged. The save change only adds a list, and old saves load with it empty (tested).

## Tests actually executed (final runs, after the last code change)

| Suite | Total | Passed | Failed | Skipped |
|---|---|---|---|---|
| EditMode | 222 | 222 | 0 | 0 |
| PlayMode | 121 | 121 | 0 | 0 |

New coverage:
- **Score history:** order, NEW BEST flags, cap of 10, a revived game updating its own entry, persistence, v2 → v3 with no loss.
- **Launcher icon:** both adaptive layers set in every density slot; no foreground pixel outside the 66 dp safe circle.
- **Hamburger menu:** pauses, blocks drops, can't open twice; an outside tap resumes with the board preserved.
- **Every menu item** opens its real screen, one at a time; back closes the top screen, then resumes.
- **Records:** shows the real best score, recent games, all 11 achievements with icons.
- **How to Play** works over the paused game.
- **Profile:** new player (default avatar, NEW badge) → returning player (rank name, biggest-item avatar, untinted).

**Found and fixed by the tests and screenshots before the final run:**
- The icon's sparkle tips poked outside the safe circle (22 pixels; the test failed).
- A returning player's avatar was tinted navy (a black blob in the screenshot); a test now checks it.

## Android build

`BuildAndroidReleaseConfig`: **Success**, 0 errors, **2 warnings**: Unity 6.6 `UAC0009` deprecation notices for the existing `#if UNITY_EDITOR || DEVELOPMENT_BUILD` in `GameplayEntryPoint.cs` and `MainMenuEntryPoint.cs`. That is pre-existing code that keeps the development fallback; there is no behaviour impact. Not changed here, to avoid another validation cycle; it can be replaced with a runtime `Debug.isDebugBuild` check later.

| | |
|---|---|
| APK | `C:\Users\HP\DesiMerge\Builds\Android\DesiMerge-rc-unsigned-test.apk` |
| Size | 32,827,029 bytes (31.3 MB) |
| SHA-256 | `fb98c38d0f456e525be728ee09f79d9d9dd4b19d309e0e1b5510f63e4d384a65` |
| Package / version | `com.DefaultCompany.DesiMerge` (unchanged) / 1.0 (1) |
| Label | **Desi Merge** (`aapt`: `application-label:'Desi Merge'`) |
| Icon | `aapt dump xmltree` of the app icon: `<adaptive-icon>` with `ic_launcher_background` and `ic_launcher_foreground` |
| Permissions | No INTERNET |
| Signing | Debug key (`CN=Android Debug`), APK Signature Scheme v2 verified. Fine for your phone, **not** for Play. |

## Remaining limitations

- **Not tested on a phone:** how the icon looks on your home screen, touch feel of the menu, notch layout on real hardware.
- No player-name entry and no XP or level system (not invented; options for later).
- Records are personal and on this phone only; there is no online leaderboard.
- Two pre-existing deprecation warnings (above).
- Open from earlier: package ID and release key (yours), the rare wall overlap (PR-13).

## Preview in Unity

1. Open `Assets/_Project/Scenes/Bootstrap.unity`, press **Play**, and set the Game view to 1080×1920.
2. Main menu: the profile badge is top-left; the **Records** tile has Achievements and Score tabs.
3. **Play**, then tap the teal **☰** at the top-left of the bar. Try every tile, Return to Game, a tap outside the card, and **Esc** (Android back).
4. To see the icon: **Edit → Project Settings → Player → Android → Icon → Adaptive**, or open `Documentation/Screenshots/launcher-icon-preview.png`.

## Install and verify on your phone

1. Connect the phone with USB debugging on, then:
   `& "C:\Users\HP\Downloads\scrcpy-win64-v5.0.1\scrcpy-win64-v5.0.1\adb.exe" install -r "C:\Users\HP\DesiMerge\Builds\Android\DesiMerge-rc-unsigned-test.apk"`
   (or copy the APK to the phone and open it in Files).
2. **Home screen:** the icon is the mango on magenta in your launcher's shape (circle or squircle), with the name "Desi Merge".
3. **In a game:** tap ☰. The game freezes; open Records (Score: your best and last games), Awards (achievements), and the rest; Android back closes the top screen, then the menu.
4. **Main menu:** the profile shows "New Player" with NEW; after a game with a score of 100+, it shows "Naya Khiladi" and your biggest item as the avatar.
5. If the install fails with a signature conflict, uninstall the old preview first (this deletes its progress).
