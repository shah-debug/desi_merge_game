# Desi Merge — Gameplay, Physics and Input Weaknesses

Static code inspection only (2026-11-03). "CODE-VERIFIED" = the failing path was traced in the source; nothing in this file
was reproduced at runtime during this audit. Finding IDs refer to `ExploitabilityRegister.md`.

## 1. Areas checked and found sound (no finding)

| Concern | Evidence |
|---|---|
| Duplicate merge events / double scoring | `MergeResolver.ResolvePending` marks both items consumed before calling the handler; both bodies' reports of one contact collapse to one merge. `Report` rejects consumed or different-level pairs. Items created by a merge are resolved next step (buffer swap), so chains never recurse. |
| Pooled object reuse during a step | `MergeBoard.RetireMerged` keeps merged items out of the pool until the step ends (`ReturnRetiredToPool`), so a stale contact can't merge a reused object. Hammer removal calls `_resolver.Forget(item)`. |
| Merged item spawning inside a wall / escaping | `MergeBoard.InsideContainer` clamps the result inside the walls and above the floor; items use `CollisionDetectionMode2D.Continuous` + interpolation (`Editor/GameplaySetup.cs` 205–206). |
| Game over raised repeatedly | `_dangerRaised` latch in `MergeBoard.CheckDangerLine`; `GameSession.TryEnd` makes `GameplayLoop.EndGame` run once. |
| Drops during pause / game over / leaving / Hammer | `GameplayLoop.TryDrop` checks `_leaving`, `PowerUps.IsTargeting` and `DropRules.CanDrop(sessionOver, state==Playing, cooldown)`. Pause sets `timeScale = 0` and `InputEnabled = false`. |
| Taps on HUD also dropping | `DropController.HandlePressStarted` arms a drop only if the press began off the HUD (`PointerFilter = GameplayHud.BlocksPointer`); every modal opened during play pauses first (`OnSkinTagClicked`/`OnGetDiamondsClicked` raise `MenuRequested`). |
| Multi-touch | Input uses `<Pointer>/press` + `<Pointer>/position` (primary pointer only); a second finger doesn't create a second drop. |
| Late revive callback | `GameplayLoop.OnReviveAdFinished` checks `_sessionGeneration`, `_revivePending`, `_disposed`, `_leaving`. Release builds use `UnavailableRewardedAdService`, so revive is hidden. |
| Restart / menu during merges | `Restart` and `ReturnToMainMenu` run from UI callbacks (Update), board resolution runs in FixedUpdate; `MergeBoard.Clear` clears resolver, active and retired lists. `_leaving` blocks repeated "Main menu". |
| Score overflow | `GameSession.Score` is `long`; per-merge points are small `int`s. Not reachable. |
| Random selection | `SpawnQueue` draws uniformly from `[0, SpawnableLevelCount)`; constructor rejects `< 1`. |
| Unbounded objects | Item count is bounded by the container (game over when the pile passes the danger line); pool default capacity 32, grows as needed. Toast queue capped at 6. |

## 2. Findings

### DM-01 — In-progress game is not saved; Android process death loses the run (MEDIUM, CODE-VERIFIED)

* **Where:** `Gameplay/GameplayEntryPoint.cs` `OnApplicationPause` (148) / `OnApplicationFocus` (156) only pause;
  `Core/GameBootstrapper.cs` flushes services on pause. No board/session serialization exists anywhere
  (`MergeBoard` and `GameSession` have no save path).
* **Failure:** the player backgrounds the game mid-run (call, app switch). Android may kill the process to reclaim memory.
  On relaunch the game starts at the splash/menu: the board, the current score, and the game-over Diamonds for that run are
  gone, and the run is never submitted as a final score (no `GamesPlayed`, no best score).
  Merges and discoveries made before the pause *are* kept (they are flushed on pause).
* **Repro:** start a game, score some points, press Home, run `adb shell am kill com.DefaultCompany.DesiMerge` (simulates
  low-memory kill of a background app), reopen the app.
