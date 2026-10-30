# Phase 6.7: Release Readiness Checklist

PASS = executed and passed. FAIL = executed and failed. BLOCKED = can't be done without something the owner must provide. NOT TESTED = not executed (reason given). BLOCKED and NOT TESTED are **not** passes.

| Gate | Item | Status | Evidence / reason |
|---|---|---|---|
| **A. Startup & stability** | Bootstrap → splash → menu, no missing references, no logged errors | PASS (Editor) | Scene tests; splash test; Android build |
| | Startup on a phone | NOT TESTED | No device |
| **B. Gameplay correctness** | Score = sum of merges, no double scoring, game over, restart, pause | PASS | Stress session invariants, Phase 2–6 gameplay tests |
| **C. Physics & merges** | No duplicate or lost merges under clusters; final level | PASS | `Cluster_…`, `FinalLevelPair_…`, stress invariants |
| | Walls hold in long sessions | **FAIL → fixed → partly verified** | Escapes found (PR-00); no escape in a 5-minute run after the fixes; rare 0.40 overlap remains (PR-13); the **15-minute run after the final fix did not complete** (stopped for low memory) |
| **D. UI & input** | One panel at a time, back everywhere, button mashing, UI input never drops items | PASS | `Menu_ButtonAndBackSpam…`, `ButtonMashing_…`, HUD blocker tests |
| | Touch feel on a phone | NOT TESTED | No device |
| **E. Persistence** | Corruption, migration, write and read failure, atomic writes | PASS (after fix PR-01) | `FailureInjectionTests`, Phase 4/6 tests |
| | Real storage conditions (full disk, SD card) | NOT TESTED | No device |
| **F. Daily rewards & missions** | Idempotent claims, resets, clock abuse within the documented offline limits | PASS (after fix PR-02) | `RewardSystemsTests`, `FailureInjectionTests` |
| **G. Progression & achievements** | Ranks, awards, collection: once only, persistent | PASS | Tests listed in `CrashAndFailureScenarios.md` |
| **H. Audio** | Routing, listener, volumes, mute, persistence, no duplicates | PASS (automated) | Audio tests |
| | Audible output, audio focus, calls | NOT TESTED | No audio device in batch mode; no phone |
| **I. Android lifecycle** | Pause/focus/quit handlers, flush on pause | PASS (simulated in Editor) | `AppInterruption_…`, bootstrapper flush |
| | Real background, lock, kill, low memory, activity recreation | NOT TESTED | No device |
| **J. Performance & memory** | 5 min Editor stress: bounded objects, memory flat after +4 MB | PASS (Editor, PC only) | `PerformanceReport.md` |
| | 15 / 30 min sessions with the final physics settings | NOT TESTED | 15 min stopped (PC low on memory); 30 min not run |
| | Frame rate, memory, heat, battery on a modest phone | NOT TESTED | No device |
| **K. Security & privacy** | No secrets, no network code, cloud services off, no unused permissions | PASS (after fix PR-05) | Audit; built manifest |
| | Data Safety form / privacy policy | BLOCKED | Owner's declarations; PR-11 (engine diagnostics) to confirm |
| **L. Build & signing** | Release-config APK builds with 0 errors; INTERNET removed | PASS (build before the solver change) | Build at 21:58: 0 errors, 2 warnings, 32.5 MB, manifest has no INTERNET permission |
| | Rebuild after the solver-iteration change | NOT TESTED | Stopped (PC low on memory) before the build step |
| | Application ID chosen by the owner | **BLOCKED** | PR-06 |
| | Release signing (upload key), AAB for Play | **BLOCKED** | PR-07 |
| **M. Real device QA** | Full manual plan | **NOT TESTED** | No device; plan in `AndroidDeviceTestReport.md` |
