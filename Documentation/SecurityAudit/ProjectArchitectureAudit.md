# Desi Merge — Project Architecture Audit

Audit date: 2026-11-03. Read-only static inspection of the working tree at commit `79abace` (branch `main`, clean,
equal to `origin/main`). No code, scenes, settings or saves were changed. Runtime verification for this audit was limited to
reading the already-built APK manifest with `aapt2` (see DependencyAndPrivacyAudit.md); **no tests were run for this audit**.

## 1. Inventory

| Area | Contents (evidence) |
|---|---|
| Engine / target | Unity 6000.6.7f1, URP 2D, Android, IL2CPP ARM64 (`ProjectSettings.asset`: `AndroidTargetArchitectures: 2`, min SDK 26; built APK target SDK 36) |
| Scenes (build order) | `Bootstrap.unity` (0), `MainMenu.unity` (1), `Gameplay.unity` (2) — `ProjectSettings/EditorBuildSettings.asset` |
| Runtime scripts | 102 C# files, ~14,950 lines, assembly `DesiMerge.Runtime` (`Assets/_Project/Scripts`) |
| Editor scripts | 25 files, assembly `DesiMerge.Editor` (content generators, scene builders, build commands, `OfflinePermissionFilter`) |
| Tests | 42 files: `DesiMerge.Tests.EditMode`, `DesiMerge.Tests.PlayMode` |
| ScriptableObjects | `GameConfig`, `GameplayConfig`, `MergeProgression` + 10 item assets, `Achievements`, `Cosmetics`, `DailyChallenges`, `DailyRewards`, `Milestones`, `Missions`, `PowerUps`, UI theme |
| Localization | `Assets/_Project/Localization/Strings.txt` (TSV, 9 languages), `NotoNaskhArabic.ttf` (Urdu) |
| Third-party code | None. Only Unity packages (`Packages/manifest.json`); no Plugins folder, no custom Gradle/manifest files |

### Runtime modules

| Module | Key types | Role |
|---|---|---|
| Core | `GameBootstrapper`, `GameServices`, `GameServicesFactory`, `GameStateMachine`, `SceneNavigator`, `IClock`/`DayKey`, `DevelopmentServices` | Composition root, state, navigation, time |
| Persistence | `FileSaveStore`, `JsonRepository<T>`, `InMemorySaveStore`, `SaveKey` | Per-key JSON files with temp+backup rotation |
| Gameplay | `GameplayEntryPoint`, `GameplayLoop`, `MergeBoard`, `MergeResolver<T>`, `MergeItem`, `DropController`, `SpawnQueue`, `PowerUpController`, `ShufflePlanner`, rules classes | Drop/merge/score/game over/power-ups/revive |
| Progression | `PlayerProgressService`, `AchievementService`, `DailyChallengeService`, `DailyRewardService`, `MissionService`, `MilestoneService`, `Rewards` (`RewardClaims`, `ProgressRewardGranter`) | Stats, Diamonds (stored as `coins`), rewards |
| PowerUps | `PowerUpService` | Free uses per game + saved charges, idempotent grants |
| Cosmetics | `CosmeticService` | Skins owned/selected/bought with Diamonds, per-skin discoveries |
| Monetization | `IRewardedAdService`, `SimulatedRewardedAdService` (dev only), `UnavailableRewardedAdService` (release) | Revive boundary; no ad SDK |
| Analytics | `LogAnalyticsService` (dev only), `NullAnalyticsService` (release) | No network |
| Audio | `AudioService`, `SilentAudioService`, `AudioLibrary`, `AudioMix` | One music source + SFX voice pool, one AudioListener |
| Settings | `SettingsService` (schema v4, includes language) | Volumes, mutes, tutorial flag, language |
| Localization | `Loc`, `UrduShaper`, `LocalizedText` | Static translation table, Urdu shaping/RTL |
| UI | `MainMenuEntryPoint`, `GameplayHud`, `GameplayMenu`, `MenuPanel` and ~30 panels/rows, `OutsideTapClose`, `FocusShade`, `SplashScreen`, animation helpers | All screens |
| Platform | `AppLifecycleHook`, `BackButton` | Pause/focus/quit forwarding, Android back |

## 2. Entry points and lifetime