* **Expected (typical for merge games):** the run resumes, or at least its score is recorded.
* **Fix:** persist a small board snapshot (item levels, positions, score, queue, session id) on pause and offer "Continue"
  on the next launch; or at minimum submit the score as a finished game on pause-kill.
* **Regression test:** PlayMode — snapshot round-trip restores items/score; EditMode — snapshot schema validation.

### DM-04 — Free power-up uses reset on Restart, enabling mission farming (LOW, CODE-VERIFIED)

* **Where:** `PowerUps/PowerUpService.cs` `BeginGame` (156) resets `_freeUsesLeft` on every `GameplayLoop.Begin`
  (called by `Restart`, line 366).
* **Failure:** drop one item → use the free Hammer on it → Restart → repeat. Each cycle counts as a power-up use for
  `UsePowerUps` missions (`MissionService.OnPowerUpUsed`, 344) at no cost. Daily/weekly "use N power-ups" missions can be
  completed in seconds. Impact is bounded by the mission reward table (one claim per mission per period).
* **Fix (if undesired):** count mission progress only for power-ups used in games that end (game over), or require a
  minimum number of drops before a use counts.

### DM-09 — Every new skin-level discovery is treated as a skin change (LOW, CODE-VERIFIED)

* **Where:** `Cosmetics/CosmeticService.cs` `RecordDiscovery` (129–143) raises `Changed`; `GameplayEntryPoint.OnSkinChanged`
  (129) subscribes to `Changed` and calls `board.RefreshSkins()` and `Loop.RefreshItemPreviews()`, which calls
  `DropController.ShowPreview` immediately.
* **Failure:** a merge that creates a level not yet seen with the current skin (`GameplayLoop.OnItemMerged` →
  `RecordDiscovery`) inside the drop cooldown cancels the delayed preview (`ShowPreviewAfterCooldown`) and shows the next item at
  the aim point at once, overlapping the item just dropped. It also re-skins every item on the board. Visual only; no score
  or physics effect.
* **Fix:** raise a separate `DiscoveryRecorded` event, or have `OnSkinChanged` react only when the selected skin id changes.

### DM-10 — Skin preview index mismatch (INFORMATIONAL, CODE-VERIFIED)

`GameplayEntryPoint.Initialize` uses `Mathf.Min(5, Count - 1)` for the Skin tag preview, `OnSkinChanged` (132) uses a fixed
`SpriteFor(5)`. With fewer than 6 items `SpriteFor(5)` would fall back to null → default art; current content has 10 items, so
not triggered.

### DM-18 — Danger timer compares two clocks (INFORMATIONAL, CODE-VERIFIED)

`MergeBoard.SpawnLocal` stamps items with `_clock()` (`Time.time`, set in `GameplayEntryPoint`), while `CheckDangerLine`
evaluates with `Time.fixedTime`. The difference is at most one fixed step (0.02 s) against a multi-second grace period; no
practical effect.

## 3. Input abuse summary

| Attempt | Result (from code) |
|---|---|
| Rapid tapping to drop faster | Blocked by `DropCooldownSeconds` in `TryDrop`. |
| Tap during pause / modal / game over / scene load | Input disabled or `_leaving`; modals pause first. |
| Double-tap Restart / Main menu / claim buttons | Restart is synchronous and idempotent; menu guarded by `_leaving`; every claim re-checks `CanClaim` / grant IDs. |
| Hammer on empty space | `MissedTarget`, keeps waiting, no charge used (`PowerUpController.SelectTarget`). |
| Shuffle with no room | Plan validated before anything moves (`MergeBoard.TryShuffle`), refused with a toast, no charge. |
| Buying a power-up then using it | Diamonds spent only after the effect succeeds — but see DM-03/DM-05 in SaveAndRewardSecurity.md. |
| Restart to refresh free uses | Possible — DM-04. |
