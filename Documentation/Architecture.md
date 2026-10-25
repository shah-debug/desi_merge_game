# Desi Merge — Architecture

Desi Merge is an offline-first 2D physics merge puzzle game for Android, built with Unity 6 (6000.6.7f1, URP 2D) and C#.
This document describes the foundation established in Phase 1. Update it whenever a structural decision changes.

## Goals

- **Offline-first.** The game is fully playable with no network. No backend, database, or networking in core code.
- **Testable.** Rules and state live in plain C# classes that can be unit tested without scenes.
- **Explicit dependencies.** Services are constructed in one place and passed in. No `FindObjectOfType`, no service-locator singletons.
- **Small surface.** Add an abstraction only when there is a second implementation or a test that needs it.

## Folder layout

```
Assets/
  _Project/                      All game-owned content lives here.
    Scripts/
      DesiMerge.Runtime.asmdef   Single runtime assembly (namespaces per area below).
      Core/                      Game state machine, service container, composition root, scene entry points.
      Configuration/             ScriptableObject configuration (GameConfig, GameplayConfig, MergeProgression, MergeItemDefinition).
      Persistence/               Local save storage (ISaveStore, FileSaveStore, JsonRepository).
      Progression/               Player progress (high score, coins, games played).
      Settings/                  Player preferences (volumes, haptics).
      Audio/                     IAudioService and cue identifiers.
      Analytics/                 IAnalyticsService and event names (local only).
      Monetization/              IRewardedAdService and result types (no SDK).
      Platform/                  Application lifecycle hooks (pause/resume/quit).
      Gameplay/                  Drop/merge loop: rules, resolver, session, board, drop input, scene entry point.
      UI/                        GameplayHud (minimal test HUD). Screens come later.
      Editor/                    DesiMerge.Editor.asmdef — setup and build tooling.
    Scenes/                      Bootstrap.unity (build index 0), Gameplay.unity (index 1).
    Prefabs/                     MergeItem.prefab (pooled physics item).
    Art/Placeholder/             Generated Circle.png / Square.png placeholder sprites (not final art).
    Audio/ Animations/
    ScriptableObjects/           GameConfig, GameplayConfig, MergeProgression, MergeItems/01_patti … 10_bus.
    Settings/Physics/            MergeItem.physicsMaterial2D.
    Tests/
      EditMode/                  DesiMerge.Tests.EditMode.asmdef
      PlayMode/                  DesiMerge.Tests.PlayMode.asmdef
  Scenes/ Settings/ Welcome/     Unity 2D template content — left in place (see "Template content").
Documentation/                   This folder.
```

Empty folders contain a `.gitkeep` so they survive version control.

### Assemblies

| Assembly | Contents | References |
|---|---|---|
| `DesiMerge.Runtime` | Everything under `Scripts/` except `Editor/` | Unity, `Unity.InputSystem`, `UnityEngine.UI` (both already in the template) |
| `DesiMerge.Editor` | Editor tooling (Editor platform only) | Runtime, `Unity.InputSystem`, `UnityEngine.UI` |
| `DesiMerge.Tests.EditMode` | Unit tests (Editor platform only) | Runtime, Test Framework |
| `DesiMerge.Tests.PlayMode` | Runtime integration tests | Runtime, Test Framework |

One runtime assembly is deliberate: areas are separated by namespace and folder, which is enough at this size and avoids
asmdef churn. Split an area into its own assembly only when there's a concrete reason (compile times, or enforcing that
Gameplay cannot reference UI). `AssemblyInfo.cs` grants `InternalsVisibleTo` to Editor and tests for test hooks.

## Runtime composition

