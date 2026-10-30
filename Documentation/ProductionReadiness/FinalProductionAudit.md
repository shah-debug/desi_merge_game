# Phase 6.7: Final Production Audit

Date: 2026-10-30. Environment: Windows 11 development PC (8 GB RAM), Unity 6000.6.7f1 batch mode. **No Android device or emulator.**

## Release decision: **NOT READY — high-priority issues remain**

Why not "Conditionally ready":
1. **PR-00 (P1, walls) is not proven closed.** It was found and fixed in three steps. A 5-minute session passes, but the 15-minute session after the final fix never completed (stopped because the PC ran low on memory), and a rare 0.40-unit wall overlap remains (PR-13).
2. **Release prerequisites missing:** placeholder application ID (PR-06) and no release signing key (PR-07). Both need the owner.
3. **Zero real-device testing** (gate M), including audible audio and Android lifecycle.

What would move it to **CONDITIONALLY READY**: the 15-minute stress session passes with the current physics settings, and a rebuilt APK.
What would move it to **RELEASE CANDIDATE**: also the owner's app ID and upload key, a signed AAB, and the device test plan passed on at least one modest phone.

## Totals

| | Count |
|---|---|
| Issues recorded | 14 (PR-00 … PR-13) |
| P1 | 2: PR-00 (walls; partly verified), PR-01 (save overwrite; fixed and verified) |
| P2 | 5: PR-02, PR-03, PR-05 (fixed and verified); PR-12 (open); PR-13 (open) |
| P3 | 4: PR-04 (fixed); PR-08 (partly); PR-09, PR-10 (open) |
| Release blockers (owner) | 2: PR-06, PR-07 |
| Owner decision | 1: PR-11 (engine diagnostics flag) |
| P0 | 0 found |

## Files changed in Phase 6.7

| File | Change |
|---|---|
| `Scripts/Persistence/JsonRepository.cs` | `LoadOutcome.ReadFailed`; never writes after a read exception (PR-01) |
| `Scripts/Core/IClock.cs` | `DayKey.Effective`, 7-day clock tolerance (PR-02) |
| `Scripts/Progression/DailyRewardService.cs`, `MissionService.cs`, `DailyChallengeService.cs` | Use `DayKey.Effective` (PR-02) |
| `Scripts/Progression/DailyRewardCatalog.cs` | Rejects duplicate power-up grants per day (PR-04) |
| `Scripts/PowerUps/PowerUpService.cs` | Clamps saved charges to the maximum (PR-03) |
| `Scripts/Gameplay/MergeBoard.cs` | Merge result kept inside the container; 2-unit-deep colliders (PR-00) |
| `ProjectSettings/Physics2DSettings.asset` | Velocity/position iterations 8/3 → 10/6 (PR-00/13) |
| `Scripts/UI/MainMenuEntryPoint.cs` | Notice for `ReadFailed` |
| `Scripts/Editor/OfflinePermissionFilter.cs` (new) | Removes INTERNET from non-development builds (PR-05) |
| `.gitignore` | `.utmp/` (PR-08) |
| `Tests/EditMode/FailureInjectionTests.cs` (new, 14) | Failure injection |
| `Tests/PlayMode/ProductionStressTests.cs` (new, 7) | Crash hunting, stress, measurements |
| `Documentation/ProductionReadiness/*.md` (8), `PhaseStatus.md` | Reports |

## Test results (actually executed)

| Suite | Total | Passed | Failed | Skipped | When |
|---|---|---|---|---|---|
| EditMode | 217 | 217 | 0 | 0 | After all code fixes (the solver change is physics-only and not used by EditMode) |
| PlayMode | 117 | 117 | 0 | 0 | After **all** fixes, including the solver change |
| Stress 5 min | 1 | 1 | 0 | 0 | After all fixes |
| Stress 15 min | — | — | — | — | **Not completed** (stopped for low memory). Its earlier runs **failed** and led to the PR-00 fixes. |
| Stress 30 min | — | — | — | — | Not run |

The reproduced-then-fixed cases (failed on the old code, pass now): 4 failure-injection tests (PR-01/02/03/04) and the 5/15-minute wall escapes (PR-00).

## Android build

The last completed build (21:58, release configuration, IL2CPP ARM64) **succeeded**: 0 errors, 2 warnings (unused URP terrain shaders), APK **32.5 MB**. The build log shows "[Build] Removed the unused INTERNET permission", and the generated manifest contains no INTERNET permission. It includes PR-00 steps 1–2 and all persistence fixes, but **not** the later solver-iteration change; that rebuild was stopped. Debug-signed: **not uploadable** to Play (PR-07).

## Area summaries

- **Lifecycle:** handlers verified in the Editor (pause once, flush on pause, focus loss and quit). Not on a device.
- **Audio:** routing, listener, volume, mute and persistence verified automatically; nobody listened; no device (audio focus, calls).
- **Data integrity:** all 9 save files survive corruption, empty or newer versions, failed writes, read failures (after PR-01), and crashes between grant and claim. See `DataIntegrityReport.md`.
- **Security/privacy:** no secrets, no network code, cloud services off, INTERNET removed from release, no SDKs. Data Safety declarations still need the owner (PR-11). I don't claim Google Play policy compliance.
- **Performance:** Editor-only numbers in `PerformanceReport.md`; no device measurement.

## Not tested, and the manual test needed

| Area | Why not | Manual test |
|---|---|---|
| Real device: all of gate M, lifecycle, audio focus, notch, touch, thermal | No device or emulator | `AndroidDeviceTestReport.md`, steps 1–12 |
| 15 and 30 minute stress with the final physics | Stopped for low PC memory / time | Re-run the stress command in `PerformanceReport.md` with `DESIMERGE_STRESS_SECONDS=900` (then 1800), with nothing else open |
| Rebuild after the solver change | Stopped for low memory | `BuildCommands.BuildAndroidReleaseConfig` |
| Signed AAB | Needs the owner's keystore and app ID | Player Settings → Publishing |

The game is **not** bug-free. These reports describe what was tested and what was not.
