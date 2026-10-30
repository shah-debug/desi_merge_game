# Phase 6.7: Performance and Stress Report

## Test conditions (all measurements are from this setup only)

- **Hardware:** the development PC (Windows 11, 8 GB RAM, integrated Intel GPU). **Not an Android device.**
- **Runtime:** Unity 6000.6.7f1 Editor in batch mode (Mono, not IL2CPP), PlayMode tests on the real `Gameplay` scene with in-memory saves.
- **Workload** (`ProductionStressTests.Stress_RapidDrops…`, seed 1234): every frame, two drop attempts at the walls, beyond the walls, the centre or random spots (the 0.5 s cooldown refuses most); a pause every 5–8 s; automatic restart after each game over.
- Frame times are Editor frame times. The 40 s run was capped at 60 fps (16.7 ms); the longer runs were not capped, which is why their median is ~1.4 ms. **Neither represents phone performance.**

## Results

| Run | When | Frames | Games | Drops | Max items | Scene objects (start/max) | Frame ms p50 / p95 / p99 / max | Gen-0 GCs | Managed MB start→end | Max wall overlap | Result |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 40 s | before the PR-00 fix | 2,359 | 1 | 79 | 33 | 200 / 268 | 16.7 / 17.6 / 23.7 / 160.5 | 15 | 37.3 → 39.7 | not measured | Pass |
| 5 min | before the PR-00 fix | — | — | — | — | — | — | — | — | — | **FAIL**: item escaped at x = 4.61 after 45 s |
| 15 min | before the PR-00 fix | — | — | — | — | — | — | — | — | — | **FAIL**: escaped at x = −3.37 after 93 s |
| 5 min | clamp + thick colliders | 179,268 | 6 | 594 | 42 | 200 / 286 | 1.4 / 3.1 / 5.7 / 975.1 | 155 | 34.6 → 38.6 | not measured | Pass |
| 15 min | clamp + thick colliders | — | 3+ | — | — | — | — | — | — | — | **FAIL** after 120 s: item 0.15 inside the wall (x = −3.40) |
| 40 s | + solver 10/6 iterations | 2,376 | 1 | 79 | 39 | 200 / 280 | 16.7 / 18.8 / 25.4 / 128.9 | 12 | 37.6 → 38.1 | 0.019 | Pass |
| 5 min | + solver 10/6 iterations | 197,808 | 6 | 600 | 41 | 200 / 284 | 1.4 / 2.5 / 4.6 / 961.0 | 166 | 34.3 → 38.6 | **0.402** (2 of 6,593 samples > 0.1) | Pass |
| 15 min | + solver 10/6 iterations | — | — | — | — | — | — | — | — | — | **NOT COMPLETED**: stopped by Claude Code because the PC ran low on memory, not by the test |
| 30 min | — | — | — | — | — | — | — | — | — | — | **NOT RUN** (time and memory on this PC) |

## Reading the numbers

- **Bounded objects:** scene objects stay at ≤ 286 (pooling works); active items peak at ~42. There is no growth across 6 games.
- **Memory:** managed memory rises ~4 MB in 5 minutes and stays there, not climbing per game. No leak trend was seen within 5 minutes; longer sessions were not completed.
- **Garbage collection:** about one gen-0 collection every 2 s in the Editor. Contributors include the test's own checks; inside the game, `MissionService` rebuilds small state objects on every merge (PR-09). This was not profiled on IL2CPP.
- **Spikes:** the single max-frame outliers (~1 s) appear once per run and are consistent with Editor/batch-mode work (GC, asset loading, the test's scene scans); there is no device profiler capture to confirm or rule this out.
- **Physics:** solver iterations were raised from Unity's defaults (8 velocity / 3 position) to 10 / 6 to stop heavy stacks squeezing items into walls. With ≤ 45 bodies the cost difference was not measurable in these Editor runs. **The rare deep overlap (0.40) means this is reduced, not eliminated** (PR-13).

## Targets proposed for a modest Android phone (to be measured by the owner)

| Metric | Target |
|---|---|
| Frame rate | Steady 60 fps (the game sets `targetFrameRate` 60); p95 frame ≤ 20 ms |
| Memory | Total ≤ 250 MB PSS; no growth over a 15-minute session |
| Startup | Splash → menu ≤ 3 s on a 2020-era mid-range phone |
| Thermal/battery | No throttling in 15 minutes; note battery drop per 15 minutes |

Measure with a Development build plus the Unity Profiler (USB), or Android Studio's profiler. **None of these were measured here.**

## Other checks (code inspection)

No `Update` polling of progression or rewards (event-driven); no `FindObjectOfType` at runtime; disk writes happen only on state changes and lifecycle events; item and effect objects are pooled; particles have fixed emission counts; textures are ≤ 512 px with platform compression for items and uncompressed small UI sprites (see `VisualAssetManifest.md`); no network use.
