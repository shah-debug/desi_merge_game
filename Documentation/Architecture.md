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
      Gameplay/                  Drop/merge loop: rules, resolver, session, board, drop input, camera fit, scene entry point.
      UI/                        HUD, main menu, settings panel, safe area, responsive scaling, popups, sparkles, UITheme.
        Animation/               Lightweight tween components (ScalePunch, PanelTransition, EntranceAnimation, ButtonFeedback, Ease).
      Editor/Content/            Generators and builders for art, audio, theme, prefabs and scenes (Phase 3).
      Editor/                    DesiMerge.Editor.asmdef — setup and build tooling.
    Scenes/                      Bootstrap.unity (0), MainMenu.unity (1), Gameplay.unity (2).
    Prefabs/                     MergeItem.prefab (pooled physics item, visual child).
      UI/                        ThemedButton.prefab, SettingsPanel.prefab.
    Art/Placeholder/             Generated Circle.png / Square.png item sprites (not final art).
    Art/UI/                      Generated UI sprites: RoundedPanel, PaintedFrame, TruckArtPattern, Flower, Sparkle, PauseIcon.
    Art/Materials/               Sparkle.mat (merge particles).
    Audio/                       AudioLibrary.asset; Music/*.wav and Sfx/*.wav are generated placeholders.
    Animations/                  Empty: UI motion is code-driven (see "Animation"), no Animator clips.
    ScriptableObjects/           GameConfig, GameplayConfig, MergeProgression, MergeItems/01_patti … 10_bus.
    Settings/Physics/            MergeItem.physicsMaterial2D.
    Settings/UI/                 UITheme.asset (palette, sprites, font, sizes).
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
| `ISettingsService` | Music/SFX volume (clamped 0–1) and mute, haptics (stored only). First run uses `GameConfig` defaults (copied, never mutated). Schema v2 adds mute; v1 saves load with mute off. | `SettingsService` |
| `IAudioService` | Play SFX by `SfxCue` (optional pitch), music by `MusicTrack` (same track never restarts), pause/stop music. Applies volume and mute settings. | `AudioService` (AudioSources on the bootstrapper), `SilentAudioService` (tests / no library) |
| `ISceneNavigator` | Load MainMenu / Gameplay; ignores requests while a load is in progress. | `SceneNavigator` |
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

### Scene layout (`Gameplay.unity`, generated by `Desi Merge → Setup → Create All Content`)

Orthographic camera size 8 (16 units tall; `CameraFit` enlarges it on narrow phones) at the origin; `Board` at
y = −7 (container 6.5 × 9, floor near the bottom of the screen); drops at board y = 10.5 (world 3.5); `HUD` canvas
(see "Layout" below); `EventSystem` with `InputSystemUIInputModule` (the project uses the new Input System only). Played directly in the Editor without Bootstrap, the entry point falls back to temporary
in-memory development services (Editor/development builds only) so the scene is testable on its own.

## UI, audio and flow (Phase 3)

### Scene flow

```
Bootstrap ──(GameConfig.firstSceneName = "MainMenu")──▶ MainMenu ──Play──▶ Gameplay
                                                           ▲                 │
                                                           └──Main Menu──────┘ (from Pause or Game Over)
```

- `ISceneNavigator` (in `GameServices`) loads scenes by the names in `GameConfig` and ignores requests while loading,
  so double taps on Play / Main Menu can't start two loads. The bootstrapper injects every loaded scene's
  `ISceneEntryPoint` as before (`MainMenuEntryPoint`, `GameplayEntryPoint`).
- Game state: menu entry sets `MainMenu`; gameplay `Begin` sets `Playing`; pause `Paused`; game over `GameOver`;
  leaving sets `MainMenu`. All transitions go through the existing `GameStateMachine`.
- Played directly in the Editor (no Bootstrap), each entry point builds `DevelopmentServices`: in-memory saves and an
  AudioService hosted on the entry point's own GameObject (destroyed with the scene, so no duplicate music players).
  Editor/development builds only.

### Pause, game over, restart, menu (in `GameplayLoop`)

| Action | Effect |
|---|---|
| Pause | Only from `Playing` and not over/leaving. `Time.timeScale = 0` (stops physics, merges and the game-over timer), drop input off, music paused, pause panel shown. |
| Resume | Only from `Paused`. Time 1, input on, music resumed, panel hidden. |
| Game over | Once per session (unchanged rule). Board frozen, input off, score submitted + saved, music stopped, game-over cue, results panel with the real final score / new best. |
| Restart | From pause or game over. Clears board and session, restores time, gameplay music, instructions shown. No re-subscription. |
| Main Menu | Once (`_leaving` guard). Restores time, stops input, freezes board, saves pending data, state `MainMenu`, navigator loads the menu. An abandoned run is **not** submitted as a final score. |
| Dispose (scene unload) | Restores `Time.timeScale = 1` even if unloaded while paused; unsubscribes everything. |

Panel animations use unscaled time and never gate state: the state changes first, the panel just animates.
Repeated button taps are absorbed by the state guards (pause twice, resume when not paused, leave twice) and by
`PanelTransition.Hide` disabling interaction immediately.

### HUD and input

- `GameplayHud` implements `IGameplayView` and only presents/raises requests: score (punch on increase), best (from
  `IPlayerProgressService`, already persisted), next item, pause button, instructions (hidden after the first drop of a
  session), pause and game-over panels, "+N" popups and merge sparkles via `ShowMergeFeedback`. It is driven by the loop's
  events and calls; nothing polls score or state.
- **UI never blocks gameplay input by accident, and gameplay never steals UI taps:** drop input is raw pointer input
  (`DropController`), and the HUD exposes `BlocksPointer(screenPos)` for its top bar. A press that starts on the top bar
  (e.g. Pause) never aims or drops; a press that started while input was disabled (e.g. the Resume tap) can't drop.
- Merge pop: `MergeItem` has a visual-only child (sprite + label) with a `ScalePunch`; the collider's transform is
  never animated, so contacts are unaffected.

### Layout

- Canvases: Screen Space Overlay, `CanvasScaler` 1080 × 1920 matched to **width** (portrait-first: taller phones get
  more height). `SafeAreaFitter` fits content to `Screen.safeArea` (notches, rounded corners) and re-applies only when
  the rect size changes. `ResponsiveScaler` shrinks a 1080 × 1920 design frame (menu) or the top bar (HUD) uniformly on
  shorter screens such as 3:4 tablets. `CameraFit` grows the orthographic view on narrow phones (19.5:9, 20:9) so the
  whole board stays visible. Touch targets are ≥ 120 reference px (pause 150, main buttons 150–210 tall).
- Decorative backgrounds (pattern, bands) are outside the safe area or behind the board at low alpha and never take
  raycasts.

### Look (UITheme)

`UITheme` holds the truck-art palette (magenta, teal, saffron, royal blue on cream), sprites, font and sizes. All
screens are generated from it by `Editor/Content` (`UiFactory` + the `ThemedButton` / `SettingsPanel` prefabs), so a
palette change plus "Rebuild Generated Scenes and Prefabs" restyles everything. All art is generated by code
(`UiArtGenerator`): rounded panel, painted frame with dotted inner border, a tileable diamond-lattice pattern with
flowers and dots, a flower motif, sparkle and pause icon. Typography is Unity's built-in font (bold + outline); no
font files are bundled.

### Animation

Code-driven, no tween package and no Animator: `ScalePunch` (score, next item, merge pop, game-over title),
`PanelTransition` (fade + card overshoot), `EntranceAnimation` (menu stagger), `ButtonFeedback` (press scale + tap
sound), `ScorePopupLayer` (pooled "+N" texts). Each component enables its `Update` only while animating and disables
itself afterwards. Merge sparkles use one `ParticleSystem` with `Emit(EmitParams)` (no per-merge objects); if the
sparkle material or particle system is missing, merges simply play without sparkles.

### Audio

`AudioService` owns one looping music `AudioSource` and a round-robin pool of SFX sources (6) on the bootstrapper's
DontDestroyOnLoad object, so changing scenes never adds players and the same track continues instead of restarting.
Volumes = clip base volume × channel volume, 0 when muted (`AudioMix`), updated through `ISettingsService.Changed`.
Missing clips log one warning per cue and stay silent. Cues: drop, merge (pitch rises with the created level),
button tap, game over; music: menu, gameplay. Clips are mapped in `AudioLibrary` (referenced by `GameConfig`).
All clips are **generated placeholders** (`AudioSynth`: pentatonic plucks over a drone with a dholak-like pulse);
replace the WAV files at the same paths to upgrade without code changes.

### Settings screen

`SettingsPanel` (prefab, opened from the main menu) binds sliders/toggles to the existing `ISettingsService`
(no second storage). Changes apply immediately through the audio service and are saved on close. There is no haptics
toggle because haptic feedback is not implemented.

## Persistence and progression (Phase 4)

### Storage

All player data is local: `Application.persistentDataPath/saves/` (app-private internal storage on Android), one
JSON file per area, each with a `schemaVersion`:

| File | Owner | Contents |
|---|---|---|
| `player_progress.json` | `PlayerProgressService` (schema 2) | best score, coins (unused), completed games, total merges, discovered item IDs, highest item ID, current/best streak, last played day |
| `achievements.json` | `AchievementService` (schema 1) | unlocked achievement IDs, rewarded achievement IDs |
| `daily_challenge.json` | `DailyChallengeService` (schema 1) | day number, challenge ID, progress, completed, claimed, latest day seen |
| `settings.json` | `SettingsService` (schema 2, unchanged system) | volumes, mute, haptics |

Content is referenced only by stable IDs (`MergeItemDefinition.Id`, achievement/challenge `id`), never by display
name or list position.

**Write path (`FileSaveStore`)**: write `key.json.tmp` → atomic `File.Replace` into `key.json`, keeping the replaced
file as `key.json.bak` (copy-then-swap fallback where `Replace` isn't supported). A crash mid-write leaves the previous
save intact.

**Read path (`JsonRepository`)**: current file → if unreadable, copy it to `key_corrupt.json` and try `key.json.bak`
→ if that also fails, defaults. Outcome is reported as `LoadOutcome` (NoSave / Loaded / RecoveredFromBackup /
ResetToDefaults); the main menu shows a notice for the last two. **Nothing is written during load**, so a damaged
file is only replaced once the player actually makes new progress, and a copy is always kept. Load never resets a
readable save.

**Failures**: `Save` never throws; it logs once, returns false, and the service keeps its dirty data for the next
flush. Gameplay continues.

**When data is written** (never per frame, never per UI refresh; services only write when dirty):
game over (`FlushAll`), leaving gameplay for the menu, settings panel close, daily reward claim, and app
pause / focus loss / quit via the bootstrapper's `AppLifecycleHook`.

**Migrations**: each service's load step upgrades in place. New fields get safe defaults automatically (JsonUtility
keeps initializer values for fields missing from older files); invalid values are clamped; blank IDs dropped.
v1 → v2 progress needs no conversion. To change a schema: bump the constant, add defaulted fields, add a
conversion step only for renamed/restructured data, and add a test that loads the previous version's JSON.

### Statistics and events

Gameplay reports facts; the progression services decide what they mean:

| Gameplay event (GameplayLoop) | Progress call | Guarantee |
|---|---|---|
| Item dropped | `RecordSeen(item)` | discovery once per ID |
| Merge resolved (board event, once per merge) | `RecordMerge(created)` | total merges +1, discovery once; final-level clear counts as a merge with no item |
| Game over (session `TryEnd`, once per session) | `SubmitFinalScore(score)` | completed games +1, best only if beaten, streak |
| Restart / Main Menu | nothing | a run abandoned via Main Menu is not a completed game; restart never clears records |

`IPlayerProgressService` raises `ItemDiscovered`, `MergeRecorded`, `GameCompleted` and `Changed`;
`AchievementService` and `DailyChallengeService` subscribe to those (no polling) and the loop forwards
discoveries / unlocks / daily completion to the HUD as toasts.

### Collection

Every `MergeProgression` level is shown; discovered items use their sprite, tint and name, others a dark silhouette
with "???". Items count as discovered when first dropped or created. IDs no longer present in the content are kept in
the save (in case content returns) but not shown or ranked; the highest item is re-derived from current content on
load. A missing sprite uses a placeholder.

### Daily challenge and streak rules

- **Day** = UTC calendar day (`DayKey`): changes at 00:00 UTC = 05:00 Pakistan time, independent of device time zone.
- **Today's challenge** = catalog entry at (day number mod count): offline, deterministic, same for everyone.
- **Effective day** = max(today, latest day seen): moving the device clock **back** never resets or repeats a day.
  Moving it **forward** is believed (a local clock cannot be secured against manipulation; documented, not hidden).
- Progress comes only from real events; resets when the effective day changes or the stored challenge ID is no
  longer in the catalog. Completed once per day (`Completed` event once).
- **Rewards**: `RewardDefinition` (type + amount) per challenge/achievement, granted exactly once and recorded by ID.
  Shipped content uses `None` because no economy is designed; `Coins` maps to the existing (unused) coin balance.
  The Claim button appears only when a challenge actually has a reward.
- **Streak** = consecutive UTC days with at least one completed game: first game 1; same day no change; next day +1;
  a missed day resets to 1 on the next game; a clock earlier than the last played day is ignored. Displayed as 0 once
  more than a day has been missed.

### Achievements

Data-driven (`AchievementCatalog`): `CompleteGames`, `TotalMerges`, `BestScore`, `DiscoverItem`, `DayStreak`.
Progress is derived from the persisted statistics (so it can't drift); only unlocked/rewarded IDs are stored.
Unlocks happen once, notify once (during play), and achievements already satisfied by existing stats unlock silently
at startup. Invalid definitions (missing/duplicate ID, unknown item, non-positive target) are skipped with a warning.

### UI

Main menu: today's challenge card (progress bar, status, day streak, Claim only with a reward), Play, Collection,
Awards, Settings, best score and a save-recovery notice. `CollectionPanel` and `AchievementsPanel` (prefabs) read
services when opened; the card refreshes from the daily service's `Changed` event. None of them touch save files.

## Template content

`Assets/Scenes/SampleScene.unity`, `Assets/Settings/*` (URP pipeline, 2D renderer, Input System actions, volume
profile) and `Assets/Welcome/*` came from the Unity 2D template. They are kept because the URP assets are referenced
by Graphics/Quality settings and the Input actions by Input System project settings. Moving them is safe only from
inside the Editor (to preserve references) and is deferred. `Assets/Welcome` and `SampleScene` can be removed once the
team agrees.

## Known gaps / next decisions

- Art and audio are generated placeholders; items are coloured circles with text labels.
- Starting Play Mode from MainMenu or Gameplay uses the in-memory development fallback (scores not saved). Start from
  Bootstrap to use real saves.
- Player Settings (company name, application identifier, IL2CPP/ARM64, target API) still at template defaults.
