# Desi Merge — Final Weakness Report (Deep Audit)

Date: 2026-11-03 · Commit audited: `79abace` (`main`, clean, equal to `origin/main`) · Type: read-only audit.
Detailed reports: [ProjectArchitectureAudit](ProjectArchitectureAudit.md) · [GameplayWeaknesses](GameplayWeaknesses.md) ·
[SaveAndRewardSecurity](SaveAndRewardSecurity.md) · [UIAndNavigationAudit](UIAndNavigationAudit.md) ·
[AndroidLifecycleAudit](AndroidLifecycleAudit.md) · [PerformanceRisks](PerformanceRisks.md) ·
[DependencyAndPrivacyAudit](DependencyAndPrivacyAudit.md) · [ExploitabilityRegister](ExploitabilityRegister.md).

## 1. Executive summary

Desi Merge is a small, well-layered offline game with **no network attack surface**: the release APK requests no INTERNET
permission and contains no third-party SDK (confirmed with `aapt2`). The merge/physics core is defensively written (single
merge per contact, pooled-object protection, one game over, guarded inputs and idempotent, grant-first rewards).

The audit found **21 findings: 0 CRITICAL, 0 HIGH, 3 MEDIUM, 9 LOW and 9 INFORMATIONAL**. The MEDIUM items are:
* **DM-01:** a run in progress is lost if Android kills the backgrounded app.
* **DM-12:** the release identity isn't set (placeholder package id, debug signing).
* **DM-13:** the latest UI and localization work was never run through the automated tests.

The LOW items are mainly economy accounting gaps:
* **DM-03:** bought power-ups don't count as used, which wrongly credits "without power-ups" missions.
* **DM-04:** Restart refreshes free power-ups, which allows mission farming.
* **DM-02:** clock changes give unlimited daily rewards. This is expected offline cheating.
* **DM-05, DM-06:** two small save-timing windows.

**Nothing was reproduced at runtime in this audit.** All gameplay/economy findings are CODE-VERIFIED or SUSPECTED.

## 2. Module inventory
See ProjectArchitectureAudit.md §1 (102 runtime scripts, 25 editor scripts, 42 test files, 3 scenes, 17 data assets).

## 3. Architecture and dependency map
See ProjectArchitectureAudit.md §2–3. One persistent bootstrapper owns `GameServices`; scene entry points receive services;
`GameplayLoop` is plain C# and unsubscribes everything in `Dispose`.

## 4. Most critical weaknesses
1. DM-01 — run lost on process death (player-visible data loss).
2. DM-13 — unverified UI/localization changes shipped to the test phone.
3. DM-12 — release identity/signing not set (blocks a store build; package id is permanent once published).
4. DM-03 — missions credited without valid completion.

## 5. Confirmed bugs (code-verified defects, not design choices)
DM-03, DM-09, DM-19, DM-10 (latent). DM-05/DM-06 are timing windows rather than logic errors.

## 6. Reproducible crash scenarios
None found. No unguarded null dereference was identified on the inspected runtime paths; configuration errors are reported via
`GameplayEntryPoint.ValidateSetup` instead of crashing. This is static inspection; it doesn't prove that no crash exists.

## 7. Potential exploit scenarios (all local, single-player)
* Device clock forward/backward → extra daily rewards and missions (DM-02).
* Restart loop with free Hammer → power-up missions (DM-04).
* Bought power-ups while doing "no power-up" missions (DM-03).
* Editing `saves/*.json` (root/debug tools) → any value within the clamped ranges (expected offline cheating).
None affects other players; all would need re-assessment before adding leaderboards, cloud saves or purchases.

## 8. Save/data-integrity risks
Atomic temp+replace writes, one backup generation, quarantine of corrupt files, no destructive reset on read failure.
Remaining: DM-01, DM-05, DM-06, DM-08, DM-11, DM-07.

## 9. Reward and economy weaknesses
DM-02, DM-03, DM-04, DM-07, DM-19. Duplicate claims of the same reward id, negative balances and partial multi-reward
grants are prevented by code (SaveAndRewardSecurity.md §4).

## 10. UI/navigation failures
DM-14 (quit without confirm), DM-09 (preview flash), DM-22 (CJK font dependency), DM-13 (unverified changes). The navigation
graph is in UIAndNavigationAudit.md §1.

## 11. Android lifecycle risks
DM-01, DM-05, DM-11; allowBackup default (DM-16). Pause/focus/quit all flush; resume stays paused (correct).

## 12. Performance/resource risks
Inspection only: DM-15, re-skin on discovery, synchronous saves on background. **No measurements exist.**

