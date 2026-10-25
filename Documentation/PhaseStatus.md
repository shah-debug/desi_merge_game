# Desi Merge — Phase Status

| Phase | Scope | Status |
|---|---|---|
| 1 — Foundation | Structure, service interfaces, persistence, tests, docs, build check | Complete (approved, committed `eb25fdb`) |
| 2 — Core gameplay | Drop/merge loop, physics, scoring, game over, test HUD | **Complete — awaiting review** |
| 3 | Not defined yet | Not started |

## Phase 2 — Core Gameplay (2026-10-25)

### Starting point (inspected)

- Phase 1 code committed and pushed (`eb25fdb`, `origin/main`), working tree clean, open Editor log had 0 compile errors.
- Phase 1 manual step still pending: Bootstrap was **not** in Build Settings (only `SampleScene`). The Phase 2 setup
  tool now performs it.
- Project uses the new Input System only (`activeInputHandler: 1`); Physics 2D default gravity −9.81, FixedUpdate simulation.
- No foundation fixes were required. `FoundationSetup` was refactored to share scene-creation code with the new
  builder (behaviour unchanged).

### Implemented

- **Configuration:** `MergeItemDefinition` (id, display name, placeholder sprite, tint, radius, mass, score),
  `MergeProgression` (ordered chain = single source of truth for "next level"; spawnable count; validation),
  `GameplayConfig` (board, drop, game-over, final-level and physics tuning; validation), `FinalLevelMergeRule`.
- **Progression assets:** Patti → Nimbu → Pyaz → Tamatar → Roti → Aam → Chai → Mango Shake → Rickshaw → Bus
  (provisional names; the first 5 can be dropped).
