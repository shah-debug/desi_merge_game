# Desi Merge: Rewards, Missions, Ranks and Achievements (Phase 6.6)

All of this is offline, stored on the device, and driven by real gameplay events. There is no server, account or currency purchase.

## Economy

| Reward | Real? | Where it goes |
|---|---|---|
| **Hammer / Shuffle charges** | Yes. They are spent in play after the free use each game. | `power_ups.json` (Phase 5). Maximum **9 saved per power-up**. |
| Coins | Dormant since Phase 1; nothing spends them | **Not used or shown** (no fake currency) |
| Cosmetics | Foundation only; nothing renders them yet | **No shop** (deferred until cosmetics change the game's look) |

Every grant has a stable grant ID (`dailyreward:<day>:<i>`, `mission:d<day>:<id>:0`, `mission:w<week>:<id>:0`, `milestone:<id>:0`, `achievement:<id>`). `PowerUpService.TryGrant` applies each ID at most once and saves before the claim is recorded. A claim interrupted between the grant and the save is completed on the next tap **without paying twice** (tested).

**Full storage:** if a reward would push a power-up past 9, the claim is refused with a message ("Hammer storage is full (9/9). Use one in a game, then claim."). The reward stays claimable for that day or period; nothing is lost or partly paid.

## Date policy (all daily systems)

- A day is a **UTC calendar day** (`DayKey`): it changes at **00:00 UTC = 05:00 Pakistan time**, whatever the device's time zone.
- A week starts **Monday 00:00 UTC** (`DayKey.WeekOf`).
- The effective day is `max(device day, latest day seen)`. Moving the clock **back** never re-opens a claimed reward or resets missions. The daily rewards screen says so if it detects it.
- **Limitation:** moving the clock **forward** is believed. A local-only game cannot prevent this without a trusted server time. This is not anti-cheat.

## Daily rewards (7-day calendar)

Data: `ScriptableObjects/Progression/DailyRewards.asset` (`DailyRewardCatalog`, editable). Save file: `daily_rewards.json`.

| Day | Reward |
|---|---|
| 1 | Hammer ×1 |
| 2 | Shuffle ×1 |
| 3 | Hammer ×1 |
| 4 | Shuffle ×1 |
| 5 | Hammer ×2 |
| 6 | Shuffle ×2 |
| 7 | "Mela bundle": Hammer ×2 + Shuffle ×2 |

Rules:
- One claim per UTC day.
- Claiming on consecutive days moves along the calendar; after Day 7 it starts again at Day 1, and the streak keeps counting.
- **Missed day:** if a whole UTC day passes without a claim, the calendar restarts at Day 1 and the streak at 1. The best streak is kept.
- Tiles show claimed (check mark), today (glow), and upcoming.
- Claim is disabled while processing and after claiming ("See you tomorrow"). Rapid taps and scene reloads cannot claim twice (tested).

## Missions

Data: `Missions.asset` (`MissionCatalog`). Save file: `missions.json`.

- **3 daily** missions (new each UTC day) and **3 weekly** missions (new each Monday UTC), chosen from the catalog by rotation, so the same day always shows the same missions.
- **Reset policy:** when a period ends, its missions are replaced and any **unclaimed rewards expire**. Claimed missions stay listed under *Completed* until their period ends.
- The *Daily* tab also shows **Today's Challenge** (the Phase 4 daily challenge, unchanged, with its own claim).

| Type | Event that advances it |
|---|---|
| CompleteGames | A finished game (`GameCompleted`, once per game, including after a revive) |
| Merges | Each recorded merge |
| ScoreInOneGame | Best final score of a game in the period (also the score after a revive) |
| CreateItem | Merging into that item **or any higher one** |
| UsePowerUps | Each successful power-up use (optionally only one power-up ID) |
| GamesWithoutPowerUps | A finished game in which no power-up was used (tracked from `PowerUpService.GameBegan`/`Used`) |

Shipped pool:
- Daily (10): finish 1 / 3 games; merge 10 / 25; score 300 / 500 in one game; make a Roti; use Hammer; use Shuffle; finish a game without power-ups. Each pays 1 charge.
- Weekly (6): 15 games; 150 merges; 1000 in one game; make a Chai; 5 power-ups; 3 games without power-ups. Each pays 2 charges.

There is no polling: progress changes only inside event handlers. Each service subscribes once and unsubscribes in `Dispose` (tested).

## Score ranks (milestones)

Data: `Milestones.asset` (`MilestoneCatalog`). Save file: `milestones.json`. Based on the **saved best score**, which is never lowered. Thresholds were chosen from the existing pacing: the daily challenges ask for 300 and 600, the old award asks for 500, and item scores are 1–55 per merge.

| Rank | Best score | Reward |
|---|---|---|
| Naya Khiladi | 100 | Hammer ×1 |
| Bazaar Regular | 250 | Shuffle ×1 |
| Chai Champion | 500 | Hammer ×2 |
| Dhaba Star | 800 | Shuffle ×2 |
| Truck-Art Ustaad | 1200 | Hammer ×2 |
| Rickshaw Racer | 1800 | Shuffle ×3 |
| Bus Badshah | 2500 | Hammer ×3 |

- A rank unlocks when a final score is **≥** its threshold (boundary tested at 99 and 100). It is announced once in play ("New rank: …").
- **Existing players** keep their best score. Ranks it already earns are marked reached at startup, without a pop-up, and their rewards are claimable.
- The menu's rank card shows the current rank, a progress bar to the next one and a dot when a reward is waiting.

## Achievements (Phase 4, extended)

The original 8 IDs are unchanged. 3 were added: `first_merge`, `discover_aam`, `score_1000`. The 8 originals had no reward; they now give Hammer or Shuffle charges (1–3). Rewards are **applied automatically when the achievement unlocks**, so the "claimed" state is the unlocked state, and the row says "received". Existing players who already unlocked achievements receive those rewards once, at the next start, if storage has room; otherwise the grant is retried at a later start.

## Collection and discovery

The first discovery of an item raises `ItemDiscovered` once. Gameplay then shows a toast with the item's art and plays the unlock sound, and achievements and missions update through the same events. Locked collection slots show a hint ("2× Nimbu") only when the item that makes them is already discovered; otherwise "???". The first item says "Drop it".

## Reward presentation

`RewardPresenter` is the single popup for daily rewards, missions and ranks. It shows the title, the reason, up to two reward icons with amounts, a rotating truck-art flower behind, and the claim sound. It appears **after** the service has applied and saved the reward. Several rewards queue, so only one popup is ever open. Android back = Collect.

## Save files

| File | Schema | Contents |
|---|---|---|
| `daily_rewards.json` | 1 | last claim day/index, streak, best streak, total claims, latest day seen |
| `missions.json` | 1 | latest day seen; per active mission: id, period, period key, progress, completed, claimed |
| `milestones.json` | 1 | reached IDs, claimed IDs |
| `settings.json` | **3** | adds `tutorialCompleted` (older files load with it false; the tutorial is skipped anyway for players with a finished game) |

All files use the existing `JsonRepository` (validated load, backup copy, recovery). Missing files mean defaults. No existing file was reset or migrated destructively.