## 13. Third-party and privacy risks
No SDKs, no network, no personal data, no secrets in the repo. Unused packages (DM-17). Logcat logging without PII (DM-20).

## 14. Exact files and methods
| ID | File : member (line) |
|---|---|
| DM-01 | `Gameplay/GameplayEntryPoint.cs` : `OnApplicationPause` (148), `OnApplicationFocus` (156) |
| DM-02 | `Core/IClock.cs` : `DayKey.Effective` (58); `Progression/DailyRewardService.cs` : `Refresh` (125), `TryClaim` (163) |
| DM-03 | `Gameplay/PowerUpController.cs` : `Consume` (157–165); `Progression/MissionService.cs` : `OnPowerUpUsed` (344) |
| DM-04 | `PowerUps/PowerUpService.cs` : `BeginGame` (156); `Gameplay/GameplayLoop.cs` : `Restart` (366) |
| DM-05 | `Progression/PlayerProgressService.cs` : `TrySpendCoins` (169) |
| DM-06 | `Cosmetics/CosmeticService.cs` : `TryBuy` (102–127) |
| DM-07 | `Progression/PlayerProgressService.cs` : `TryGrantCoins` (40–63); `Gameplay/GameplayLoop.cs` : `AwardGameDiamonds` (458) |
| DM-09 | `Cosmetics/CosmeticService.cs` : `RecordDiscovery` (129); `Gameplay/GameplayEntryPoint.cs` : `OnSkinChanged` (129) |
| DM-10 | `Gameplay/GameplayEntryPoint.cs` : `OnSkinChanged` (132) |
| DM-11 | `Persistence/FileSaveStore.cs` : `Write` (59–66) |
| DM-14 | `UI/MainMenuEntryPoint.cs` : `OnBackPressed` (230–236) |
| DM-15 | `Progression/MissionService.cs` : `OnMergeRecorded` (314–316) |
| DM-19 | `Progression/PlayerProgressService.cs` : `TryGrantCoins` (57) |

All paths are relative to `Assets/_Project/Scripts/`.

## 15. Severity and confidence
See ExploitabilityRegister.md (summary table).

## 16. Recommended fixing order
1. **DM-13:** run EditMode + PlayMode, fix any failures (gate for everything else).
2. **DM-03:** one-line class of fix, wrong rewards today.
3. **DM-01:** board snapshot / Continue (largest player-visible gain).
4. **DM-05, DM-06, DM-19, DM-11:** small save-hardening fixes.
5. **DM-09, DM-10:** event split, index fix.
6. **DM-04, DM-02:** decide as game-design questions (accept or limit).
7. **DM-12:** before any store build (needs your choice of package id and keystore).
8. Informational items as time allows.

## 17. Missing test coverage
* Never executed: `Tests/EditMode/LocalizationTests.cs`, `Tests/PlayMode/LocalizationFlowTests.cs`, the Hammer-focus case in
  `Tests/PlayMode/HudLayoutTests.cs`; and the whole suite since the localization/HUD/glass/splash/top-row changes.
* No test for: bought power-up counting toward missions (DM-03); restart farming (DM-04); purchase-write interruption
  (DM-06); spend durability (DM-05); clock-backwards > 7 days granting a new claim (DM-02 backward); `OutsideTapClose`
  behaviour per modal; language dropdown persistence through restart of the app; `LightThemePass` output; splash timing ≤ 2 s
  in the real Bootstrap scene; process-death behaviour (DM-01).

## 18. Unverified assumptions
* Which `FileSaveStore` replace path IL2CPP Android uses (DM-11).
* Android audio-focus behaviour during calls (not tested on device in this audit).
* CJK font availability on non-Pixel devices (DM-22).
* Runtime cost of per-merge mission refresh and of `FlushAll` on slow storage (no profiling).
* Current Google Play target-API deadline (not re-checked online).
* That unused packages add nothing to the APK (not measured).

## 19. Overall technical risk assessment
**Security risk: LOW.** Offline, no network, no SDKs, no secrets, internal storage only.
**Reliability risk: MEDIUM.** The core is robust, but recent UI work is untested, and a run is lost on process death.
**Release risk: MEDIUM.** The release identity and signing are not configured.
The application isn't claimed to be production-ready by this audit.

## 20. Recommended next phase
"Phase 8 — Audit fixes": run the full test suites first (DM-13), then fix in the order of §16 with a regression test for each
finding, then a device session on the Pixel (background-kill, call interruption, language switching, 15-minute play). **No
fixing has started. Waiting for your approval.**