```
Bootstrap.unity (build index 0)
 └─ "Bootstrap" GameObject
     ├─ GameBootstrapper      composition root, DontDestroyOnLoad
     └─ AppLifecycleHook      added at runtime; forwards pause/resume/quit

GameBootstrapper.Awake
  ├─ validates GameConfig
  ├─ sets Application.targetFrameRate (Android defaults to 30)
  ├─ FileSaveStore(persistentDataPath/saves)
  ├─ GameServicesFactory.Create(config, store, Debug.isDebugBuild) → GameServices
  ├─ AppLifecycleHook.Paused/Quitting → GameServices.FlushAll()
  └─ SceneManager.sceneLoaded → SceneEntryPoints.InitializeAll(scene, services)
GameBootstrapper.Start
  └─ loads GameConfig.FirstSceneName (if set)
```

- **`GameServices`** is a plain container of the application-lifetime services. It has no static accessor.
- **`GameServicesFactory`** is the only place that picks concrete implementations (e.g. simulated ads in development
  builds, unavailable ads in release builds).
- **`ISceneEntryPoint`**: each scene has one MonoBehaviour implementing this. When the scene loads, the bootstrapper
  searches *that scene's* hierarchy for entry points and calls `Initialize(GameServices)`. The entry point wires the
  scene's own objects via serialized references and constructor/method injection. This replaces global lookups.
  `Initialize` runs after the scene's `Awake`/`OnEnable` and before `Start`.
- A second bootstrapper (e.g. Bootstrap scene loaded twice) destroys itself.

## Services

| Interface | Responsibility | Phase 1 implementation(s) |
|---|---|---|
| `IGameStateService` | Top-level flow: Boot → MainMenu → Playing ⇄ Paused → GameOver. Table-driven; invalid transitions return false. | `GameStateMachine` |
| `IPlayerProgressService` | High score, coins, games played. Validates inputs, saturates on overflow, clamps tampered data. Dirty-tracked; `Flush()` persists. | `PlayerProgressService` |
| `ISettingsService` | Music/SFX volume (clamped 0–1), haptics. First run uses `GameConfig` defaults (copied, never mutated). | `SettingsService` |
| `IAudioService` | Play SFX by `SfxCue`, music by `MusicTrack`. Implementation applies volume settings. | `SilentAudioService` (no-op) |
| `IAnalyticsService` | Record named events. Must never throw or block; never networked in offline phases. | `LogAnalyticsService` (dev), `NullAnalyticsService` |
| `IRewardedAdService` | Provider-agnostic rewarded ads. Callback invoked exactly once. Game must work when unavailable. | `SimulatedRewardedAdService` (dev), `UnavailableRewardedAdService` (release) |
| `IAppLifecycle` | Pause/resume/quit events. | `AppLifecycleHook` |
| `ISaveStore` | Raw key → string storage. | `FileSaveStore`, `InMemorySaveStore` (tests) |

## Persistence

- Each data object (`PlayerProgressData`, `SettingsData`) is a `[Serializable]` class with a `schemaVersion` field,
  serialized with `JsonUtility` through `JsonRepository<T>`.
- `FileSaveStore` writes one `<key>.json` per key under `Application.persistentDataPath/saves`, writing to a `.tmp`
  file first and swapping it in, so a killed process does not corrupt the previous save.
- Keys are restricted to `[A-Za-z0-9_-]` (no path traversal).
- Unreadable/corrupt data loads as defaults and the bad payload is kept as `<key>_corrupt` for diagnosis.
- Saves are flushed on **application pause** (the last reliable callback on Android) and quit. Gameplay code may also
  call `Flush()` at natural checkpoints (e.g. game over).
- Schema migrations go in each service's `Sanitize` step, keyed on `schemaVersion`.

## Configuration

`GameConfig` (ScriptableObject, `Assets/_Project/ScriptableObjects/GameConfig.asset`): target frame rate, first scene,
default settings, and development-only toggles for simulated ads and analytics logging. `Validate()` returns a list
of problems, which the bootstrapper logs as errors. Gameplay tuning will get its own assets rather than growing this one.

## Offline-first and dependency policy

