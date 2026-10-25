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
      Configuration/             ScriptableObject configuration (GameConfig).
      Persistence/               Local save storage (ISaveStore, FileSaveStore, JsonRepository).
      Progression/               Player progress (high score, coins, games played).
      Settings/                  Player preferences (volumes, haptics).
      Audio/                     IAudioService and cue identifiers.
      Analytics/                 IAnalyticsService and event names (local only).
      Monetization/              IRewardedAdService and result types (no SDK).
      Platform/                  Application lifecycle hooks (pause/resume/quit).
      Gameplay/                  (Phase 2+) merge items, spawner, physics, scoring.
      UI/                        (Phase 2+) screens and HUD.
      Editor/                    DesiMerge.Editor.asmdef — setup and build tooling.
    Scenes/                      Bootstrap.unity (+ gameplay scenes later).
    Prefabs/ Art/ Audio/ Animations/
    ScriptableObjects/           GameConfig.asset and future data assets.
    Settings/                    Project-owned render/input assets (future).
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
| `DesiMerge.Runtime` | Everything under `Scripts/` except `Editor/` | Unity only |
| `DesiMerge.Editor` | Editor tooling (Editor platform only) | Runtime |
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

## Template content

`Assets/Scenes/SampleScene.unity`, `Assets/Settings/*` (URP pipeline, 2D renderer, Input System actions, volume
profile) and `Assets/Welcome/*` came from the Unity 2D template. They are kept because the URP assets are referenced
by Graphics/Quality settings and the Input actions by Input System project settings. Moving them is safe only from
inside the Editor (to preserve references) and is deferred. `Assets/Welcome` and `SampleScene` can be removed once the
team agrees.

## Known gaps / next decisions

- No real audio implementation, UI, or gameplay yet (Phase 2+).
- Starting Play Mode from a non-Bootstrap scene has no services. Options for Phase 2: an editor setting that sets
  `EditorSceneManager.playModeStartScene` to Bootstrap, or a per-scene dev fallback.
- Player Settings (company name, application identifier, IL2CPP/ARM64, target API) still at template defaults.
