# Desi Merge — Save System, Local Data and Reward Economy

Static inspection (2026-11-03). Context: the game is **offline-first and single-player** with no leaderboard, purchases or
server. Editing one's own save or changing the phone clock is *expected local cheating*, not a security breach, unless it
would also affect other players or money. Each finding states which category it is in.

## 1. Persistence design (verified in code)

| Property | Implementation |
|---|---|
| Location | `Application.persistentDataPath/saves` — app-private internal storage (`GameBootstrapper` line 64) |
| Format | `JsonUtility` plain JSON per key, no encryption/signature |
| Atomic write | `FileSaveStore.Write`: write `.tmp`, then `File.Replace(tmp, main, .bak)` (line 59) |
| Recovery | `JsonRepository.Load`: main → `.bak` → defaults; unreadable main copied to `<key>_corrupt` first |
| Read errors | If the store *throws* on read, the session uses defaults and `_writeBlocked` prevents overwriting the real file |
| Validation | Each service clamps tampered values on load (e.g. `PlayerProgressService.Migrate`: non-negative score/coins/streaks, list caps; `PowerUpService` clamps charges to `maxSavedCharges`) |
| Save timing | Claims and grants flush immediately; everything else flushes on game over, return to menu, app pause, focus loss and quit (`GameServices.FlushAll`) |
| Idempotent rewards | Every reward is applied under a stable grant id (`RewardClaims.GrantAll`); `TryGrantCoins`/`TryGrant` return "already applied" for a known id |

## 2. What a local player can manipulate

| Target | How | Category |
|---|---|---|
| Diamonds, best score, stats, unlocks, discoveries | Edit `saves/*.json` (needs root, a debuggable build, or a backup-restore tool). Values are only clamped to valid ranges, not verified. | Expected offline cheating. Would be **HIGH** if a leaderboard, cloud sync or real-money store were added. |
| Daily rewards, daily/weekly missions, daily challenge | Change the device date (no root needed) — DM-02 | Expected offline cheating; would matter with any purchasable or shared economy. |
| Power-up missions | Restart farming — DM-04 | Gameplay-design weakness, low impact. |

No path was found where *another* party (app, website, network) can read or alter the saves: no INTERNET permission in the
release APK, no exported content providers, saves in internal storage. Android auto-backup applies (DM-16).

## 3. Findings

### DM-02 — Clock manipulation yields unlimited daily rewards and missions (LOW for offline; CODE-VERIFIED)

* **Where:** `Core/IClock.cs` `DayKey.Effective` (58–66), `Progression/DailyRewardService.cs` `Refresh` (125) / `TryClaim`
  (163); the same `Effective` day drives `MissionService.Refresh` (188) and the daily challenge.
* **Forward:** the device date is trusted when it moves forward. Setting the date +1 day, claiming, +1 day, claiming… gives
  one full calendar reward per step.
* **Backward:** if the device date is more than `ClockToleranceDays` (7) before the latest day seen, `Effective` returns the
  device day; `Refresh` then clears `lastClaimDay` because it is in the "future", so a new claim opens under a **new** grant
  prefix (`dailyreward:<day>`). Jumping back another 8+ days repeats it.
* **Not possible:** re-claiming the *same* day for a second reward — the grant id `dailyreward:<day>:<i>` is already applied
  (as long as it is still remembered, see DM-07).
* **Fix (only if it matters):** server time is out of scope offline. Options: cap claims per real elapsed time using
  `SystemClock.elapsedRealtime`-style monotonic time stored across sessions, or simply accept as offline cheating (document it).

### DM-03 — Power-ups bought with Diamonds don't count as "used" (LOW, CODE-VERIFIED)

* **Where:** `Gameplay/PowerUpController.cs` `Consume` (157–165): when no free/saved use is left, it calls
  `_wallet.TrySpendCoins(...)` and returns `Used` **without** calling `PowerUpService.TryConsume`, so `PowerUpService.Used`
  is never raised.
* **Effects:** `MissionService.OnPowerUpUsed` (344) never runs for bought uses →
  1. `_powerUpUsedThisGame` stays false, so **"Finish N games without power-ups" missions are credited even when the player
     used bought power-ups** (reward without valid completion);
  2. "Use N power-ups" / "Use the Hammer once" missions don't count bought uses (progress too low).
