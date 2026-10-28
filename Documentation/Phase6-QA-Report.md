# Desi Merge — Phase 6 QA Report (Release-Candidate Validation)

Date: 2026-10-28 · Project: `C:\Users\HP\DesiMerge` · Unity 6000.6.7f1 · Machine: Windows 11 Pro, 8 GB RAM, integrated Intel GPU.
The pasted Phase 6 brief was cut off at section 11 ("Use clear…"); everything up to that point was followed.

## 1. Baseline (before changes)

| Item | State |
|---|---|
| Git | Branch at `f93f0bd` (Phase 4). **Phase 5 work uncommitted** in the working tree; preserved, nothing reverted. |
| Unity / modules | 6000.6.7f1 with AndroidPlayer, WindowsStandaloneSupport, WebGLSupport installed. |
| Compile | 0 errors, 0 project warnings (end of Phase 5). |
| Tests | EditMode 172/172, PlayMode 92/92 (Phase 5 runs, same day). |
| Android build | Last successful build: Phase 1 (development APK). Not rebuilt since. |
| Known bugs | None reported. Known limitations listed in PhaseStatus.md (Phase 3–5). |
| Performance | Never measured. No device available. |

## 2. Audit findings

| # | Finding | Severity | Status |
|---|---|---|---|
| F1 | **Phantom merge**: when two merges resolve in one physics step, the pool hands a just-merged object straight back as the new item (un-consumed), so a stale contact reported in the same step could merge two items that never touched (extra score, extra merge stats). Reproduced by a new test (failed: 3 merges instead of 2). | **High** | **Fixed** (`MergeBoard`: merged items stay consumed and out of the pool until the step is resolved). Regression test added. |
| F2 | **Screen orientation was Auto Rotation with landscape allowed** for a portrait-only layout. | **High** | **Fixed**: Portrait only (verified in the built APK manifest: `screenOrientation="portrait"`). |
| F3 | **No Android back-button handling** (back did nothing in game and menu). | Medium | **Fixed**: Gameplay — cancels Hammer selection, else pause/resume; ignored at game over. Menu — closes the open panel, else exits the app (saves are flushed on quit). |
| F4 | **App going to the background mid-game did not pause the game** (play would continue unseen on return / under a notification shade). | Medium | **Fixed**: `OnApplicationPause(true)` (and focus loss on device) pauses play; the player resumes from the pause panel. Game over and revive-in-progress are unaffected. |
| F5 | Template `SampleScene` was enabled in Build Settings (shipped unused scene). | Low | **Fixed**: disabled (the scene file is kept). |
| F6 | Application identifier is the placeholder `com.DefaultCompany.DesiMerge`; company name `DefaultCompany`. | **Release blocker** | **Open — owner decision** (permanent on Google Play; not invented here). |
| F7 | No release keystore: APKs are signed with Unity's debug key. | **Release blocker** | **Open — owner action** (create/keep a keystore, or use Play App Signing; never commit it). |
| F8 | `android.permission.INTERNET` is in the merged manifest although the game has no network code (Unity adds it by default; `ForceInternetPermission` is off). | Low | Documented. Harmless for an offline game, and a future ad SDK needs it. Removing it would need a manifest override. |
| F9 | `submitAnalytics: 1` in Player Settings (Unity engine device statistics). Unity Cloud services (Analytics/Ads/Crash) are all disabled; no project code collects data. | Low | Documented for the owner's privacy review / Data safety form; not changed. |
| F10 | Unused packages: `com.unity.collab-proxy`, `com.unity.learn.iet-framework` (template tutorials in `Assets/Welcome`), `com.unity.visualscripting`, `com.unity.timeline`, several 2D packages. | Low | Documented, not removed (template tutorial assets depend on iet-framework; removal is an owner decision). Engine code stripping is on. |
| F11 | Windows standalone settings were 1920×1080 full screen (landscape) — irrelevant for Android, awkward for desktop testing. | Low | Changed to a 540×960 window (Standalone only). |
| F12 | Toast not scaled on 3:4 tablets (from Phase 5). Real ad provider not integrated. Undo deferred. | Low / known | Unchanged, documented in PhaseStatus.md. |

