# Phase 6.7: Crash and Failure Scenarios

Every row was either **executed** (an automated test on the real code, named here) or is marked **NOT TESTED** with the reason. Unity Test Framework fails a test on any logged error or unhandled exception, so every "Pass" also means "no exception was logged". PlayMode tests use the real generated scenes with in-memory saves.

New in 6.7: `ProductionStressTests` (PlayMode, 6) and `FailureInjectionTests` (EditMode, 14 incl. 5 corrupt-file cases).

## A. Gameplay

| # | Scenario | Result | Evidence |
|---|---|---|---|
| 1–4 | Rapid drops, the same spot, beyond the left/right walls, at the walls, near the top | Pass | `Stress_RapidDropsEdgesPausesAndRestarts…`: every frame, two drop attempts at ±(half width + 1), ±half width, 0 or random; invalid positions are clamped; checks every 30 frames that no item is outside the walls, below the floor or NaN |
| 5 | Crowded spawn area | Pass | The same stress session plays until the pile reaches the danger line and ends the game |
| 6–9 | Many simultaneous merges, chains, merges during other collisions, several contacts in one physics step | Pass | `Cluster_ManyIdenticalItemsTouching…`: 24 touching Patti at once; total value (Σ 2^level) is conserved (no double or lost merge); merges = items removed; score equals the merges |
| 10 | Merging the highest item | Pass | `FinalLevelPair_ResolvesOnce` (ClearBoth rule) |
| 11–12 | Repeated game overs, restart right after | Pass | Stress session auto-restarts after each game over and checks the board is empty and the score is 0; `RepeatedRestarts_…` (Phase 6) |
| 13 | Back to the menu during play | Pass | `MainMenu_LeavesOnceRestoresTimeAndSaves`, `MenuToGameplayAndBack…` |
| 14–15 | Rapid screen switches, button mashing during animations | Pass | `ButtonMashing_PauseResumeRestartEndInOneFrame…`, `Menu_ButtonAndBackSpam…` (200 random actions) |
| 16 | Pause during a merge | Pass | `PauseOnTheMergeFrame_FreezesIt_ThenItMergesOnceOnResume` |
| 17 | Pause during spawning | Pass | Stress session pauses every 5–8 s while drops are in flight; drops are refused while paused |
| 18 | Resume after a long pause | Pass (Editor) | Pause uses `timeScale = 0`; physics and timers don't accumulate, so the length of the pause makes no difference. Long real-time pauses on a phone: NOT TESTED (no device) |
| 19 | Invalid drops (during cooldown, paused, game over) | Pass | Stress session (2 attempts per frame, most refused), `Pause_StopsTimeInputAndMusic`, `PowerUps_RejectedWhilePausedAndAfterGameOver` |
| 20 | Long sessions with many objects and effects | Pass | 40 s, 5 min and 15 min sessions; see `PerformanceReport.md` |

Each stress frame also asserts: score never decreases within a game; `MergeCount` equals the merges the board reported; score equals the sum of `ScoreRules` for those merges (no double scoring); active items ≤ 150; scene objects stay bounded (pooling).

## B. UI and navigation

| # | Scenario | Result | Evidence |
|---|---|---|---|
| 1, 2, 9, 14 | Rapid open/close, repeated back, several reward popups, interrupted transitions | Pass | `Menu_ButtonAndBackSpam…`: 200 random button, back and claim actions with almost no frames in between; never more than one panel (+ the popup); back always returns to the bare menu |
| 3 | Settings during gameplay | n/a | The pause screen has no settings button (by design since Phase 3) |
| 4 | Closing settings | Pass | `Close_PersistsSettings`. Changes apply and save immediately; there is no unsaved state. |
| 5 | Tab switching | Pass | `Menu_EveryNewButton…` switches all tabs |
| 6, 12, 13 | Claim while navigating away, double-tap claim, disabled buttons | Pass | Spam test (claim mashing paid exactly once); `DailyReward_ClaimFromTheScreen…` (double tap) |
| 7 | Missions open while progress changes | Pass | `MissionsPanel` listens to `Changed`; `MissionAndRank_Claimed…` changes progress with the menu loaded |
| 8 | Restart while a modal is open | Pass | `PauseRestart_AsksForConfirmation…`, button mashing test |
| 10 | UI while paused | Pass | `ButtonFeedback`/panels use unscaled time; pause/confirm tested at `timeScale = 0` |
| 11 | Touch outside modals | Pass (by construction) | Every modal has a full-screen raycast dim; `PressOnHudControl_DoesNotDrop` |
| 15 | Leave during scene loading | Pass | `SceneNavigator.IsLoading` blocks repeats; menu buttons disable after Play |

## C. Persistence (all on isolated in-memory stores or a temp folder)