```
Bootstrap.unity
  └─ GameBootstrapper (DontDestroyOnLoad)
       ├─ GameServicesFactory.Create(config, FileSaveStore(persistentDataPath/saves), Debug.isDebugBuild?, audioHost)
       │     Settings → Loc.Initialize → Audio → Progress → PowerUps → RewardGranter → Achievements → DailyChallenges
       │     → Cosmetics → DailyRewards → Missions → Milestones
       ├─ AppLifecycleHook: Paused / FocusLost / Quitting → GameServices.FlushAll()
       └─ SplashScreen.Play() while LoadSceneAsync(MainMenu)
MainMenu.unity → MainMenuEntryPoint.Initialize(services) → panels bound to services → Play → Navigator.LoadGameplay
Gameplay.unity → GameplayEntryPoint.Initialize(services) → new GameplayLoop(...) → Loop.Begin()
                  OnDestroy → Loop.Dispose() (unsubscribes everything)
```

* Singletons: none in the classic sense. The bootstrapper is the only `DontDestroyOnLoad` object and owns `GameServices`;
  scene entry points receive services by method call (`ISceneEntryPoint`). `Loc` is a static class (global state).
* Scenes opened directly in the Editor or a development build use `DevelopmentServices` (in-memory store) — guarded by
  `#if UNITY_EDITOR || DEVELOPMENT_BUILD` in `GameplayEntryPoint.Start` (line 107) and `MainMenuEntryPoint` (line 208).
* State machine: `Boot → MainMenu → Playing ⇄ Paused → GameOver`; `GameplayLoop.EnterPlayingState` routes through MainMenu
  when no direct edge exists.

## 3. Dependency map (who subscribes to whom)

| Publisher → event | Subscribers |
|---|---|
| `PlayerProgressService.MergeRecorded / GameCompleted / FinalScoreRecorded` | `MissionService`, `AchievementService`, `DailyChallengeService`, `MilestoneService` |
| `PlayerProgressService.ItemDiscovered` | `GameplayLoop` (toast), achievements |
| `PowerUpService.Used / GameBegan` | `MissionService` only |
| `PowerUpService.Changed` | `GameplayLoop.RefreshPowerUps` |
| `AchievementService.Unlocked` | `GameplayLoop` (toast), `CosmeticService` (achievement-unlocked skins) |
| `CosmeticService.Changed` | `GameplayEntryPoint.OnSkinChanged`, menu/skins UI |
| `SettingsService.Changed` | `AudioService.ApplyVolumes`, `SettingsPanel.Refresh` |
| `Loc.Changed` (static) | every `LocalizedText` |
| `MergeBoard.ItemMerged / FinalLevelCleared / DangerTimeExceeded / ItemRemoving / Shuffled` | `GameplayLoop`, `PowerUpEffects` |
| `DropController.DropRequested / TargetRequested` | `GameplayLoop` |
| `IGameplayView` events (`GameplayHud`) | `GameplayLoop` |

All `GameplayLoop` subscriptions are removed in `Dispose()` (GameplayLoop.cs, `Dispose`); `GameServices.Dispose` disposes
audio, achievements, daily challenges and other subscribers.

## 4. Save files

`Application.persistentDataPath/saves/<key>.json` (+ `.json.bak`, transient `.json.tmp`, quarantined `<key>_corrupt.json`).
Keys: player progress, settings, power-ups, cosmetics, achievements, daily challenge, daily rewards, missions, milestones
(`SaveKey`). Format: `JsonUtility` JSON, plain text, no encryption or integrity check (expected for offline-first).

## 5. Reference-usage notes

Scripts referenced only through serialized fields in generated scenes/prefabs (e.g. `FocusShade`, `OutsideTapClose`,
`IconBeat`, `SplashScreen`) are generated by `Assets/_Project/Scripts/Editor/Content/*Builder.cs`; they are live at runtime
even though no runtime code calls `AddComponent` for them. No Resources folder, Addressables, or reflection-based loading
was found (`grep` for `Resources.Load`, `Addressables`, `Activator`, `GetType(` in runtime code returned nothing relevant).

## Audit checklist

| Module | Inspected |
|---|---|
| Bootstrap, services factory, lifecycle | Yes |
| Persistence (FileSaveStore, JsonRepository) | Yes |
| Gameplay loop, board, resolver, item, drop, power-ups | Yes |
| Progress, daily rewards, missions, power-up service, cosmetics, reward granting | Yes |
| Achievements, daily challenge, milestone services | Partially (claim paths and subscriptions; same `RewardClaims` pattern) |
| HUD, menus, navigation, back button, outside-tap close | Yes (handlers, pointer blocking, back paths) |
| Audio | Yes (music/SFX/pause paths) |
| Android settings, built APK manifest, packages | Yes |
| Localization | Partially (runtime API, table; on-device rendering not verified in this audit) |
| Performance | Code inspection only — no profiler data |