Checked and found OK: no `FindObjectOfType`/`GameObject.Find` at runtime; single bootstrapper (duplicate destroyed); scene entry points dispose subscriptions; per-frame `Update`s are self-disabling animations or a tiny popup pool; disk writes only at save points (game over, leaving, app pause/focus/quit, reward grant); development fallbacks are compiled only in Editor/development builds (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`), simulated ads only when `Debug.isDebugBuild`; no secrets, API keys or network calls in the project; no Android plugins.

## 3. Files changed in Phase 6

| File | Why |
|---|---|
| `Scripts/Gameplay/MergeBoard.cs` | F1 fix (`RetireMerged`, `ReturnRetiredToPool`; `Clear` also returns retired items). |
| `Scripts/Gameplay/GameplayLoop.cs` | `HandleBack`, `HandleAppInterrupted` (F3, F4). |
| `Scripts/Gameplay/GameplayEntryPoint.cs` | Back button + app pause/focus hooks (F3, F4). |
| `Scripts/UI/MainMenuEntryPoint.cs` | Back button: close panel / exit (F3). |
| `Scripts/Platform/BackButton.cs` (new) | Event-driven back/Escape input (Input System). |
| `Scripts/Editor/BuildCommands.cs` | Release-config Android build, Windows test build, richer build log line. |
| `ProjectSettings/ProjectSettings.asset` | Portrait only (F2); Windows window 540×960 (F11). |
| `ProjectSettings/EditorBuildSettings.asset` | SampleScene disabled (F5). |
| Tests: `PlayMode/MergeBoardTests.cs`, `PlayMode/ReleaseHardeningTests.cs` (new), `PlayMode/SceneFlowTests.cs`, `EditMode/ServiceCompositionTests.cs` | Regression and hardening tests (below). |
| Docs: this report, `PhaseStatus.md`, `TestingStrategy.md` | |

No packages added or removed. No gameplay rules changed.

## 4. Tests

| Suite | Total | Passed | Failed | Skipped | Not run |
|---|---|---|---|---|---|
| EditMode (final run) | 174 | 174 | 0 | 0 | 0 |
| PlayMode (final run) | 98 | 98 | 0 | 0 | 0 |

New in Phase 6 (8): phantom-merge regression (failed before the fix, passes after); back button (pause toggle, Hammer cancel, ignored at game over); app interruption pauses once and never after game over; five restarts leave no items and no duplicated subscriptions; merges after game over can't change the final score; missing optional content (no items/catalogs/ads) still gives working offline services; disk write failure in `FlushAll` keeps changes and saves later; measurement harness. `SceneFlowTests` (real scenes) now also checks menu back closes a panel and `OnApplicationPause` pauses gameplay.
Existing coverage for merges, scoring, spawn bounds, saves/recovery, collection, achievements, daily challenges, power-ups, ads/revive and settings persistence (Phases 1–5) all passed again. Tests use in-memory stores, temp folders, manual clocks and fake ad providers only.

## 5. Builds

| Build | Result |
|---|---|
| Android, release configuration (IL2CPP, ARM64, min SDK 26, target SDK 36), `BuildCommands.BuildAndroidReleaseConfig` | **Succeeded**: 0 errors, 2 warnings (URP Terrain shaders unsupported/stripped; terrain unused). 17 min 35 s. APK 31.7 MB at `Builds/Android/DesiMerge-rc-unsigned-test.apk`. Scenes: Bootstrap, MainMenu, Gameplay. **Debug-signed — not uploadable.** |
| Windows 64-bit, `BuildCommands.BuildWindows` | **Succeeded**: 0 errors, 2 warnings (same shaders). 2 min 44 s. `Builds/Windows/DesiMerge.exe`. |
| Windows release player smoke run (headless, 25 s) | Booted, loaded Bootstrap → MainMenu, **no errors or exceptions** in the player log. (Headless: no input or rendering checked.) |
| Android device install / run | **Not done — no device available.** |

`Builds/` is ignored by Git (`.gitignore` line 6; verified with `git check-ignore`).

## 6. Performance (measured, with environment)

| Measurement | Environment | Result |
|---|---|---|
| Board step (2D physics + merge resolution + loop events), 40 items, 500 fixed steps, merges happening (40 → 28 items) | Unity Editor PlayMode test, batch mode, Mono, dev PC (8 GB, Intel iGPU) | ≈ 0.012–0.031 ms per step across 4 runs (budget at 50 Hz: 20 ms) |
| Managed heap growth over those 500 steps | `Profiler.GetMonoUsedSizeLong`, counter verified with a 64 KB probe | No measurable growth; 0 GC collections. (`GC.GetAllocatedBytesForCurrentThread` was found not to work on Unity's Mono and was not used.) |
| Asset weight | File sizes / import settings | Largest runtime assets: music WAVs 0.7 MB + 0.5 MB source (imported as mono Vorbis q0.6, compressed in memory); all textures < 7 KB. |
| APK size | Built APK | 31.7 MB |

**Not measured:** frame rate, rendering cost, memory, battery or thermals on any Android device, and Unity Profiler captures of the full scene. Editor numbers are not device numbers.

## 7. Save recovery and offline results

All verified by automated tests (no device): first launch without a save; corrupt save → backup; unrecoverable save → defaults with the bad copy kept as `<key>_corrupt` and nothing overwritten at load; write failure → no exception, changes retried; app pause flushes; interrupted reward claim neither loses nor duplicates; duplicate ad callbacks ignored; ad unavailable/failed/cancelled/offline → no reward, Restart always works; missing optional content → game still runs. The game has no network code: core play is fully offline.

## 8. Release blockers and outstanding manual work

Blockers for a store release candidate:
1. Choose the final **application ID** and **company name** (Player Settings → Android) — permanent once published.
2. Create a **release keystore** (or plan Play App Signing) and build an **AAB** signed with it.
3. **Physical Android device testing** (checklist below) — nothing has been verified on a device.
4. If ads ship: integrate a real SDK, consent (UMP/CMP), Data safety and target-audience declarations (see PhaseStatus.md Phase 5).

Device checklist (to do on at least one low-end and one modern phone, including a notched/rounded one):
- [ ] Installs and launches; portrait locked; no rotation.
- [ ] Safe area: top bar and buttons clear of notch/rounded corners; text readable; buttons easy to hit.
- [ ] Drag/lift to drop is accurate; taps on HUD buttons never drop items.
- [ ] Hammer, Shuffle, Pause, Resume, Restart, Main Menu, Settings, Collection, Awards, daily Claim.
- [ ] Back button: in game pauses/resumes, cancels Hammer; in menu closes panels, then exits.
- [ ] Home button / incoming call / notification mid-game → returns paused; music resumes correctly; no time jump game-over.
- [ ] Kill the app after a game → best score, stats, collection, power-up charges survive relaunch.
- [ ] Airplane mode: everything works; Continue hidden in release build.
- [ ] Profile with the Unity Profiler (development build + USB): FPS, GC per frame, memory over a 10-minute session.

## 9. Verdict

**Not ready for store release.** The build is a **technical release-candidate for internal device testing**: it compiles, all 272 automated tests pass, and Android and Windows builds succeed. It still needs the owner decisions above (application ID, signing) and device validation.