- **Gameplay logic (plain C#):** `MergeRules`, `MergeResolver<T>` (once-only merges, no recursion), `GameSession`
  (score, single game over, restart), `ScoreRules`, `SpawnQueue` (seeded), `DropRules` (boundary clamp, can-drop),
  `GameOverRule`.
- **Unity components:** `MergeItem` (pooled body), `MergeBoard` (walls from config, pool, merge resolution in
  `FixedUpdate`, danger line), `DropController` (event-driven mouse/touch input, visual-only preview, cooldown preview),
  `GameplayLoop` (orchestration + Phase 1 services: game state, progress submit/flush, analytics, audio cues),
  `GameplayEntryPoint` (`ISceneEntryPoint`, validation, dev fallback), `IGameplayView` + `GameplayHud`.
- **Minimum UI:** score, best, next-item preview, game-over panel with score/new-best and Restart, config error text.
- **Rules (configurable, see Architecture.md → "Rules chosen for Phase 2"):** game over when an item's top stays above
  the danger line for 2 s (items < 1.5 s old ignored); final level `ClearBoth` with 100 bonus; 0.5 s drop cooldown.
- **Editor tooling:** `Desi Merge → Setup → Create Gameplay Content` (idempotent) and
  `Desi Merge → Setup → Rebuild Gameplay Scene (overwrites)`.

### Files

Created — scripts:
`Scripts/Configuration/{MergeItemDefinition, MergeProgression, GameplayConfig, FinalLevelMergeRule}.cs`,
`Scripts/Gameplay/{MergeRules, IMergeable, MergeResolver, GameSession, ScoreRules, SpawnQueue, DropRules, GameOverRule, MergeItem, MergeBoard, DropController, IGameplayView, GameplayLoop, GameplayEntryPoint}.cs`,
`Scripts/UI/GameplayHud.cs`, `Scripts/Editor/{EditorSceneUtility, GameplaySetup}.cs`.

Created — tests:
`Tests/EditMode/{TestContent, MergeLogicTests, GameplayRulesTests, GameplayConfigTests}.cs`,
`Tests/PlayMode/{GameplayTestRig, MergeBoardTests, GameplayLoopTests, GameplayEntryPointTests, GameplaySceneSmokeTests}.cs`.

Created — generated assets (by the setup tool, run in batch mode on a verification copy and copied in with their
`.meta` files): `Scenes/Gameplay.unity`, `Prefabs/MergeItem.prefab`, `Art/Placeholder/{Circle, Square}.png`
(generated shapes, **not final art**), `Settings/Physics/MergeItem.physicsMaterial2D`,
`ScriptableObjects/{GameplayConfig, MergeProgression}.asset`, `ScriptableObjects/MergeItems/01_patti … 10_bus.asset`.

Changed: `Scripts/DesiMerge.Runtime.asmdef` and `Scripts/Editor/DesiMerge.Editor.asmdef` (reference
`Unity.InputSystem`, `UnityEngine.UI` — both already installed; **no packages added**),
`Scripts/Editor/FoundationSetup.cs` (uses shared helper), `ScriptableObjects/GameConfig.asset`
(`firstSceneName: Gameplay`), `Documentation/{Architecture, TestingStrategy, PhaseStatus}.md`.
Added `Documentation/Screenshots/phase2-*.png`.

Not changed: `ProjectSettings/` in your project (Build Settings are updated by the menu step below), Packages,
template content.

### Scene and configuration changes

- `Gameplay.unity`: Main Camera (orthographic size 8), `Board` (MergeBoard; walls/floor/background/danger line are built
  at runtime from `GameplayConfig`), `DropController` + `Preview`, `HUD` canvas (1080×1920 reference, portrait),
  `EventSystem` (`InputSystemUIInputModule`), `GameplayEntryPoint`.
- `GameConfig.firstSceneName = Gameplay`, so Bootstrap → Gameplay.
- Build order produced by the setup tool: `Bootstrap` (0), `Gameplay` (1), existing `SampleScene` kept after them.

### Verification actually run

Batch mode on a copy of the project (the Editor holds the project lock). After the first PlayMode run failed on a
cached broken URP import in the copy (see below), the copy's `Library` was deleted, it was mapped to a short drive
path, fully reimported, and everything was re-run.

| Check | Result |
|---|---|
| Compile (Editor + test assemblies) | No `error CS`; no `warning CS` from `_Project` |
| Setup tool (`GameplaySetup.CreateGameplayContent`) | Exit 0, all assets created, build order and GameConfig updated |
| EditMode tests | **96 / 96 passed** (50 Phase 1 + 46 new) |
| PlayMode tests | **33 / 33 passed, 0 skipped** (5 Phase 1 + 28 new, incl. 2 smoke tests against the real Gameplay scene) |
| Rendered captures (smoke test) | 3 PNGs reviewed: walls, danger line, labelled items, preview, HUD, game-over panel, all 10 levels |
| Play Mode in the interactive Editor | **Not run by me** (Editor window not controllable from here) |
| Real mouse/touch input path | **Not covered by automated tests** — tests call `GameplayLoop.TryDrop` directly; `DropController` input callbacks were not exercised |
| Android build / device / Device Simulator | **Not run in Phase 2** (last verified Android build: Phase 1) |

Failures encountered and how they were resolved:
1. First PlayMode run: 32/33 — smoke test failed on an `Error` logged by URP
   (`Host type is not matching any asset type … TraceVirtualOffset.urtshader`). Your Editor log has no such error;
   it came from the verification copy's `Library`, which cached failed imports from Phase 1's long-path copy.
   After a clean reimport the error is gone (0 occurrences in all final logs) and the test passes.
2. First capture of each run drew the walls with the wrong texture (ellipses). Shown to be a first offscreen render
   artifact in batch mode: with one warm-up render the walls are correct. The scene/sprite references were verified
   correct.
3. One final run reported 31 passed / 2 skipped because a mirror sync had removed the generated scene from the copy;
   the content was regenerated and the suite re-run (33/33). Skipped tests are not counted as passes.
4. Test-isolation weakness found in review (smoke tests left the Gameplay scene loaded while later physics tests stepped
   all bodies): fixed with a teardown that unloads the scene, before the final runs.

### Known issues / limitations

- Item labels are a child `TextMesh` and rotate with the rolling item (can appear upside down); labels on the
  smallest items (Patti) are very small. Placeholder only.
- "Game Over" title overlaps the danger line on the panel; cosmetic.
- No pause UI; backgrounding the app does not set `GameState.Paused` (progress is still flushed on pause).
- HUD ignores the device safe area (notches/cut-outs).
- Audio is the Phase 1 no-op service; no sounds play.
- Playing the Gameplay scene directly uses in-memory dev services, so "Best" starts at 0 and nothing is saved. Start
  from Bootstrap for real saves.
- A tap anywhere on screen (including over the score text) aims/drops; there is no dedicated input zone yet.

### Manual steps (in your open Editor)

1. Click into the Unity Editor and wait for the import to finish; the Console should have no red errors.
   **Known:** at the end of this phase, your open Editor's last compile showed
   `GameplaySetup.cs: The name 'FoundationSetup' does not exist`. Its cached script list had dropped
   `FoundationSetup.cs` and `BuildCommands.cs` after an auto-refresh during editing (both files are on disk,
   unchanged, and compiled fine in the clean batch build). Their timestamps were touched so the Editor re-imports
   them. If the error remains, right-click `Assets/_Project/Scripts/Editor` → **Reimport**; if it still remains,
   close and reopen the project.
2. Run **Desi Merge → Setup → Create Gameplay Content** once. Everything already exists, so this only sets Build
   Settings to Bootstrap (0), Gameplay (1). Confirm under File → Build Profiles → Scene List.
3. Run the Test Runner (Window → General → Test Runner): EditMode → Run All, PlayMode → Run All.

### How to play-test

- **Quick:** open `Assets/_Project/Scenes/Gameplay.unity` → Play. (Console shows one yellow warning:
  "Started without Bootstrap…" — expected.)
- **Full flow (real saves):** open `Assets/_Project/Scenes/Bootstrap.unity` → Play; it loads Gameplay automatically.
- **Phone-shaped view:** in the Game view, switch the dropdown from *Game* to *Simulator* and pick a phone.
- **Controls:** move the mouse (or drag a finger in Simulator) to aim the faded preview; click/release to drop.
  Two equal items merge into the next one and add score. Stack items above the red line for 2 s to see Game Over,
  then press **Restart**.
- **Tuning:** select `ScriptableObjects/GameplayConfig.asset` (board, cooldown, game-over timing, final rule) or any
  `ScriptableObjects/MergeItems/*.asset` (name, colour, size, score). Changes apply on the next Play.

## Phase 1 — Foundation (2026-10-25)

### Environment (inspected)

- Unity **6000.6.7f1** (Hub install) with Android Build Support (bundled SDK platforms 34/36/37, NDK, OpenJDK), WebGL, Windows.
- Project from the Unity 6 URP 2D template. Packages unchanged; Test Framework 1.8.0 already present.
- Not a Git repository. No prior game code.
- Player Settings at template defaults: company `DefaultCompany`, product `DesiMerge`, no explicit application id, Android min SDK 26.

### Delivered

- `Assets/_Project/` folder structure (Scripts by area, Scenes, Prefabs, Art, Audio, Animations, ScriptableObjects, Settings, Tests).
- Assemblies: `DesiMerge.Runtime`, `DesiMerge.Editor`, `DesiMerge.Tests.EditMode`, `DesiMerge.Tests.PlayMode`.
- Services and implementations: game state machine, player progress, settings, audio (no-op), analytics (log/null),
  rewarded ads (simulated/unavailable, no SDK), app lifecycle, file-based save store with atomic writes.
- Composition root (`GameBootstrapper`) + scene-scoped injection (`ISceneEntryPoint`), no global lookups.
- `GameConfig.asset` and `Bootstrap.unity` (generated by the setup tool, see below).
- Editor tooling: **Desi Merge → Setup → Create Foundation Assets**, **Desi Merge → Build → Android Development APK**.
- Docs: Architecture, CodingStandards, TestingStrategy, PhaseStatus.

### Verification actually run

The Editor had the project open (lockfile held), so batch-mode checks ran on a copy of the project
(Assets, Packages, ProjectSettings, Library) in a temporary folder.

| Check | Command | Result |
|---|---|---|
| Compile (Editor, all assemblies) | via test runs below; also open Editor log | No `error CS` in either |
| EditMode tests | `Unity -batchmode -runTests -testPlatform EditMode` | **50 / 50 passed**, exit 0 |
| PlayMode tests | `Unity -batchmode -runTests -testPlatform PlayMode` | **5 / 5 passed**, exit 0 |
| Setup tool | `-executeMethod DesiMerge.Editor.FoundationSetup.CreateFoundationAssets` | Passed on 3rd run after two bugs fixed (below) |
| Android build (dev, IL2CPP ARM64, Bootstrap + SampleScene) | `-buildTarget Android -executeMethod DesiMerge.Editor.BuildCommands.BuildAndroidDevelopment` | **Succeeded**, exit 0, `DesiMerge-dev.apk` 41 MB (~70 min first IL2CPP build). Report counted 2 errors, both Unity package editor assets failing to import in the copy (see noise below), none from project code |
| Install/run on a device | — | **Not run** (no device/emulator checked) |

Bugs found and fixed during verification:
1. Setup tool threw `Cannot create a new scene additively with an untitled scene unsaved` → now creates the scene in
   Single mode after offering to save, then restores previously open saved scenes.
2. Bootstrap scene saved with an empty `config` reference because opening a scene in Single mode unloaded the
   previously loaded GameConfig → config is now loaded after the scene is created.

Environment noise (not project issues): the copy's long temp path pushed some `Library/PackageCache` files past
Windows' 260-character path limit (`DirectoryNotFoundException` for VisualScripting, ShaderGraph, URP and Input System
editor files; a `Host type is not matching any asset type … TraceVirtualOffset.urtshader` error followed from that),
and Unity logged a licensing token warning. None affected compile, tests, or the build result. A build from the real
project path should not show them; confirm with the manual build step below.

