# Desi Merge: Visual Redesign Test Report (Phase 6.5)

Date: 2026-10-28. Machine: Windows 11, 8 GB RAM, Intel iGPU. Unity 6000.6.7f1 in batch mode. Screenshots are real offscreen renders of the generated scenes at 540×960 (9:16), 540×1170 (19.5:9 with a simulated notch and gesture bar) and 720×960 (3:4), saved as `Documentation/Screenshots/phase6.5-*.png`.

## Automated tests (final runs, after the last code change)

| Suite | Total | Passed | Failed | Skipped |
|---|---|---|---|---|
| EditMode | 180 | 180 | 0 | 0 |
| PlayMode | 103 | 103 | 0 | 0 |

Compile: 0 errors, 0 project warnings.

New in this phase: 6 EditMode tests (`VisualContentTests`) and 5 PlayMode tests (`VisualRedesignSceneTests`), on top of the 174 + 98 from Phase 6.

Problems the tests caught along the way, all fixed and re-run:
- The chai saucer extended past its collider circle (11,680 px). The root cause was an ellipse-distance approximation that made outlines up to about 3× thicker on flat shapes. The painter was fixed.
- A few anti-aliased edge pixels of chai (6) and mango shake (2) still crossed the circle. All item art is now drawn at 97% scale.
- I also found board decorations (ornament caps) able to draw over items in the corners. Items, the preview and effects now sort above all board art, and a test enforces this.

A background run was stopped once by Claude Code because the PC was low on memory. It was re-run in the foreground; the numbers above come from the completed runs.

## Acceptance checklist

| # | Check | Result | How |
|---|---|---|---|
| 1 | Gameplay scene opens without missing references | Pass | Scene tests load it; the entry point validates references; no errors logged |
| 2 | All configured levels have intended artwork | Pass | `EveryLevel_HasItsOwnIllustratedSprite_DrawnUntinted`; all-levels screenshot |
| 3 | Merge rules, scores, progression unchanged | Pass | `Redesign_KeepsMergeOrderSizesAndScores`; the data diff only touches `sprite` and the new `tintSprite`; every existing merge and score test passes |
| 4 | Next preview matches the next spawned object | Pass | `NextPreview_ShowsTheArtworkOfTheItemDroppedNext` (3 drops in the real scene) |
| 5 | Pause/resume | Pass | Existing pause tests and scene flow; pause screenshot |
| 6 | Hammer and Shuffle through real implementations | Pass | Existing power-up tests; `PowerUpButtons_ShowSelectedAndUnavailableStates_FromTheRealService` |
| 7 | Game Over and Restart | Pass | Scene flow and smoke tests; pause Restart needs confirmation (`PauseRestart_AsksForConfirmation…`) |
| 8 | Main Menu navigation | Pass | `MenuToGameplayAndBack…` (menu → game → menu, panels open/close) |
| 9 | Settings persist | Pass | Existing settings persistence tests (unchanged service) |
| 10 | No duplicate subscriptions or repeated callbacks | Pass | New listeners are added in `Awake` and removed in `OnDestroy`; existing restart/subscription tests pass |
| 11 | No compiler errors or unexplained Console errors | Pass | 0 errors and 0 warnings; tests fail on unexpected error logs |
| 12 | Safe areas and aspect ratios | Pass (simulated) | Screenshots at 9:16, 19.5:9 with notch, and 3:4. The Phase 5 tablet toast overlap is fixed. Not checked on real notched hardware. |
| 13 | UI usable while paused | Pass | Panels and buttons use unscaled time; pause/confirm flow tested while `timeScale = 0` |
| 14 | Effects don't interfere with physics | Pass | `Decorations_HaveNoCollidersOrBodies…` and `MergePop_ScalesOnlyTheVisualChild…` |
| 15 | Android build | See below | |

## Android build

`BuildCommands.BuildAndroidReleaseConfig` (IL2CPP, ARM64) **succeeded**: 0 errors, 2 warnings (the known URP Terrain shaders, unused), 3 min 31 s of build-pipeline time. Output: `Builds/Android/DesiMerge-rc-unsigned-test.apk`, **32.3 MB** (Phase 6: 31.7 MB, so the new art adds about 0.5 MB). Debug-signed and not uploadable to Play. **Not installed on a device.**

## Not verified

- **No physical Android device was used.** Touch feel, real notch/cutout behaviour, the adaptive-icon appearance, frame rate, texture memory and battery on phones are unverified.
- The live Unity Game view with real mouse/touch input was not used. Screenshots are scripted renders, and interactions were driven by tests.
- Visual quality is judged from the screenshots by me (the developer). No user testing was done.

## Manual verification steps (Unity Editor)

1. Open the project in Unity Hub, then open `Assets/_Project/Scenes/Bootstrap.unity` and press **Play**. The game starts at the main menu, as on a phone.
2. In the Game view, choose a portrait resolution (e.g. 1080×1920, then 1080×2400, then 1536×2048) and check that nothing overlaps.
3. Menu: open Collection, Awards and Settings, then close each. Move the sliders, restart Play mode, and check the values are kept.
4. Press **Play**: drag to aim, release to drop. Merge two Patti leaves and watch for the pop, ring, sparkles and +score.
5. Tap **Hammer**: it highlights in saffron, Shuffle dims and a magenta banner appears. Tap an item to remove it, or press Cancel.
6. Tap **Pause** → **Restart**: a confirmation appears. **Keep Playing** returns to the pause menu; **Yes, Restart** restarts.
7. Stack items past the dashed line to reach **Game Over**: check the score, NEW BEST (only when it is a new best), Play Again and Main Menu.
8. On a device: install `Builds/Android/DesiMerge-rc-unsigned-test.apk` and repeat steps 3–7. Also check the launcher icon, the notch area, and the back button.
