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

## Phase 3 coverage

- EditMode (`PresentationLogicTests`): settings mute persistence and v1 → v2 migration, `AudioMix` volume maths,
  `AudioLibrary` missing-clip handling, safe-area anchor maths (notch, invalid screen), `ResponsiveScaler` and
  `CameraFit` sizing across 9:16 / 19.5:9 / 3:4, easing curve endpoints.
- PlayMode:
  - `PauseAndFlowTests` (rig, injected time scale and fake navigator): pause stops time/input/music and blocks drops;
    resume restores; repeated pause/resume ignored; no pause after game over; game-over shows the real final score
    and stops music; restart from pause; Main Menu leaves exactly once, restores time and saves; dispose while paused
    restores time and unsubscribes; merge feedback once with the awarded points; instructions hide after the first drop;
    presses on HUD controls never drop; the Resume tap can't drop.
  - `AudioServiceTests`: fixed source count (no duplicate players), same track not restarted, music volume follows
    settings and mute, SFX mute, missing clips warn once, dispose unsubscribes.
  - `SettingsPanelTests`: controls show stored values, write to the existing settings service, follow external changes,
    persist on close.
  - `SceneFlowTests` (real generated scenes): MainMenu → Settings → Play (double tap ignored) → Gameplay (real loop,
    exactly one audio host) → drops → Pause/Resume (time scale) → Game over (final score text = session score) →
    Main Menu (double request ignored, time restored, no extra audio players).
- Screenshots (`DESIMERGE_CAPTURE_DIR`): menu, settings, gameplay, pause and game over at 9:16, 19.5:9 with a simulated
  notch (`SafeAreaFitter.SimulatedSafeArea`), and 3:4. Reviewed by eye; they are not assertions.

## Phase 4 coverage

All Phase 4 tests use `InMemorySaveStore` or a unique temp folder, and `ManualClock` for dates; none touch real saves.

- EditMode (`ProgressionTests`):
  - Save/load: new player defaults; file round-trip across a simulated restart; corrupt save recovered from backup
    (corrupt copy kept); unrecoverable save → defaults with nothing overwritten at load; write failure keeps changes
    and retries; v1 → v2 migration; settings and progress sharing one store.
  - Statistics: best score non-regression; one event and one count per merge / completed game.
  - Collection: discovery once and persisted; removed content IDs kept but ignored; highest item repaired.
  - Streak: same day / next day / missed day / broken display; clock moved backwards; UTC day boundaries.
  - Daily: progress and single completion; survives restart; resets next day; clock moved back doesn't reopen a day;
    reward claimable exactly once (also after restart); no-reward challenge isn't claimable; invalid/unknown content skipped.
  - Achievements: unlock + notify + reward once and persisted (no re-notify after restart); silent unlock from existing
    stats; progress from real statistics; unknown item / duplicate IDs skipped.
- PlayMode (`ProgressionIntegrationTests`, `ProgressionUiTests`): physics merge recorded once; drop discovers; game
  over counts once with best score and streak; restart keeps records; leaving to menu doesn't finalize; toasts once
  per discovery / achievement / daily completion; simulated app restart restores progress, achievements and daily;
  settings persist alongside. UI: collection discovered/locked/counter from saved data, sprite placeholder, achievement
  rows and stats, daily card updates from events and hides Claim without a reward.
- Changed expectation: `FileStore_Overwrite_...` now also expects `slot.json.bak` (intentional: backups).

## Phase 5 coverage

No test needs an ad network: ads come from deterministic fakes (`FakeAdProvider` in the PlayMode rig, which holds each
request until the test calls `Finish(result, times)`; `ScriptedProvider` in EditMode). Storage is in-memory only.

- EditMode (`PowerUpAndAdTests`):
  - Inventory: free uses from config and reset per game; free uses before saved charges; unknown IDs refused; saved
    charges persist across restart (free uses don't); corrupt save → defaults with the bad copy kept; tampered values clamped.
  - Rewards: grant applied once per grant ID (also after restart); unknown power-up / bad amount / over the maximum refused
    and not recorded; daily power-up reward granted exactly once; **interrupted claim** (grant saved, claim not) → claiming
    again doesn't duplicate; claim refused when full stays claimable; achievement reward retried once there is room.
  - Revived games: best score only, game and streak counted once; daily "score" sees the higher score, "games" doesn't double.
  - Ad coordinator: each request reported once with the provider's result (Completed / Skipped / Failed / NoFill /
    LoadFailed / NetworkUnavailable); duplicate callbacks ignored; concurrent request refused; not ready / unsupported
    starts nothing; provider exception → Failed; abandoned request ignores a late completion; shipped fallbacks.
  - Shuffle planning: inside the walls with the gap, no overlaps (3 seeds), refuses when it can't fit, deterministic per seed.
  - Cosmetics: selection needs an existing owned item, persists, unlocked by achievement; invalid saved selection → default.
- PlayMode (`PowerUpAndReviveTests`, real physics): Hammer removes exactly the tapped item with no score/merge; miss keeps
  waiting and Cancel costs nothing; one power-up at a time / pressing again cancels; refused while paused and after game
  over; pausing cancels targeting; empty board / used up / unknown refused; free uses reset on restart while a used saved
  charge stays used after an app restart; Shuffle keeps items inside, apart and with the same levels, and refuses (nothing
  moves, nothing used) when it can't fit; revive only after `Completed`; every failure result grants nothing and can be
  retried; duplicate callbacks revive once; one revive per game and the game counted once; revive clears the danger zone
  and doesn't end again within 5 s; Restart works with ads not ready or unsupported; app interruption during the ad;
  restart/leaving during the ad ignores the late reward.
- `SceneFlowTests` (generated scenes): power-up bar ids/counts, Hammer banner and selection, cancel without cost,
  Continue offered with the development (simulated) provider, revive keeps the score, second game over hides Continue.

## Future

- CI (e.g. GameCI) running both suites and the Android build on each push — once the project is in Git.
- On-device smoke test checklist per release.

## Phase 6 coverage

- `MergeBoardTests.StaleContactBetweenReusedPoolObjects_DoesNotMergeInTheSameStep`: regression for the phantom merge
  (failed before the fix).
- `ReleaseHardeningTests`: back button, app interruption, repeated restarts (no leaked items/subscriptions), merges
  after game over, and `Measure_BoardStepCostAndAllocations` — a logged measurement (`[Perf]` in the log), not a
  timing assertion, so it can't be flaky.
- `ServiceCompositionTests`: missing optional content still yields working offline services; `FlushAll` write failure.
- `SceneFlowTests`: menu back closes a panel; `OnApplicationPause` pauses gameplay on the real scene.
- Device-only checks are a manual checklist in `Phase6-QA-Report.md`.
