# Desi Merge — Testing Strategy

## Tooling

- **Unity Test Framework 1.8.0** (`com.unity.test-framework`, already in `Packages/manifest.json`; no extra packages).
- Test assemblies:
  - `Assets/_Project/Tests/EditMode/DesiMerge.Tests.EditMode.asmdef` — Editor-only, fast unit tests.
  - `Assets/_Project/Tests/PlayMode/DesiMerge.Tests.PlayMode.asmdef` — runs in Play Mode for MonoBehaviour/lifecycle behaviour.
- Both use `UNITY_INCLUDE_TESTS` so they are excluded from player builds.

## What goes where

| Layer | Test type | Examples |
|---|---|---|
| Pure C# rules/state (state machine, progress, settings, ads stand-ins, factory) | EditMode | `GameStateMachineTests`, `PlayerProgressServiceTests` |
| Persistence (file IO, JSON, corruption) | EditMode, unique temp dir per test | `PersistenceTests` |
| MonoBehaviours, scene wiring, lifecycle | PlayMode | `BootstrapTests` |
| Gameplay physics/merging (Phase 2+) | EditMode for rules (merge table, scoring); PlayMode for physics interactions | — |
| Device behaviour (performance, pause/kill, input) | Manual on device, checklist in PhaseStatus | — |

Prefer EditMode: put logic in plain classes so most tests don't need Play Mode.

## Rules

- Tests are deterministic: no real time, no randomness without a seed, no network.
- Never read or write real player saves: use `InMemorySaveStore` or a `Path.GetTempPath()` subfolder deleted in TearDown.
- Expected log errors/warnings are declared with `LogAssert.Expect`; unexpected error logs fail the test.
- PlayMode tests destroy what they create (use `DestroyImmediate` in TearDown when a later test depends on it being gone,
  e.g. the bootstrapper singleton guard).
- Internal test hooks (`GameBootstrapper.Configure`, `GameConfig.SetForTests`) are `internal` and exposed via
  `InternalsVisibleTo`; don't make them public.

## Running tests

**In the Editor:** Window → General → Test Runner → EditMode / PlayMode → Run All.

**Command line** (the Editor must not have the project open):

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
& $unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults editmode-results.xml -logFile editmode.log
& $unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults playmode-results.xml -logFile playmode.log
```

Exit code 0 = all passed; results are NUnit XML. Do not pass `-quit` with `-runTests`.

**Android build check:**

```powershell
& $unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod DesiMerge.Editor.BuildCommands.BuildAndroidDevelopment -logFile build.log
```

Output: `Builds/Android/DesiMerge-dev.apk` (keep `Builds/` out of version control).

## Phase 1 coverage

- Game state: valid/invalid transitions, event payloads.
- Persistence: round-trip, overwrite without leftover temp files, delete, unsafe key rejection, defaults on missing data,
  corrupt data fallback and quarantine.
- Progress: high score semantics, coin spend/earn validation, overflow saturation, change events, dirty-flag flushing,
  reload, tampered-data clamping.
- Settings: config defaults on first run, defaults not mutated, clamping, change events, saved values win.
- Composition: default config valid, validation errors, dev vs. release implementation selection, config toggles,
  `FlushAll`, ad stand-in callbacks.
- PlayMode: bootstrapper builds services and persists across loads, duplicate guard, missing-config error path,
  flush on application pause, scene-scoped entry point injection.

## Phase 2 coverage

**Deterministic physics.** Gameplay PlayMode tests use `GameplayTestRig`, which switches
`Physics2D.simulationMode` to `Script`, disables the board's `FixedUpdate`, and drives a manual clock. Each
`Simulate()` call is exactly one `Physics2D.Simulate(0.02)` followed by `MergeBoard.Step(now)`, so contacts, merges
and game-over timing don't depend on frame rate or wall-clock delays. The rig restores the simulation mode on dispose.

- EditMode (`MergeLogicTests`, `GameplayRulesTests`, `GameplayConfigTests`):
  - Merge rules: same level upgrades, different levels never merge, final level per rule, invalid levels ignored.
  - Resolver: duplicate callbacks merge once, one item touching two equals merges once, consumed/self/null ignored,
    final clear, items created during resolve wait for the next step, clear drops pending contacts.
  - Session: score events, ends once, no score after end, restart clears.
  - Spawn queue: seeded determinism, range, advance, reset.
  - Drop rules: boundary clamping (incl. item wider than container), `CanDrop` across states and cooldown.
  - Game-over rule: delay, reset when below the line, spawn grace.
  - Config validation: missing progression/definition/id/sprite, duplicates, spawnable range, item too wide.
- PlayMode (`MergeBoardTests`, `GameplayLoopTests`, `GameplayEntryPointTests`), with real Rigidbody2D/Collider2D:
  - Same-level contact merges exactly once; different levels don't; three touching equals → one merge.
  - Merge results chain on a later step; final level cleared (ClearBoth) or kept (NoMerge); frozen board doesn't merge.
  - Danger line raises once after the delay; `Clear()` resets and pooled items work again.
  - Loop: drop clamped to walls at drop height, queue advance, cooldown, no drop while paused or after game over,
    score awarded once per merge, final bonus, game over once (single progress submit + flush), restart clears
    board/score/state and allows a second game over, repeated restarts don't duplicate subscriptions, dispose unsubscribes.
  - Entry point: missing references and invalid progression log errors and don't start; second initialize ignored.
- PlayMode smoke (`GameplaySceneSmokeTests`): loads the generated `Gameplay` scene, drops 8 items in real time, ends
  and restarts; and spawns one item of every level. This checks wiring only. With the environment variable
  `DESIMERGE_CAPTURE_DIR` set, it saves rendered PNGs (`gameplay-after-drops.png`, `gameplay-game-over.png`,
  `gameplay-all-levels.png`) for visual review. Skipped (ignored) if the scene isn't in Build Settings.

## Future

- CI (e.g. GameCI) running both suites and the Android build on each push — once the project is in Git.
- On-device smoke test checklist per release.
