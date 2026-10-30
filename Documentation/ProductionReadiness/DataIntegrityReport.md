# Phase 6.7: Data Integrity Report

## Save files (`Application.persistentDataPath/saves/`)

| File | Owner service | Written | Validation on load |
|---|---|---|---|
| `player_progress.json` | PlayerProgressService | Game over, leaving gameplay, pause/focus loss/quit | Migrates v1→v2; clamps negatives; drops invalid IDs |
| `settings.json` (v3) | SettingsService | Settings closed, lifecycle | Volumes clamped 0..1 |
| `power_ups.json` | PowerUpService | Immediately on each grant; on flush | Negatives → 0; **counts clamped to the maximum (fix PR-03)**; grant-ID list bounded to 200 |
| `achievements.json` | AchievementService | Flush | Unknown IDs ignored |
| `daily_challenge.json` | DailyChallengeService | Claim (immediately), flush | Day policy (`DayKey.Effective`) |
| `daily_rewards.json` | DailyRewardService | Claim (immediately), flush | Index clamped to the calendar; streaks ≥ 0; day policy |
| `missions.json` | MissionService | Claim (immediately), flush | Progress ≥ 0; stale or unknown entries dropped |
| `milestones.json` | MilestoneService | Claim (immediately), flush | Unknown IDs ignored |
| `cosmetics.json` | CosmeticService | Flush | Unknown or unowned selection falls back to the default |

## Guarantees and the tests that prove them

| Guarantee | Test(s) |
|---|---|
| Atomic write: a crash during a save leaves the previous file | `FileStore_InterruptedWrite_KeepsTheLastGoodFile` |
| Corrupt or empty main file → backup → defaults, bad copy kept | `BrokenSaveFiles_LoadAsWorkingDefaults…` (5 variants × 9 files), Phase 4 repository tests |
| **Unreadable storage never overwrites real progress** | `UnreadableSave_IsNeverOverwrittenByAFreshProfile` (failed before fix PR-01) |
| Failed writes are reported and retried, never half-applied | `FailedWrites_AreReported_AndSavedOnTheNextFlush` |
| A reward is paid at most once (grant ID before claim record) | `KilledBetweenGrantAndClaimRecord…`, `DailyReward_ClaimInterruptedAfterTheGrant…`, `Grant_IsAppliedOncePerGrantId…` |
| Nothing negative or impossible | `ImpossibleValues_AreClampedToValidRanges` |
| Saves from a newer app version keep known data | `NewerSchemaVersion_KeepsTheKnownProgress` |
| Old saves migrate; nothing is reset | Phase 4 migration tests, `Settings_…OldSavesLoad…`, `Milestones_ExistingBestScore…` |
| Displayed inventory = saved inventory | Claim tests compare `SavedCharges` after a simulated restart |

## When progress can still be lost (by design, documented)

- Stats and mission progress from the **current** game are saved at game over, on leaving gameplay, and when the app is paused or loses focus. If Android kills the process without a pause callback (rare: Android normally pauses first), the game in progress is lost; claimed rewards are not, because they save immediately.
- A save written by a **newer** app version and then opened by an older one loses fields the older one doesn't know (PR-10). No newer version exists yet.
- An unreadable save (PR-01) is protected but not usable until the next launch; the menu says so.

## Not verified

Real Android storage behaviour (scoped storage, storage full, SD card removal with `installLocation=preferExternal`, PR-12) needs a device. See `AndroidDeviceTestReport.md`.
