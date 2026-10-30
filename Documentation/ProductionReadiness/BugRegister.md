# Phase 6.7: Bug Register

Severity uses the brief's scale (P0 critical … P3 low). "Reproduced" means a test failed on the unfixed code. All test runs are real batch-mode runs on 2026-10-30.

| ID | Sev | Area | Status |
|---|---|---|---|
| PR-00 | **P1** | Physics: merged items can escape through the container walls | **Fixed for the observed cause** (no escape in two 5-minute runs afterwards); the 15-minute verification is **not completed** |
| PR-13 | P2 | Under heavy stacks an item can still be squeezed up to 0.40 units into a wall (2 of 6,593 samples in 5 minutes, after the solver change) | **Open**: reduced, not eliminated |
| PR-01 | **P1** | Persistence: a temporary read failure can wipe progress | **Fixed** + regression test |
| PR-02 | P2 | Daily systems locked for years after a wrong future date | **Fixed** + regression test |
| PR-03 | P2 | Impossible power-up counts accepted from the save file | **Fixed** + regression test |
| PR-04 | P3 | A calendar day with two grants of one power-up could fail halfway | **Fixed** (validation) + test |
| PR-05 | P2 | Unused `INTERNET` permission in the release APK | **Fixed** (build step), verified in the built manifest |
| PR-06 | **Blocker** | Placeholder application ID `com.DefaultCompany.DesiMerge` | **Open — owner decision** |
| PR-07 | **Blocker** | Release APK/AAB signed with the debug key | **Open — needs the owner's keystore** |
| PR-08 | P3 | Android build cache `.utmp/` tracked in Git | Partly fixed (`.gitignore`); owner runs `git rm -r --cached .utmp` |
| PR-09 | P3 | `MissionService` rebuilds its state objects on every gameplay event (small allocations per merge) | Open (low impact; see `PerformanceReport.md`) |
| PR-10 | P3 | Opening a save from a **newer** app version in an older version drops the unknown fields on the next save | Open (no newer version exists yet; documented) |
| PR-11 | Decision | `m_EngineDiagnosticsEnabled: 1` in `UnityConnectSettings`, while crash reporting and analytics are off | Open — owner to confirm before the Data Safety form |
| PR-12 | P3 | `installLocation="preferExternal"` (Unity default); apps on removable storage can go missing if the card is removed | Open — consider "Auto" |

---

### PR-00 (P1): items escaped through the walls during long sessions

- **Reproduction:** the long sessions of `ProductionStressTests.Stress_RapidDrops…` (rapid drops at and beyond both walls, pauses, auto-restart). The **5-minute** run failed after 45 s: `item escaped through a wall at (4.61, 0.85)`; the wall face is at x = 3.25. The **15-minute** run failed after 93 s at x = −3.37. The shorter 40 s run had passed, so only longer sessions expose it. Escaped items fall out of play, which silently changes the board and can block a game or end it unfairly.
- **Root cause:** `MergeBoard.OnUpgrade` spawned the merged item, which is **larger** than the two it replaces, at their midpoint with no bounds check. When two items merge against a wall, the new item starts partly inside the wall collider, which was only 0.5 units deep. With the pile pressing on it, the physics solver could resolve the overlap toward the outside and push it through.
- **Fix (two small, local changes in `MergeBoard`):**
  1. `InsideContainer()` clamps the merged item's spawn point to x ∈ ±(half width − radius) and y ≥ radius. A merge against a wall now starts touching the wall, not inside it. The shift is at most the difference in radius, a few centimetres in game units; scores and rules are unchanged.
  2. Wall and floor **colliders** are 2 units deep (art unchanged, still 0.5). The inner faces are exactly where they were, so the play area is identical, and nothing can be pushed through the full depth.
  3. After those two, the 15-minute run still failed after 120 s: an item 0.15 **inside** the thicker wall, squeezed by a heavy stack, not escaped. 2D solver iterations were raised from Unity's defaults (velocity 8 / position 3) to **10 / 6** (`ProjectSettings/Physics2DSettings.asset`), the standard fix for soft stacking. This is a documented physics change; drop and merge rules are untouched. The stress test now fails only on a real escape (centre past the visible wall art), and **measures** the deepest overlap instead of hiding it (PR-13).
- **Regression tests:** `MergeAgainstAWall_BiggerResultStartsInsideTheContainer` (deterministic: two Mango Shakes stacked against the right wall become a Rickshaw that must start within the walls). The 5- and 15-minute sessions were then re-run; results are in `PerformanceReport.md`. The deterministic test was added together with the fix and was **not** run against the old code; the original evidence is the two failed long runs above.

### PR-01 (P1): an unreadable save can be overwritten by a fresh profile

