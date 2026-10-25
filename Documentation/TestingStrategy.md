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

## Future

- CI (e.g. GameCI) running both suites and the Android build on each push — once the project is in Git.
- On-device smoke test checklist per release.
