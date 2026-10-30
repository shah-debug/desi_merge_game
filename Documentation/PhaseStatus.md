# Desi Merge — Phase Status

| Phase | Scope | Status |
|---|---|---|
| 1 — Foundation | Structure, service interfaces, persistence, tests, docs, build check | Complete (approved, committed `eb25fdb`) |
| 2 — Core gameplay | Drop/merge loop, physics, scoring, game over, test HUD | Complete (committed `3216c75`) |
| 3 — UI, art, animation, audio | Main menu, HUD, pause/game over, settings, feedback, audio | Complete (committed `24b863f`) |
| 4 — Persistence & progression | Robust saves, stats, collection, daily challenges, streaks, achievements | Complete (committed `f93f0bd`) |
| 5 — Power-ups, rewarded ads, monetization foundation | Hammer, Shuffle, rewarded-ad revive, ad boundary, cosmetics foundation | Complete (committed `8ae6060`) |
| 6 — QA hardening & RC validation | Audit, bug fixes, tests, builds, measurements | Complete (committed `8ae6060`; see `Phase6-QA-Report.md`) |
| 6.5 — Visual redesign & polish | Original item art, design system, HUD, screens, playfield, game feel, icon | Complete (committed `e2d162a`; see `VisualRedesignTestReport.md`) |
| 6.6 — Player experience, rewards, audio | Audio fix, splash, tutorial, menu, daily rewards, missions, ranks, reward popup, results, icons, SFX | **Complete — awaiting review** (see `Phase66_TestReport.md`) |
| 6.7 — Production readiness audit | Crash hunting, failure injection, stress, fixes, release gate | **Done — decision: NOT READY (high-priority issues remain)**; see `ProductionReadiness/FinalProductionAudit.md` |
| 7 | Not defined yet | Not started |

## Phase 6.6 — Production-Grade Player Experience (2026-10-30)

Docs: [`Phase66_Audit.md`](Phase66_Audit.md), [`AudioSystemAudit.md`](AudioSystemAudit.md), [`RewardAndMissionSystem.md`](RewardAndMissionSystem.md), [`GameScreenFlow.md`](GameScreenFlow.md), [`VisualDesignSystem.md`](VisualDesignSystem.md), [`VisualAssetManifest.md`](VisualAssetManifest.md), [`Phase66_TestReport.md`](Phase66_TestReport.md). Screenshots: `Screenshots/phase6.6-*.png`.

