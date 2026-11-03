# Desi Merge — Performance and Resource Risks

**No profiler, FPS, memory or ANR data was collected for this audit.** Everything below comes from code inspection and the
build output; it identifies risks, it does not prove runtime performance. Earlier phases ran a 15-minute Editor stress test
(Phase 6.7); no device performance capture exists.

## 1. Bounded by design (no finding)

| Resource | Bound |
|---|---|
| Board items | Container + danger line ends the game; object pool reuses `MergeItem`s |
| Merge processing | One resolver pass per FixedUpdate over that step's contacts; no recursion |
| Toasts | Queue capped at 6 (`ToastPresenter.MaxQueued`) |
| Score history / grant ids | Capped (10 recent games, 200 grant ids per list) |
| Audio sources | 1 music + fixed SFX voice pool |
| Particles | Two pre-built systems (`MergeEffects`), max 300 / 24 particles |
| Save writes | Small JSON files, written only when dirty; claim flushes are user-triggered |
| Per-frame components | `IconBeat` ×2, `FocusShade` and `DailyRewardsPanel` countdown run only while active/open; `ToastPresenter`, `PowerUpEffects` disable themselves when idle |

## 2. Risks (inspection only)

* **DM-15 (INFORMATIONAL):** `MissionService.OnMergeRecorded` (316) calls `Refresh()` on every merge, and `Refresh`
  recomputes the effective day and re-runs mission selection (`Roll`) for daily and weekly pools. Achievement, milestone and
  daily-challenge services also react to each merge. Cost per merge is small lists and LINQ-free loops, but it runs several
  times per second during chains and allocates (`RemoveAll` lambdas, `new[]` arrays). Not measured.
* **DM-09 side effect (LOW):** each newly discovered skin level re-skins every board item (`MergeBoard.RefreshSkins`). It only
  happens the first time a level is seen per skin, so it is rare.
* **Synchronous save on the main thread:** `FlushAll` on pause/focus-loss writes up to nine small files with `File.Replace`.
  Expected to take milliseconds; on very slow storage it could add a short hitch when backgrounding. Not measured.
* **Localization relayout:** switching language triggers every `LocalizedText`; Urdu wrapping measures text word by word
  (`Loc.WrapRightToLeft`). One-off cost on the Settings screen, not per frame.
* **Build size:** release APK 35.2 MB (35,212,748 bytes, `Builds/Android/DesiMerge-rc-unsigned-test.apk`, 2026-11-03).
  Generated art is 256-px PNGs plus a bundled Noto Naskh Arabic font. No sprite atlas review was done.

## 3. What is needed to close this area

A device capture (Unity Profiler over USB or `adb shell dumpsys gfxinfo`) of a 10-minute game on a low-end Android phone:
frame time during merge chains, GC allocations per merge, memory after several restarts, time spent in `FlushAll` on
background.