- Phase 1 added **no packages**. Packages in `manifest.json` are the Unity 2D template defaults.
- No code under `_Project` uses `UnityWebRequest`, sockets, or any SDK.
- Monetization and analytics SDKs, if added later, live behind the existing interfaces and are chosen in
  `GameServicesFactory`. Gameplay never references an SDK type.

## Gameplay (Phase 2)

### Data

- **`MergeItemDefinition`** (one asset per level): stable `id`, provisional display name, placeholder sprite + tint,
  radius (collision and visual), mass, score value (awarded when the item is *created* by a merge).
- **`MergeProgression`**: the ordered list of definitions. Level = index; the next level is index + 1; the last entry
  is the final level. This list is the **single source of truth** for the merge chain — definitions deliberately do
  not hold a separate "next" reference that could disagree with it. Also holds how many low levels can be dropped
  (default 5: Patti … Roti). `Validate()` reports missing/duplicate/repeated entries, missing ids or sprites,
  non-positive radii and an invalid spawnable count.
- **`GameplayConfig`**: board size, drop height and cooldown, spawn seed, danger line, game-over delay and spawn
  grace, final-level rule and bonus, physics material, gravity scale, damping. `Validate()` includes the
  progression's checks plus geometry (danger line ≤ container top, drop above danger line, every item fits the width).

Initial progression: Patti → Nimbu → Pyaz → Tamatar → Roti → Aam → Chai → Mango Shake → Rickshaw → Bus
(provisional names, radii 0.30 → 1.45, scores 1, 3, 6, 10, 15, 21, 28, 36, 45, 55).

### Responsibilities

| Type | Kind | Responsibility |
|---|---|---|
| `MergeRules` | static | Decides whether two levels merge and into what, including the final-level rule. |
| `MergeResolver<T>` | plain C# | Queues contact reports during physics, resolves them once per step in report order. Marks both items consumed *before* the handler runs, so duplicate callbacks and one item touching several equals can only merge once. Items created while resolving wait for the next step (no recursion). |
| `GameSession` | plain C# | Score, merge count, `IsOver`. Only place score changes. `TryEnd()` succeeds once. |
| `ScoreRules` | static | Merge awards the created item's score; final clear awards the configured bonus. |
| `SpawnQueue` | plain C# | Current + next item, seeded `System.Random` (seed 0 = random per session). |
| `DropRules` | static | Clamp drop x inside the walls for the item's radius; `CanDrop` (not over, state Playing, cooldown elapsed). |
| `GameOverRule` | static | The game-over condition (below). |
| `MergeItem` | MonoBehaviour | Pooled body: applies definition (sprite, tint, scale, mass, material), reports `OnCollisionEnter2D` with other items to the board. No rules. |
| `MergeBoard` | MonoBehaviour | Builds walls/floor/danger line from config, owns the `ObjectPool<MergeItem>` and active list, resolves merges in `FixedUpdate`, checks the danger line, raises `ItemMerged` / `FinalLevelCleared` / `DangerTimeExceeded`. Knows nothing about score, UI or services. |
| `DropController` | MonoBehaviour | Event-driven pointer input (`<Pointer>/position`, `<Pointer>/press` — mouse and touch, no `Update` polling), clamps aim, shows a **visual-only preview** (SpriteRenderer, no Rigidbody/Collider), raises `DropRequested`. Does not decide whether a drop is allowed. |
| `GameplayLoop` | plain C# | Orchestrator: drop request → `CanDrop` → spawn → advance queue; board events → score; danger → single game over (freeze board, disable input, submit/flush progress, analytics, state → GameOver, HUD); restart. Subscribes once in its constructor, unsubscribes in `Dispose`. |
| `IGameplayView` / `GameplayHud` | interface / MonoBehaviour | Score, best, next item, game-over panel, restart button, configuration error text. The loop only talks to the interface. |
| `GameplayEntryPoint` | MonoBehaviour, `ISceneEntryPoint` | Validates config and scene references, initializes board and drop controller, creates and disposes the loop. The only gameplay type that references a concrete UI type. |