- **Reproduction:** `FailureInjectionTests.UnreadableSave_IsNeverOverwrittenByAFreshProfile`. Save a best score of 900 and 4 Hammer charges. Make reads throw (`IOException`), which is what Android returns when storage is momentarily unavailable or a file is locked. Start the services: the load reported `NoSave` (a first launch). Failed on the unfixed code: `Expected: ReadFailed  But was: NoSave`.
- **Root cause:** `JsonRepository.TryReadSafely` caught the exception and returned "not found", so `Load` returned an empty profile as a first launch, and the next `Save` wrote it. `FileSaveStore.Write` keeps exactly one `.bak`, so the second save also replaced the backup: **the real progress was gone after two saves** (game over and app pause each save).
- **Fix (smallest safe):** `JsonRepository` remembers that a read threw. If no data could be loaded, it returns the new `LoadOutcome.ReadFailed`, and `Save` refuses to write for that session, so the file on disk is never replaced. The main menu shows "Your saved progress couldn't be opened right now. It is safe; restart the game to load it."
- **Evidence:** the same test now passes: after reads work again, the best score of 900 and the 4 charges are intact.

### PR-02 (P2): a wrong future date locked daily rewards, missions and the daily challenge for years

- **Reproduction:** `FailureInjectionTests.FarFutureClockMistake_DoesNotLockDailySystemsForYears`. Device date 10 years ahead, claim, then correct the date: the reward stayed unavailable (`Expected: True But was: False`). Missions stayed on the future day.
- **Root cause:** to stop "move clock back, claim again", every daily system used `max(today, latest day seen)`. One wrong future date (dead-battery resets and bad network time do happen) made "latest day seen" years ahead, so nothing would refresh until that date.
- **Fix:** `DayKey.Effective(today, latest)` keeps the protection for small backward moves (up to 7 days: no re-claim) but trusts the device again when it is **more than 7 days** behind the latest day seen. Used by `DailyRewardService` (whose calendar then restarts at Day 1; claimed rewards are kept), `MissionService` and `DailyChallengeService`.
- **Trade-off (documented):** a player can get one extra claim by moving the clock back more than a week. That is no worse than moving it forward, which no offline game can stop.
- **Evidence:** the test passes; `ClockMovedBackAFewDays_StillCannotReclaim` and the existing clock-back tests still pass.

### PR-03 (P2): impossible power-up counts accepted

- **Reproduction:** `FailureInjectionTests.ImpossibleValues_AreClampedToValidRanges`. A save with `"count":9999` gave 9999 Hammers (`Expected: 9 But was: 9999`). Negative values were already clamped to 0.
- **Root cause:** `PowerUpService.Migrate` clamped only the lower bound.
- **Fix:** after loading, saved charges are clamped to each power-up's `maxSavedCharges`.

### PR-04 (P3): calendar validation

- **Reproduction:** `CalendarDay_WithTwoRewardsOfOnePowerUp_IsRejectedAsInvalid` failed. Such a day passes the room check reward by reward, then the second grant fails, giving a half-paid claim that reports `Failed`.
- **Fix:** `DailyRewardCatalog.IsValid` rejects a day that grants the same power-up twice. The shipped calendar is unaffected (its Day 7 is one Hammer plus one Shuffle grant).

### PR-05 (P2): unused INTERNET permission

- **Found:** the generated `unityLibrary/src/main/AndroidManifest.xml` declared `android.permission.INTERNET`. A search found no network use in project code, and the Unity cloud services are disabled.
- **Root cause:** Unity's Android template adds it by default.
- **Fix:** `Editor/OfflinePermissionFilter.cs` removes it from non-development builds after Gradle generation (development builds keep it for the profiler). It must be removed if a network SDK is ever added.
- **Evidence:** see the Android build section of `FinalProductionAudit.md` (the manifest of the rebuilt APK is checked there).

### PR-06 / PR-07: release blockers that need the owner

- **PR-06:** the Android package name comes from the default "DefaultCompany". Google Play identifies the app by it forever, and it can't be changed after the first upload. Choose a name you control (e.g. `com.<yourname>.desimerge`) and set it in Player Settings → Android → Identification. **Not changed by me:** it is your decision, and the brief says to preserve it.
- **PR-07:** the build uses Unity's debug keystore. Play needs a release-signed **AAB**. Create an upload keystore (Player Settings → Publishing Settings), keep it and its passwords **out of Git**, and enrol in Play App Signing. I did not create or commit any key.

### Not bugs (checked and found correct)

Corrupt, empty, `{}`, null-field and future-schema files for all 9 save keys load as working defaults; atomic writes survive a torn `.tmp`; a missing main file falls back to `.bak`; failed writes are reported and saved later; a kill between grant and claim neither loses nor duplicates; merges conserve value under a 24-item cluster; final-level merges resolve once; pause freezes merges; button-mashing pause, restart, game over and power-ups leaves a playable game; menu button and back spam never stacks panels or pays twice. See `CrashAndFailureScenarios.md`.