* **Repro:** use up the free Hammer and saved charges, buy a Hammer with Diamonds and smash an item, finish the game → the
  "without power-ups" daily mission advances.
* **Fix:** raise `Used` for bought uses too (e.g. a `PowerUpService.RecordPurchasedUse(id)` that fires `Used`/`Changed`).
* **Regression test:** EditMode — buy-and-use advances `UsePowerUps` and resets the no-power-up flag.

### DM-05 — Diamonds spent on a power-up are not saved immediately (LOW, CODE-VERIFIED)

`PowerUpController.Consume` → `PlayerProgressService.TrySpendCoins` (169) marks dirty but does not flush; the next flush is at
game over / pause / menu. A hard crash (not a normal background, which flushes) refunds the Diamonds. Player-favourable; no
duplication of anything else.

### DM-06 — Skin purchase writes two files in sequence (LOW, CODE-VERIFIED)

`Cosmetics/CosmeticService.cs` `TryBuy` (102–127): spends Diamonds, `wallet.Flush()` (progress file), then `Flush()` (cosmetics
file). A crash between the two writes leaves Diamonds spent and the skin not owned. Window is a few milliseconds.
Fix: write the ownership first and spend second, using a purchase grant id so a retry can't charge twice.

### DM-07 — Grant-id memory is capped at 200 and shared with per-game Diamond grants (INFORMATIONAL, CODE-VERIFIED cap; impact SUSPECTED)

`PlayerProgressData.MaxRememberedCoinGrants = 200`; `GameplayLoop.AwardGameDiamonds` (458) adds one id per finished game
(`game:<session>:<total>`). After ~200 games the ids of old daily-reward/mission/achievement coin grants are evicted
(`TryGrantCoins` lines 57–61). Idempotency for those old claims then depends only on the claim file. It becomes a
duplication path only together with DM-08 (claim file rolled back). Fix: keep per-game grants out of the shared list, or key the
cap per source.

### DM-08 — Files recover independently from their own backups (LOW, SUSPECTED)

Each `JsonRepository` falls back to its own `.bak`. If, for example, `dailyrewards.json` is corrupt and restored from `.bak`
while `progress.json` is current, the claim record is one version older than the granted Diamonds. Today's re-claim is still
blocked by the grant id (unless evicted, DM-07). Not reproduced; requires file corruption.

### DM-19 — `TryGrantCoins` has no overflow guard (LOW, CODE-VERIFIED, needs a tampered save)

`PlayerProgressService.TryGrantCoins` line 57 uses `_data.coins += amount`, unlike `AddCoins` which saturates at
`long.MaxValue`. Only reachable if the save is edited to near `long.MaxValue`; the wrapped negative value is clamped to 0 on the
next load (`Migrate`). Fix: reuse the saturating add.

## 4. Economy behaviour checked and found sound

* Claim buttons are idempotent: `DailyRewardService.TryClaim` (163), `MissionService.TryClaim` (204) and
  `MilestoneService.TryClaim` (166) re-check eligibility and grant through `RewardClaims.GrantAll`, which checks **all**
  rewards fit before applying any (no partial grants), and only then records the claim and flushes.
  `DailyChallengeService.TryClaim` (163) grants its single reward with `IRewardGranter.Grant` under a stable id, then records
  the claim — same grant-first ordering.
* Exported components: only the launcher activity (`UnityPlayerGameActivity`, `exported=true`, required for the launcher);
  no content providers or exported services/receivers in the built APK manifest (checked with `aapt2 dump xmltree`).
* Grants are written before the claim record (`PowerUpService.TryGrant` and `TryGrantCoins` flush immediately), so an
  interrupted claim can be retried without double reward.
* Negative Diamonds are impossible through code (`TrySpendCoins` refuses insufficient balance; load clamps to ≥ 0).
* Power-up storage overflow is refused (`CanGrant` vs `maxSavedCharges`) and reported as "storage full" instead of being lost.
* Game-over Diamonds after a revive are topped up, not paid twice (`AwardGameDiamonds` grant id includes the total).