| # | Scenario | Result | Evidence |
|---|---|---|---|
| 1 | Closing during a save | Pass | `FileStore_InterruptedWrite_KeepsTheLastGoodFile` (torn `.tmp`) |
| 2–4, 20 | Force-stop during play / after a claim / after a mission / after a daily claim | Pass | `KilledBetweenGrantAndClaimRecord…`, `FailedWrites_AreReported…`, `DailyReward_FirstClaim…Restart`, `Missions_RewardIsClaimedExactlyOnce…` |
| 5–9 | Corrupt JSON, missing, empty, missing/null fields, unknown version | Pass | `BrokenSaveFiles_LoadAsWorkingDefaults…` (5 cases × all 9 files), `NewerSchemaVersion_KeepsTheKnownProgress` |
| 10 | Old version migration | Pass | `Settings_TutorialFlag_PersistsAndOldSavesLoad…` (v2 → v3), Phase 4 progress migration tests |
| 11–12 | Negative score, impossible quantities | **Pass after fix PR-03** | `ImpossibleValues_AreClampedToValidRanges` |
| 13–15 | Duplicate reward, achievement, discovery | Pass | Grant-ID tests, `AchievementReward_IsAppliedOnce…`, `Collection_FirstDiscovery…` |
| 16 | Write failure | Pass | `FailedWrites_AreReported_AndSavedOnTheNextFlush` |
| 17 | **Read failure** | **Pass after fix PR-01** (was data loss) | `UnreadableSave_IsNeverOverwrittenByAFreshProfile` |
| 18 | Rapid consecutive saves | Pass | Saves happen only on state changes and lifecycle events; services skip clean writes; atomic replace each time |
| 19 | Full disk | Pass (simulated as a write exception) | Same as 16. A real full disk: NOT TESTED |

## D. Daily rewards and missions abuse

| Scenario | Result | Evidence |
|---|---|---|
| Claim twice, double tap, restart after claiming | Pass | `DailyReward_FirstClaim…`, `DailyReward_ClaimFromTheScreen…`, spam test |
| Date backwards (≤ 7 days) | Pass: no re-claim | `DailyReward_ClockMovedBack…`, `ClockMovedBackAFewDays…` |
| Date far forward, then corrected | **Pass after fix PR-02** | `FarFutureClockMistake…` |
| Skip several days | Pass: back to Day 1 | `DailyReward_MissedDay…` |
| Rapid repeated mission events, completing twice, claiming a claimed mission | Pass | `Missions_MergeProgress_CompletesOnce…`, `Missions_RewardIsClaimedExactlyOnce…` |
| Several missions completing at once | Pass | `Missions_ScoreCreateAndPowerUpTypes…` (one score completes two) |
| Week change | Pass | `Missions_DailyResetsEachDay_WeeklyResetsOnMonday` |
| Restart during the reward animation | Pass | The reward is saved **before** the popup appears (`TryClaim` then `Show`); the popup is presentation only |
| Old saves | Pass | Missing 6.6 files load as defaults; an existing best score earns ranks at startup |

Limitation: offline date checks are not anti-cheat. Moving the clock forward (or back by more than a week) can earn extra claims. Only a trusted server time could prevent this.

## E. Audio

| Scenario | Result | Evidence |
|---|---|---|
| Music/SFX off, volume 0, rapid changes, mute/unmute | Pass | `MusicVolumeFollowsSettingsAndMute`, `SfxMute_SkipsPlayback`, `AudioService_MusicVolumeAndMute_ApplyImmediately` |
| Scene change, restart, pause/resume | Pass | `SourcesAreCreatedOnceAndNeverDuplicated`, `PlayingTheSameTrackAgain…`, `Pause_StopsTimeInputAndMusic`, scene flow (one audio host) |
| Missing clip | Pass | `MissingClips_WarnOnceAndStaySilent` |
| Missing AudioSource / duplicate manager | Pass | Sources are created by the service, never from the scene; the bootstrapper destroys duplicates; exactly one listener |
| Routing (listener present) | Pass | `Scenes_HaveExactlyOneEnabledAudioListener…` |
| **Audible output, app switch, phone call, audio focus** | **NOT TESTED** | No audio device in batch mode, no phone. Manual checklist in `AudioSystemAudit.md` |

## F. Power-ups and rewarded ads (test doubles only; no provider success was faked in a release build)

| Scenario | Result | Evidence |
|---|---|---|
| Hammer without a target, repeated, during other operations | Pass | `Hammer_MissKeepsWaiting…`, `PowerUps_RepeatedOrSecondActivationIsHandled`, mashing test |
| Shuffle with too few or too many items | Pass | `PowerUps_EmptyBoardAndUsedUpAreRefused`, `Shuffle_RefusedWhenItCannotFit…` |
| Not enough charges | Pass | `Consume_UsesFreeUsesBeforeSavedCharges…` |
| Interruption during use | Pass | `Pause_WhileChoosingATarget_CancelsWithoutCost` |
| Ad unavailable, load failure, cancelled, no callback, duplicate callback, backgrounded, late reward, revive repeated | Pass | `Revive_*`, `AppInterruptedDuringAd…`, `RestartOrLeavingDuringAd…`, `Coordinator_*` (8 tests) |
| Real ad provider | NOT TESTED / n/a | No ad SDK is integrated; release builds hide Continue |

## Controlled failure injection (test-only, never in release builds)

| Injected failure | Detected | Handled | Data safe | Feedback | Recovers |
|---|---|---|---|---|---|
| Save write throws | `FlushAll` returns false, one warning per file | Changes kept in memory | Yes | Silent retry (no UI, by design) | Yes, at the next flush |
| Save read throws | `LoadOutcome.ReadFailed`, warning | Defaults this session, **writes blocked** | **Yes (after PR-01)** | Menu notice | Yes, at the next launch |
| Corrupt or empty file | Warning, `*_corrupt` copy kept | Backup, then defaults | Backup preserved | Menu notice for progress | Yes |
| Missing audio clip | One warning per cue | Silent cue | n/a | None | Yes |
| Ad unavailable/failed/cancelled/duplicate | Coordinator result | No reward | Yes | Revive status text | Retry allowed |
| Scene load already in progress | `IsLoading` | Second request refused | n/a | Buttons disabled | Yes |
| Repeated input | Guards in loop and panels | Ignored | Yes | n/a | Yes |

Test doubles live only in test assemblies (`FaultyStore`, `FakeAdProvider`). No debug or cheat controls exist in the game.