- **Audio bug fixed.** Root cause: no `AudioListener` in any scene (generated cameras never had one), so Unity rendered no sound. `AudioService` now owns exactly one listener on the persistent audio host. Added 7 sound cues; SFX are preloaded.
- **New:** branded splash (Unity splash off); 4-page first-game tutorial (skippable, shown once); redesigned menu with rank card and notification dots; 7-day **Daily Rewards**; **Missions** (3 daily, 3 weekly, completed tab, today's challenge); score **Ranks** (7 milestones); rewards on achievements (+3 new achievements); one reusable **reward popup**; a game-over **results summary**; a discovery toast with item art; collection hints; one-panel-at-a-time navigation with Android back for every screen.
- **Economy:** all rewards are real power-up charges (maximum 9 each). There are no coins on screen and no shop (deferred: no real currency or visible cosmetics).
- **Saves:** new `daily_rewards.json`, `missions.json`, `milestones.json`; `settings.json` schema 3 (`tutorialCompleted`). Nothing reset; the existing best score earns ranks at startup.
- **Regenerate:** **Desi Merge > Setup > Apply Phase 6.6 Experience** (rebuilds MainMenu/Gameplay/UI prefabs and the Bootstrap splash; data assets are kept).
- **Tests:** EditMode **203/203**, PlayMode **110/110** (final runs).
- **Android build:** release-config APK **succeeded** (32.5 MB, debug-signed, 0 errors). Not installed on a phone.

## Phase 6.5 — Visual Redesign and Game Polish (2026-10-28)

Docs: [`VisualRedesignAudit.md`](VisualRedesignAudit.md), [`VisualDesignSystem.md`](VisualDesignSystem.md),
[`VisualAssetManifest.md`](VisualAssetManifest.md), [`VisualRedesignTestReport.md`](VisualRedesignTestReport.md).
Screenshots: `Screenshots/phase6.5-*.png`.

- **Art:** 10 original illustrated items (paan leaf → truck-art bus) drawn by Editor code, sized inside each collider
  circle. 17 icons, panels, buttons, truck-art ornament and border strip, patterned walls, dashed danger line, background
  gradient, and a launcher icon. Display font: Lilita One (SIL OFL, license included).
- **Screens:** new top bar (score, best, next-item well, pause); icon power-up buttons with selected and unavailable
  states; pause card with Resume / Restart (with confirmation) / Main Menu; game-over card; main menu; Settings,
  Collection and Awards panels.
- **Feel:** merge pop with squash-and-stretch on the visual child only, merge ring burst, plus the existing sparkles,
  popups, button press and panel transitions.
- **Unchanged:** merge order, IDs, sizes, masses, scores, physics, saves and services. Board decorations have no
  colliders, and items always draw above them.
- **Fixed on the way:** the toast overlapped the board on 3:4 tablets (Phase 5 F12).
- **Tests:** EditMode **180/180**, PlayMode **103/103**, 0 skipped. No device testing.
- **Android build:** release-config APK **succeeded** (32.3 MB, debug-signed). Not installed on a phone.
- **Regenerate:** `Desi Merge > Setup > Apply Visual Redesign` (overwrites generated art, scenes and UI prefabs; keeps data).

## Phase 6 — QA Hardening and Release-Candidate Validation (2026-10-28)

Full report: [`Phase6-QA-Report.md`](Phase6-QA-Report.md).

- Fixed: phantom merge of reused pool objects in one physics step (High, regression test); landscape auto-rotation on a
  portrait game (High); Android back button (Medium); auto-pause when the app is backgrounded (Medium); template
  SampleScene removed from the build (Low); Windows test window 540×960.
- Tests (final runs): EditMode **174/174**, PlayMode **98/98**, 0 skipped, 0 project warnings.
- Builds: Android release-config APK **succeeded** (IL2CPP ARM64, 31.7 MB, debug-signed); Windows 64-bit **succeeded**;
  Windows release player booted headless without errors. **No Android device test.**
- Release blockers: application ID/company name, release keystore/AAB, device validation (and ad SDK + consent if ads ship).
- Verdict: technical RC for internal device testing; **not ready for store release**.

### Build commands (Unity Editor closed; run from the project folder in PowerShell)

```powershell
# Android release configuration → Builds\Android\DesiMerge-rc-unsigned-test.apk
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\HP\DesiMerge" -buildTarget Android -executeMethod DesiMerge.Editor.BuildCommands.BuildAndroidReleaseConfig -logFile build-android.log

# Android development (simulated ads) → Builds\Android\DesiMerge-dev.apk
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\HP\DesiMerge" -buildTarget Android -executeMethod DesiMerge.Editor.BuildCommands.BuildAndroidDevelopment -logFile build-android-dev.log

# Windows 64-bit → Builds\Windows\DesiMerge.exe
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\HP\DesiMerge" -buildTarget Win64 -executeMethod DesiMerge.Editor.BuildCommands.BuildWindows -logFile build-windows.log
```

A build is a snapshot: rebuild after code changes before handing it to testers. `Builds/` is git-ignored.

## Phase 5 — Power-ups, Rewarded Ads and Monetization (2026-10-28)

### Audit (before editing)

- Git: clean working tree at `f93f0bd` (Phase 4 committed). Unity Editor closed, so batch mode used the real project.
- Phase 4 verified as present (services, saves, catalogs, UI) and its tests passed again in this phase's runs.
- Existing monetization (Phase 1): `IRewardedAdService` (`IsReady`, `Show(callback)`), `RewardedPlacement`
  (`ContinueAfterGameOver`, `DoubleCoins`), `RewardedAdResult`, `SimulatedRewardedAdService` (dev builds) and
  `UnavailableRewardedAdService` (release). Nothing used them yet. **No ad SDK** in `Packages/manifest.json` or Assets.
- Economy: a dormant `coins` balance exists (Phase 1) but nothing earns or spends coins; no shop or currency design.
  Therefore no currency was introduced; power-ups use per-game free uses plus saved charges from daily rewards.
- Game over was final (`EndGame` counts the game, saves, shows the panel); the board had no way to remove one item
  or reposition items; there was no snapshot/command history for Undo.

### Implemented

| Feature | Status |
|---|---|
| **Hammer** | Implemented. Select → tap an item → exactly that item is removed. Cancel / press again / pause / game over cancel without cost. No score, not a merge. |
| **Shuffle** | Implemented as a **restricted, documented version**: items are re-stacked into non-overlapping shelves inside the walls (largest first, random within a shelf), velocities cleared. Refused with "Not enough room to shuffle" (nothing moves, nothing used) if the stack would come within 0.4 units of the danger line. |
| **Undo** | **Deferred, disabled (no button).** A reliable rollback would have to restore positions, rotations, velocities, levels, score, merge count, the spawn queue *and* long-term statistics, discoveries, achievements, daily progress and saved files that each merge already updated. That is too large to do safely in this phase. |
| Usage limits | Per power-up in `PowerUps.asset`: `freeUsesPerGame` (Hammer 1, Shuffle 1; reset at every new game or restart, not saved; a revive keeps what's left) and `maxSavedCharges` (9). |
| Saved charges / rewards | Shipped daily challenges now reward 1–2 Hammer/Shuffle charges (Claim button). Grants are idempotent (stable grant IDs) and saved before the claim, so an interrupted claim never loses or duplicates a reward. Achievements still give no reward. No coins, no currency. |
| Rewarded-ad abstraction | `IRewardedAdService` gained `IsSupported`; results gained `NoFill`, `LoadFailed`, `NetworkUnavailable`. New `RewardedAdCoordinator` (`GameServices.Ads`): one request at a time, first callback wins, duplicates/late callbacks ignored, provider exceptions → Failed, abandon on restart/menu/unload. Gameplay and UI never call the provider. |
| **Real ad provider** | **Not integrated.** Release builds use `UnavailableRewardedAdService` (Continue is hidden). Development/Editor builds use `SimulatedRewardedAdService` (instant result; it is not a real ad). |
| Game-over revive | Continue (AD) on the game-over panel, with Loading / no-ad / cancelled / failed / offline messages; Restart and Main Menu always available. One revive per game (`maxRevivesPerGame`). Granted only on `Completed`. Safe state: items whose top is within 3 units of the danger line are removed, the rest get a fresh grace period; score, merges and stats kept; the game is counted once (`SubmitRevivedFinalScore` only updates the best). |
| Optional placements | Only the revive. No interstitials, banners, automatic or per-scene ad requests. `DoubleCoins` stays unused. |
| Cosmetics | **Foundation only**: data-driven definitions, ownership separate from selection, achievement unlocks, validation, `cosmetics.json` save. No catalog asset shipped, no shop UI, nothing visual reads the selection yet. No purchases or paid currency. |

### Files

- New runtime: `Scripts/Monetization/RewardedAdCoordinator.cs`; `Scripts/PowerUps/{PowerUpDefinition,PowerUpCatalog,PowerUpService}.cs`;
  `Scripts/Cosmetics/{CosmeticDefinition,CosmeticCatalog,CosmeticService}.cs`;
  `Scripts/Gameplay/{PowerUpController,ShufflePlanner,GameplayViewStates}.cs`; `Scripts/UI/PowerUpButton.cs`.
- Changed runtime: `Monetization/{IRewardedAdService,RewardedAdTypes,SimulatedRewardedAdService,UnavailableRewardedAdService}.cs`,
  `Core/{GameServices,GameServicesFactory}.cs`, `Configuration/{GameConfig,GameplayConfig}.cs`,
  `Gameplay/{GameplayLoop,IGameplayView,GameSession,MergeBoard,MergeItem,MergeResolver,DropController}.cs`,
  `Progression/{Rewards,AchievementService,DailyChallengeService,IPlayerProgressService,PlayerProgressService}.cs`,
  `Persistence/SaveKey.cs`, `Analytics/AnalyticsEvents.cs`, `UI/{GameplayHud,DailyChallengeCard,MainMenuEntryPoint}.cs`.
- Editor: new `Editor/Content/PowerUpContentBuilder.cs`; changed `ContentPaths`, `ContentSetup`, `GameplaySceneBuilder`,
  `ProgressionContentBuilder` (comment only).
- Assets: new `ScriptableObjects/Progression/PowerUps.asset`; changed `DailyChallenges.asset` (rewards),
  `GameConfig.asset` (`powerUps` reference); regenerated `Gameplay.unity` (power-up bar, Hammer banner, Continue
  button, taller game-over card), `MainMenu.unity` and the UI prefabs (rebuild; no intended changes there).
- Tests: new `EditMode/PowerUpAndAdTests.cs`, `PlayMode/PowerUpAndReviveTests.cs`; changed `PlayMode/GameplayTestRig.cs`
  (power-up catalog, `FakeAdProvider`, view fakes), `PlayMode/SceneFlowTests.cs`.
- Docs: this report, `Architecture.md` (Phase 5 section), `TestingStrategy.md` (Phase 5 coverage),
  `Screenshots/phase5-*.png`.
- **Dependencies: none added.** `Packages/manifest.json` unchanged.

### Save files added

`power_ups.json` (v1: saved charges per power-up ID + recently applied grant IDs) and `cosmetics.json` (v1). Same
repository rules as Phase 4 (backup, corrupt copy kept, no write at load, never throws). Existing saves need no
migration; the new fields in `RewardDefinition` default to empty.

### SDK, consent and policy (for when a real provider is added)

Not done in this phase, and **no compliance is claimed**. Before serving real ads: pick an SDK and follow its current
official Unity integration guide; implement `IRewardedAdService` in one class (report `Completed` only from the SDK's
verified reward callback, map no-fill/load/show/network errors to the matching results, call back on the main thread);
obtain consent where required (e.g. a UMP/CMP flow for GDPR/UK users) before requesting ads; complete Google Play's
Data safety form and Families/target-audience declarations, and configure child-directed / under-age treatment if
the audience includes children; consider server-side reward verification. Then switch `GameServicesFactory` to it.

### Verification actually run (real project, batch mode)

| Check | Result |
|---|---|
| Compile | 0 errors, 0 warnings from project code |
| Content rebuild (`ContentSetup.RebuildGeneratedScenesAndPrefabs`) | Exit 0; PowerUps.asset created, daily rewards set, GameConfig wired |
| EditMode | **172 / 172 passed** (139 existing + 33 new) |
| PlayMode | **92 / 92 passed** (70 existing + 22 new), 0 skipped; the scene flow test ran (not ignored) |
| Screenshots | Hammer selection (9:16), game over with Continue (9:16, 3:4), gameplay with power-up bar (3:4) reviewed; saved as `Documentation/Screenshots/phase5-*.png` |
| Live ads / real reward callbacks | **Not verified (no SDK integrated)** |
| Android build / device | **Not run** |

### Known limitations

- Shuffle can refuse on a crowded board (shelf packing is less dense than a settled pile), which is exactly when it
  is most wanted. Refusal costs nothing.
- Hammer has no extra "are you sure?" dialog; choosing a target is the confirmation and Cancel is always shown.
- Free uses are per game: restarting (which clears the board) gives them back.
- If a real ad provider never calls back, Continue stays on "Loading ad..." until the player taps Restart or Main
  Menu (both always work); there is no timeout.
- Merges made during the revived part of a game count as usual; items removed by a revive or Hammer are simply gone.
- On 3:4 tablets the toast (not scaled, as in Phase 4) briefly overlaps the drop-preview area; it never takes input.
- `DoubleCoins` placement and the coin balance remain unused. Cosmetics have no UI and no shipped content.
- The menu's save-recovery notice reports only the main progress file, not `power_ups.json`/`cosmetics.json`.

### Manual test instructions

1. Open `Bootstrap.unity` → Play (Editor = development build → simulated ads).
2. Drop a few items. Tap **HAMMER** (badge shows uses left): the banner appears; tap an item: it disappears and the
   badge drops to 0. Try again with 0: "No uses left this game". Tap Hammer, then **CANCEL**: nothing is used.
3. Tap **SHUFFLE**: items re-stack in rows; nothing pokes out of the walls. Fill the jar and try again: "Not enough room".
4. Let the jar overflow → Game Over shows **CONTINUE (AD)**. Tap it: the simulated provider completes at once, the top
   of the jar is cleared and play resumes with the same score. Overflow again: no Continue the second time.
5. To see release behaviour, turn off `GameConfig > Simulate Rewarded Ads`: no Continue button at all, Restart works.
   (Cancel/failure/offline messages are covered by the automated tests; there is no in-game switch for them.)
6. Main menu: today's challenge shows its reward ("Reward: +1 Hammer"); complete it and press **CLAIM**; the next
   game's Hammer badge shows 2 (1 free + 1 saved).
7. Saves: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\DesiMerge\saves\power_ups.json`.

## Phase 4 — Persistence, Collection, Daily Challenges and Progression (2026-10-27)

### Audit (before editing)

- Git clean at `24b863f` (Phase 3, committed by the user; contents identical to the Phase 3 output). Unity closed, so
  work and verification ran on the real project in batch mode. Last Editor compile: success.
- Existing persistence: atomic temp-file writes; corrupt data → defaults + `_corrupt` copy; progress v1
  (best, coins, games); settings v2. **Gaps found:** no backup file (a corrupt save became defaults and the next
  save overwrote the original file), a failed write **threw** (would have broken the game-over flow), no save on
  focus loss, no statistics/discovery/achievements/daily/streak. Coins existed only as an unused Phase 1 API and no
  economy is designed, so none was introduced.

### Implemented

- **Persistence hardening:** `.bak` of the previous version on every write; load order current → backup → defaults
  with `LoadOutcome`; nothing written at load; `Save` never throws (failures logged once, retried on the next flush);
  flush on focus loss added; game over now flushes all services.
- **Progress v2** (`player_progress.json`): completed games, total merges, discovered item IDs, highest item,
  current/best/last-day streak; v1 migrates automatically.
- **Statistics from real events:** drop → discovery; each resolved merge → +1 merge and discovery; game over →
  +1 completed game, best only if beaten, streak. Restart and Main Menu never finalize a game.
- **Collection:** panel with all configured items, discovered vs locked ("???" silhouette), counter, placeholder
  sprite for missing art; unknown/removed IDs preserved but ignored.
- **Daily challenges:** data-driven catalog (7 challenges: merges, score in one game, create item, finish games),
  UTC day key, effective day = max(today, latest seen), progress from events, completion once, optional reward
  claimable exactly once. Card on the main menu.
- **Streaks:** consecutive UTC days with a completed game (rules in Architecture.md).
- **Achievements:** data-driven catalog (8 achievements), unlock/notify/reward once, silent catch-up at startup,
  progress derived from statistics. Awards panel with statistics.
- **Feedback:** in-game toasts for new discoveries, achievement unlocks, daily completion; save-recovery notice on
  the main menu.

### Files

Created — runtime: `Core/IClock.cs` (IClock, SystemClock, ManualClock, DayKey);
`Progression/{Rewards, StreakRules, AchievementDefinition, AchievementCatalog, AchievementService, DailyChallengeDefinition, DailyChallengeCatalog, DailyChallengeService}.cs`;
`UI/{ToastPresenter, CollectionSlot, CollectionPanel, AchievementRow, AchievementsPanel, DailyChallengeCard}.cs`.

Created — editor: `Editor/Content/ProgressionContentBuilder.cs`.
Created — tests: `EditMode/ProgressionTests.cs`, `PlayMode/ProgressionIntegrationTests.cs`.
Created — assets: `ScriptableObjects/Progression/{Achievements, DailyChallenges}.asset`,
`Prefabs/UI/{CollectionPanel, AchievementsPanel}.prefab`.

Modified — runtime: `Persistence/{ISaveStore (+TryReadBackup), FileSaveStore (.bak), InMemorySaveStore, JsonRepository (LoadOutcome, non-throwing Save), SaveKey}.cs`;
`Progression/{PlayerProgressData (v2), IPlayerProgressService, PlayerProgressService}.cs`;
`Settings/SettingsService.cs` (uses load outcome; keeps changes if a write fails);
`Configuration/{GameConfig (content refs), MergeProgression (IndexOf)}.cs`;
`Core/{GameServices (+achievements, daily, clock, item content; FlushAll returns success), GameServicesFactory, GameBootstrapper (focus-loss flush)}.cs`;
`Platform/{IAppLifecycle, AppLifecycleHook}.cs` (FocusLost);
`Gameplay/{GameplayLoop (records drops/merges, flushes all at game over, toasts), IGameplayView (ShowToast)}.cs`;
`UI/{GameplayHud (toast), MainMenuEntryPoint (new panels, daily card, save notice)}.cs`.
**Unchanged:** merge resolution, physics, scoring rules, session, game-over rule.

Modified — editor: `Editor/Content/{ContentSetup, ContentPaths, MainMenuSceneBuilder, GameplaySceneBuilder}.cs`.
Modified — tests: `PlayMode/{GameplayTestRig, SceneFlowTests}.cs`; `EditMode/PersistenceTests.cs` (one assertion
intentionally updated: overwrites now leave `slot.json.bak`).
Regenerated: `Scenes/{MainMenu, Gameplay}.unity`, UI prefabs. `GameConfig.asset` gained the content references.

### Save format and storage

See Architecture.md → "Persistence and progression": four JSON files under `persistentDataPath/saves`, each with a
schema version, atomic writes with one backup, stable IDs only. No database, backend, cloud, account or package.

### Verification actually run (real project, batch mode)

| Check | Result |
|---|---|
| Compile | First attempt: 1 test-file error (ambiguous `Object` alias) — fixed. Final: 0 errors, 0 project warnings |
| Content rebuild | Exit 0, all references resolved |
| EditMode | **139 / 139 passed** (115 existing + 24 new) |
| PlayMode | **70 / 70 passed** (58 existing + 12 new) — first run 67/70: 3 failures from a bug in my new test helper (added a second RectTransform), not in game code; fixed and re-run |
| Screenshots | Menu (9:16, 3:4), Collection and Awards reviewed. Awards rows initially had the progress bar crossing the description text; moved under the progress number, rebuilt, re-tested (139/139, 70/70) and re-captured. Saved as `Documentation/Screenshots/phase4-*.png` |
| Android build / device | **Not run** |

### Known limitations

- Dates use the device clock; moving it **forward** gives new daily challenges early (documented; can't be prevented
  offline). Daily reset is 00:00 UTC (05:00 PKT), not local midnight.
- No reward economy: shipped challenges/achievements have no rewards, so there's no Claim button in practice.
- Merges in an abandoned run count toward totals and daily merges (they happened); the run itself isn't a completed game.
- A changed daily catalog order on the same day restarts that day's challenge.
- Playing MainMenu/Gameplay directly in the Editor uses in-memory saves (nothing persists between scenes); start from
  Bootstrap to test persistence.

### Manual test instructions

1. Open `Bootstrap.unity` → Play. First launch: menu shows today's challenge, "Finish a game to start a streak".
2. Play a game to Game Over. Watch the toasts ("New discovery …", "Achievement: First Game"). Main Menu → the
   challenge card and Best score update; **Collection** shows discovered items; **Awards** shows stats and unlocks.
3. Stop Play, press Play again (= app restart): everything above is restored.
4. Save files are in `%USERPROFILE%\AppData\LocalLow\DefaultCompany\DesiMerge\saves` (Editor/Windows). To test
   recovery: with Unity stopped, put garbage in `player_progress.json` → next launch restores from `.bak` and the
   menu says so. Delete both `player_progress.json` and `.bak` to start fresh.

## Phase 3 — UI, Art, Animation and Audio (2026-10-26)

### Audit (before editing)

- Git: clean at `3216c75` (Phase 2 committed by the user). Unity Editor closed, so this phase was built and verified
  **directly on the real project** in batch mode (no copy).
- Phase 2's open compile error ("FoundationSetup does not exist") was a stale Editor cache: after the user restarted
  Unity the Editor log showed "Tundra build success". Baseline on the real project before any change:
  **EditMode 96/96, PlayMode 33/33**. Phase 2's pending Build Settings step was done by the setup tool.
- Existing assets: generated item circles only; **no audio files, no fonts, no TextMesh Pro resources, no main menu,
  no settings UI**; audio was the no-op service. Gameplay (drop, merge, score, game over, restart) was working.

### Screens and components implemented

| Screen / feature | What it does |
|---|---|
| **Main menu** (`MainMenu.unity`) | Truck-art background (pattern + bands), "Desi Merge" title plate with flower motifs and tagline "Milao • Barhao • Jeeto", the real merge chain (read from `MergeProgression`), **Play** (loads the real Gameplay scene), **Settings**, best score (shown only when > 0). Staggered entrance animation. No collection/shop buttons (features don't exist). |
| **Settings** (`SettingsPanel.prefab`) | Music volume + mute, SFX volume + mute, bound to the existing `ISettingsService`; applied live, saved on close. No haptics toggle (haptics not implemented). |
| **Gameplay HUD** | Score (punch on increase), Best (from the existing persisted progress), Next item (punch on change), Pause button, first-drop instructions, "+N" score popups and coloured merge sparkles; visible board walls and danger line on a faint pattern backdrop. |
| **Pause panel** | Resume, Main Menu. Time scale 0, drops off, music paused. |
| **Game-over panel** | Title punch, actual final score, "NEW BEST!" badge when applicable, best score, Restart, Main Menu. |
| **Merge pop** | New merged item's visual child pops (collider untouched). |
| **Audio** | Menu and gameplay music, drop, merge (pitch rises with level), button tap and game-over cue through `AudioService`; separate music/SFX volume and mute. |
| **Safe area / responsive** | `SafeAreaFitter`, `ResponsiveScaler` (tablets), `CameraFit` (tall phones). |

### Files

Created — runtime scripts:
`Core/{ISceneNavigator, SceneNavigator, DevelopmentServices}.cs`,
`Audio/{AudioLibrary, AudioMix, AudioService}.cs`,
`Gameplay/CameraFit.cs`,
`UI/{UITheme, SafeAreaFitter (+SafeAreaMath), ResponsiveScaler, ScorePopupLayer, MergeEffects, SettingsPanel, MainMenuEntryPoint}.cs`,
`UI/Animation/{Ease, ScalePunch, PanelTransition, EntranceAnimation, ButtonFeedback}.cs`.

Created — editor scripts: `Editor/Content/{ContentPaths (+EditorRefs), UiFactory, UiArtGenerator, AudioSynth, ContentSetup, MainMenuSceneBuilder, GameplaySceneBuilder}.cs`.

Created — tests: `EditMode/PresentationLogicTests.cs`; `PlayMode/{PauseAndFlowTests, AudioAndSettingsUiTests, SceneFlowTests, SceneCapture}.cs`.

Created — generated assets (all original, made by code; **placeholders**):
`Art/UI/{RoundedPanel, PaintedFrame, TruckArtPattern, Flower, Sparkle, PauseIcon}.png`, `Art/Materials/Sparkle.mat`,
`Audio/Music/{menu_loop, gameplay_loop}.wav`, `Audio/Sfx/{drop, merge, button_tap, game_over}.wav`,
`Audio/AudioLibrary.asset`, `Settings/UI/UITheme.asset`, `Prefabs/UI/{ThemedButton, SettingsPanel}.prefab`,
`Scenes/MainMenu.unity`.

Modified — runtime: `Settings/{SettingsData (schema v2), ISettingsService, SettingsService}.cs` (mute flags);
`Audio/{IAudioService, SilentAudioService}.cs` (pitch, pause music); `Configuration/GameConfig.cs` (scene names,
audio library); `Core/{GameServices (navigator, Dispose), GameServicesFactory (audio host), GameBootstrapper}.cs`;
`Gameplay/GameplayLoop.cs` (pause/resume/menu, time scale, music, feedback, instructions);
`Gameplay/{IGameplayView, DropController (pointer filter), GameplayEntryPoint, MergeItem (visual child, pop), MergeBoard (1 line: pop on merge)}.cs`;
`UI/GameplayHud.cs` (rewritten).
**Unchanged:** merge resolver, merge rules, session, score rules, game-over rule, gameplay config, physics settings.

Modified — editor: `Editor/GameplaySetup.cs` (data only; prefab gets a visual child; scene building moved to
`ContentSetup`).

Modified — tests: `PlayMode/{GameplayTestRig (fake navigator, injected time scale, new view members), GameplaySceneSmokeTests}.cs`, PlayMode asmdef (+`UnityEngine.UI`).

Modified — generated assets / settings: `Scenes/Gameplay.unity` (regenerated), `Prefabs/MergeItem.prefab`
(rebuilt in place, same GUID), `ScriptableObjects/GameConfig.asset` (`firstSceneName: MainMenu`, audio library),
`ProjectSettings/EditorBuildSettings.asset` (Bootstrap 0, MainMenu 1, Gameplay 2, SampleScene kept).
No packages added; no networking, database or ads code.

### Animation and audio actually connected

- Connected and exercised in the scene-flow test: menu entrance, panel transitions (settings, pause, game over),
  button press feedback, score punch, next punch, "+N" popups (seen in a capture), merge pop, sparkles, game-over title punch.
- Audio wiring is covered by tests (sources, volumes, mute, track switching, missing clips). **Audible output was not
  verified**: tests ran in batch mode, nobody listened. The clips are synthesized placeholders, not composed music.

### Placeholders / missing licensed assets

- All UI art, item art and audio are generated placeholders produced by `Editor/Content` (no third-party or downloaded
  assets). Replace files at the same paths to upgrade.
- No custom font: Unity's built-in font (bold + outline). No Urdu-script text (built-in font coverage not verified).

### Verification actually run (real project, batch mode)

| Check | Result |
|---|---|
| Compile | 0 errors; 0 warnings from project code (two deprecation warnings in my test code were fixed) |
| Content builder (`ContentSetup.RebuildGeneratedScenesAndPrefabs`) | Exit 0 |
| EditMode tests | **115 / 115 passed** (96 existing + 19 new) |
| PlayMode tests | **58 / 58 passed, 0 skipped** (33 existing + 25 new) |
| Screenshots | 12 captures reviewed (menu, settings, HUD, pause, game over × 9:16 / 19.5:9 with notch / 3:4) |
| Android build | **Not run in Phase 3** (last Android build verified: Phase 1; an IL2CPP build took ~70 min) |
| Device / Device Simulator / listening to audio | **Not done** |

Problems found and fixed during verification:
1. First screenshot pass: on a 3:4 tablet the menu title was cut off; on a 19.5:9 phone the board was slightly wider
   than the view. Added `ResponsiveScaler` and `CameraFit`; re-captured and confirmed.
2. Code review before running: `?.`/`??` used on Unity objects (fake-null risk) in three places; a settings panel
   that would hide itself when opened from an inactive state; button press scale snapping back while held. All fixed
   before the first test run.

### Known issues

- Item labels still rotate with the items and are tiny on the smallest items (unchanged from Phase 2).
- `CameraFit` and `ResponsiveScaler` apply on startup / rect changes; fine for a portrait-locked game, but the
  Android orientation is not yet locked to portrait in Player Settings.
- Backgrounding the app does not auto-pause the game (progress is still saved on pause).
- Abandoning a run via Main Menu doesn't count as a played game or high score (by design; revisit if wanted).
- The Bootstrap scene still has a plain black camera (shown for one frame before the menu).

### Manual steps (Unity Editor)

1. Open the project; wait for import. The Console should be free of red errors.
2. Optional: Window → General → Test Runner → EditMode / PlayMode → Run All.
3. Recommended before an Android build: Player Settings → Resolution and Presentation → Default Orientation
   **Portrait**.
4. If you edit `UITheme` colours: **Desi Merge → Setup → Rebuild Generated Scenes and Prefabs (overwrites)**.

### How to launch and inspect each screen

- **Full game (real saves, one music player):** open `Assets/_Project/Scenes/Bootstrap.unity` → Play.
  Main menu appears → **Settings** (move sliders, toggle Mute, Done) → **Play**.
- **In gameplay:** drag to aim, release to drop; merges show "+N" and sparkles; tap the orange **II** (pause) → Resume
  or Main Menu; stack items above the red line for 2 s → Game Over → Restart or Main Menu.
- **Single screens:** open `MainMenu.unity` or `Gameplay.unity` directly → Play (yellow "Started without Bootstrap"
  warning is expected; saves are temporary).
- **Phone shapes / notches:** Game view → switch to **Simulator** → pick a phone with a notch, and a tablet.

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