Dependency direction: `GameplayEntryPoint` → `GameplayLoop` → (`MergeBoard`, `DropController`, `IGameplayView`,
`GameServices`). The board and drop controller never reference each other, the UI, or services.

### Frame flow

1. Pointer release → `DropController.DropRequested(x)` → `GameplayLoop.TryDrop` → `MergeBoard.SpawnLocal`.
2. Physics step → `MergeItem.OnCollisionEnter2D` → `MergeBoard.ReportContact` → `MergeResolver.Report` (queued only).
3. Next `MergeBoard.FixedUpdate` → `ResolvePending`: consumed items return to the pool, the next level spawns at the
   midpoint with the pair's average velocity → `ItemMerged` → `GameSession.AddMergeScore`.
4. Same `FixedUpdate` → danger-line check → possibly `DangerTimeExceeded` → `GameplayLoop.EndGame` (once).

### Rules chosen for Phase 2 (all configurable in `GameplayConfig`)

- **Game over:** an item whose top edge stays above the danger line (container top, 9 units above the floor) for
  **2 s continuously** ends the game. Items younger than **1.5 s** (just dropped from above the line, or just created
  by a merge) are ignored, so a normal drop or merge never ends the game. On game over the board freezes
  (`Rigidbody2D.simulated = false`), pending merges are discarded, input is disabled, and the event is raised once
  until restart.
- **Final level:** `ClearBoth` — two Buses touching are both removed and award a **100**-point bonus.
  Alternative `NoMerge` keeps them as stacking obstacles.
- **Drop:** 0.5 s cooldown; x clamped to `±(halfWidth − radius)`; only a press that *started* while input was enabled
  can drop, so tapping Restart never drops an item.
- **Pause:** drops are only accepted in `GameState.Playing`; no pause UI exists yet.

### Physics setup

`Rigidbody2D` Dynamic, interpolated, continuous collision detection; `CircleCollider2D` radius 0.5 on a prefab scaled
to the item diameter; shared `PhysicsMaterial2D` (friction 0.3, bounciness 0.05) on items and walls; gravity from the
project's Physics 2D settings (−9.81) × gravity scale 1.5. Gravity is never simulated manually. Items are pooled
(`UnityEngine.Pool.ObjectPool`) so merges don't allocate GameObjects.

### Scene layout (`Gameplay.unity`, generated by `Desi Merge → Setup → Create Gameplay Content`)

Orthographic camera size 8 (16 units tall) at the origin; `Board` at y = −7 (container 6.5 × 9, floor near the
bottom of the screen); drops at board y = 10.5 (world 3.5); `HUD` Screen Space Overlay canvas with
`CanvasScaler` 1080 × 1920, match 0.5 (portrait); `EventSystem` with `InputSystemUIInputModule` (the project uses the
new Input System only). Played directly in the Editor without Bootstrap, the entry point falls back to temporary
in-memory development services (Editor/development builds only) so the scene is testable on its own.

## Template content

`Assets/Scenes/SampleScene.unity`, `Assets/Settings/*` (URP pipeline, 2D renderer, Input System actions, volume
profile) and `Assets/Welcome/*` came from the Unity 2D template. They are kept because the URP assets are referenced
by Graphics/Quality settings and the Input actions by Input System project settings. Moving them is safe only from
inside the Editor (to preserve references) and is deferred. `Assets/Welcome` and `SampleScene` can be removed once the
team agrees.

## Known gaps / next decisions

- No real audio implementation, home screen or final art yet. Placeholder circles with text labels only.
- Starting Play Mode from the Gameplay scene uses the in-memory development fallback (scores not saved). Start from
  Bootstrap to use real saves.
- Player Settings (company name, application identifier, IL2CPP/ARM64, target API) still at template defaults.
