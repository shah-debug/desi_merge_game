# Phase 6.7: Android Device Test Report

**No physical Android device or emulator was available.** `adb devices -l` (using the adb that ships with scrcpy) listed no devices on 2026-10-30, and no Android emulator is installed on this PC. **No scenario in this file was run on Android.** Nothing here may be read as a device pass.

## What was verified without a device

| Item | How | Result |
|---|---|---|
| APK builds (IL2CPP, ARM64, minSdk 26, targetSdk 36, portrait) | Real release-config build | See `FinalProductionAudit.md` |
| Manifest: portrait only; permissions | Generated Gradle manifest | Portrait; INTERNET removed (fix PR-05) |
| Lifecycle code paths | PlayMode tests call the same handlers Android triggers (`OnApplicationPause`, `OnApplicationFocus`) | Gameplay pauses once and not after game over; services flush on pause, focus loss and quit |
| Back button | Escape-key action, the same Input System binding as Android back | Every screen tested |
| Safe area and aspect ratios | Simulated 9:16, 19.5:9 with a notch, and 3:4 screenshots | Layout fits; modals scale |

## Manual device test plan (owner, with a phone)

Record: phone model, Android version, build (versionName and versionCode), date.

1. Install: `adb install -r Builds\Android\DesiMerge-rc-unsigned-test.apk` (or copy the APK to the phone). First launch shows the splash → menu; the tutorial appears at the first Play.
2. Play 3 full games: merges, the danger line, game over, Play Again, Main Menu.
3. Claim the daily reward, then **force-stop** (Settings → Apps → Desi Merge → Force stop), relaunch: still claimed, Hammer count kept.
4. Finish a mission and claim it; force-stop during the reward popup; relaunch: claimed once.
5. **Audio:** music on the menu and in game; taps, drop and merge sounds; Settings sliders and mute change the sound immediately; settings kept after relaunch.
6. Press Home during a game, return: the game is paused, music paused, Resume works. Lock and unlock the screen: same.
7. Incoming call or alarm during play: no sound overlap; paused afterwards.
8. Back button on every screen (popup → panel → menu → exit).
9. Notch/cut-out phone: the HUD isn't hidden; drag-to-aim works near the screen edges.
10. Play 15 minutes continuously: no slowdown, the phone doesn't overheat (note battery % before and after).
11. Low memory: open several heavy apps, return: the game resumes, or restarts with progress intact.
12. Optional: Settings → Developer options → "Don't keep activities": background and return; progress intact.

Each item is **NOT TESTED** until the owner runs it.
