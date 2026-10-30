# Desi Merge: Phase 6.6 Test Report

Date: 2026-10-30. Machine: Windows 11, 8 GB RAM. Unity 6000.6.7f1, batch mode. Screenshots are scripted offscreen renders of the real generated scenes (540×960 = 9:16, 720×960 = 3:4, 540×1170 with a simulated notch), saved as `Screenshots/phase6.6-*.png`.

## Automated tests (final runs, after the last code change)

| Suite | Total | Passed | Failed | Skipped |
|---|---|---|---|---|
| EditMode | 203 | 203 | 0 | 0 |
| PlayMode | 110 | 110 | 0 | 0 |

Compile: 0 errors, 0 script warnings.

**New EditMode (23, `RewardSystemsTests`):**
- Daily rewards: first claim once and restart; consecutive days and streak; missed day back to Day 1 (best streak kept); wrap after the last day; clock moved back; full storage then later claim; interrupted claim not paid twice.
- Missions: deterministic, distinct rotation; merge progress completes once; claim exactly once and saved; daily reset vs Monday weekly reset; score, create-item and power-up types from real events (including "without power-ups"); no counting after dispose.
- Milestones: exact threshold 99/100 and only once; an existing best score honoured at startup without being reset; claim once and persisted.
- Achievement reward applied once across restarts; collection discovery reported once and persisted.
- Audio: exactly one listener; music volume and mute applied immediately; the shipped library has a clip for every cue.
- Settings: tutorial flag persists and a v2 file loads.
- Shipped catalogs are valid and use only real power-ups.

**New PlayMode (7, `ExperienceFlowTests`, real scenes, in-memory saves):**
- Every new menu button opens its screen, only one at a time, and back closes it; collection hints.
- Daily claim from the screen: rapid double tap pays once, one popup, the dot clears, back collects.
- Mission and rank claimed from their cards through the real services.
- First game shows the tutorial, which blocks drops; Skip ends it for good and dropping works again.
- Game-over summary shows real counts and the new discovery.
- Exactly one enabled AudioListener in the real menu scene, and menu music is assigned.
- The splash intro finishes once, within its duration.

**Existing tests changed, with reasons (none removed):**
- `ProgressionIntegrationTests.Toasts_…`: discoveries now go to the dedicated `ShowDiscovery` (toast with item art) instead of a plain text toast. The assertion checks that discovery is shown once.
- `ProgressionIntegrationTests.Collection_…`: locked slots now show a hint ("2× L0") when the item that makes them is discovered. The "???" case is covered in `ExperienceFlowTests`.
- `SceneFlowTests.MenuToGameplayAndBack…`: the AudioSource count is now read from the service (1 + 8 voices, raised from 6 for the extra cues) instead of the hard-coded 7. It still asserts a single audio host.

Failures found and fixed during the phase:
- The three new catalog classes were in files with other names, so Unity saved the assets without a script reference. A shipped-catalog test caught it; the classes were moved to their own files.
- Mission-row and rank-card text overlapped (seen in screenshots, caused by the pivot convention).
- The reward popup showed an empty glow for a missing second reward.
- The claim label overflowed its button.
- Test-only: a teardown scene-name clash; an old test indexing past a 4-item rig.

## Acceptance checklist

| Check | Result | Evidence |
|---|---|---|
| Sound actually routed (listener present) | Pass (automated) | Listener tests; **audibility not verified by ear** |
| Volume and mute apply immediately and persist | Pass | EditMode audio and settings tests; existing settings UI tests |
| Daily reward once per day, streak, missed-day rule, restart, clock back | Pass | 7 EditMode and 1 PlayMode tests |
| Missions progress from events, claim once, reset policy | Pass | 6 EditMode and 1 PlayMode tests |
| Rank thresholds, no duplicate unlock, existing best kept | Pass | 3 EditMode and 1 PlayMode tests |
| Every new button opens a real screen; one modal at a time; back | Pass | `Menu_EveryNewButton…` |
| Tutorial once, skippable, blocks input | Pass | `FirstGame_ShowsTheTutorial…` |
| Results card | Pass | `GameOver_ShowsTheSessionSummary…`, screenshots |
| Gameplay rules, scores, physics unchanged | Pass | All existing gameplay, merge, score and power-up tests pass unchanged; item data untouched |
| Layout at 9:16, 3:4, notch | Pass (simulated) | Screenshots; modal cards scale on short screens |

## Android build

`BuildCommands.BuildAndroidReleaseConfig` (IL2CPP, ARM64) **succeeded**: `Build Finished, Result: Success`, 0 errors, 2 warnings (the known unused URP Terrain shaders), 12 min 42 s (it included the Android re-import of the new assets). Output: `Builds/Android/DesiMerge-rc-unsigned-test.apk`, **32.5 MB** (32,480,285 bytes; Phase 6.5 was 32.3 MB). It is debug-signed and cannot be uploaded to Play. **Not installed on a device.**

## Not verified

- **No physical Android device was used.** Audio audibility, audio focus and interruptions, touch feel, notch behaviour, frame rate and memory are unverified on hardware.
- Nobody listened to the audio: batch mode has no audio output device.
- The Editor Game view was not used interactively; flows were driven by tests and screenshots.
- Visual quality was judged by me from screenshots; no player testing was done.

## Manual test steps

**Preview every screen (Editor).** If the Editor says another Unity instance is running, close leftover `Unity.exe` in Task Manager first.
1. Open `Assets/_Project/Scenes/Bootstrap.unity` and press **Play**. You should see the splash, then the menu. Set the Game view to 1080×1920, then 1536×2048.
2. Menu: tap the rank card (Ranks), Daily Rewards, Missions (try all three tabs), Collection, Awards and Settings. Each opens alone and Esc closes it.
3. Press **Play**. On a fresh profile, *How to Play* appears; go through the pages or Skip.
4. Pause → Restart (confirm) → Resume; play until **Game Over** and read the summary.

To get a fresh profile, delete the save folder (it is your own Editor data): `%USERPROFILE%\AppData\LocalLow\DefaultCompany\DesiMerge\saves` (Editor and Windows builds).

**Daily rewards**
1. Fresh profile → the menu shows a red dot on Daily Rewards → open it → Day 1 glows → Claim → the popup shows "Hammer ×1" → Collect. The button now reads *See you tomorrow*, and in a game the Hammer button shows one more use.
2. Close and re-open the app: still claimed (no second claim).
3. Next day: change the PC/phone date forward 1 day → Day 2 is available, streak 2. Forward 2 more days (skipping one) → back to Day 1.
4. Set the date back before the last claim → the claim is unavailable, with a message about the device date. Restore the correct date afterwards.
5. Fill storage: claim until Hammer shows 9 saved → the next Hammer reward says storage is full; use one in a game, then claim.

**Missions:** Missions → Daily shows 3 missions plus Today's Challenge, with a reset countdown. Play: merge, finish games, use Hammer/Shuffle. Mission toasts appear in game, and the results card says rewards are waiting. Back in the menu, the Missions dot is on; Claim → popup; the mission moves to *Completed*. The next UTC day replaces daily missions; Monday replaces weekly ones.

**Ranks/unlocks:** score 100+ → in game "New rank: Naya Khiladi!", and the results card shows the new rank → the menu rank card shows the rank and a dot → open it → Claim → popup. Reach a new item → a toast with its art, and Collection shows it and the hint for the next one.

**Audio:** see the checklist in `AudioSystemAudit.md`.