### Manual steps still required (in the open Editor)

1. Focus the Editor and let it import; confirm the Console has no errors.
2. Run **Desi Merge → Setup → Create Foundation Assets** once. The scene and config already exist, so this only puts
   `Assets/_Project/Scenes/Bootstrap.unity` at build index 0 (it was not written to `ProjectSettings` from outside the
   Editor to avoid clobbering the Editor's in-memory settings). Confirm in File → Build Profiles → Scene List.
3. Open `Bootstrap.unity`, select **Bootstrap**, confirm *Config* references `GameConfig`. Press Play: Console shows
   `[Analytics] app_started` and no errors.
4. Window → General → Test Runner: run EditMode and PlayMode to confirm in your Editor.
5. Optional: switch the platform to Android and run **Desi Merge → Build → Android Development APK**
   (`Builds/Android/DesiMerge-dev.apk`), install it on a device, and check it launches to a black screen without crashing
   (`adb logcat -s Unity` should show `[Analytics] app_started`).
6. Player Settings (when ready, before any store build): company name, application identifier
   (e.g. `com.<company>.desimerge`), version, icons; confirm IL2CPP + ARM64 and target API level.
7. Initialize Git with a Unity `.gitignore` (excluding `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, `*.csproj`, `*.slnx`).

### Not done (by design)

- Gameplay, UI, real audio, monetization/analytics SDKs, backend/networking.
- Template content (`Assets/Scenes`, `Assets/Settings`, `Assets/Welcome`) left in place; see Architecture.md.
