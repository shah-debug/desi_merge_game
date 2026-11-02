# Desi Merge: Diamonds and Gameplay Polish (2026-11-02)

## Fixes requested after the UI phase

| Request | What changed | Evidence |
|---|---|---|
| BEST stayed 0 while the score rose | The HUD's BEST shows max(saved best, current score) live. The saved best still changes only at game over (unchanged rules). | `PolishTests.BestScore_FollowsTheScoreLive…` |
| "HOW TO PLAY" / "LET'S PLAY" not centred | New `IconLabelLayout` keeps icon + text centred as one group and re-centres when the text changes; long text shrinks to fit. Added to every icon + label button. | `PolishTests.ButtonLabels_StayCentred…`, screenshot |
| Professional font | Body text uses **Poppins SemiBold** (SIL OFL 1.1, from github.com/google/fonts; licence `Art/Fonts/Poppins-OFL.txt`). Titles stay Lilita One. | Screenshots |
| Progress bars curved at the ends | They were a pill drawn with *Filled*, which squashes the round ends. New `SlicedFill` keeps the pill sliced and sets its width from `fillAmount` (with a full round cap at small values). Used by missions, ranks, records, today's challenge and the profile bar. | `PolishTests.ProgressFills_KeepTheirRoundedShape` |
| Hammer: zoom in, then a hammer smashes | Camera zooms to 88% while Hammer waits for a target. On tap, an illustrated hammer swings down, and a copy of the item's art squashes with a burst. | `PolishTests.Hammer_ZoomsIn…`, screenshot |
| Shuffle: zoom in, then shuffle | The camera zooms in briefly; every item's art glides from its old place to its new one. | `PolishTests.Shuffle_ZoomsIn…` |

All effects are visual only (`PowerUpEffects`, `ItemTravel`): rules, physics and scores are unchanged, and the existing power-up tests still pass. Found by the tests and fixed: the effects component never subscribed to Shuffle, because it disabled itself before `OnEnable`.

## Diamonds 💎 (new real currency)

| Part | Implementation |
|---|---|
| Storage | The existing saved `coins` balance in `player_progress.json`, shown as Diamonds. Schema 4 adds `appliedCoinGrants` (one-time reward IDs, max 200). Old saves load at 0 diamonds, with nothing reset. |
| One-time payouts | `TryGrantCoins(grantId, amount)` pays each reward ID once, ever, and saves immediately. The previous `AddCoins` path had no such guard; all rewards now use the new one. |
| Earning | **1 💎 per 50 points** at game over (a revived game is topped up, never paid twice), shown on the results card. **Daily rewards:** Day 1–7 = 20, 25, 30, 35, 40, 50, 100 💎, plus the existing power-ups. |
| Spending | When Hammer or Shuffle has no free use or saved charge left, its button shows a price badge: **Hammer 30 💎, Shuffle 20 💎** (`PowerUpDefinition.diamondPrice`). Diamonds are taken **only when the power-up works**: a miss, a cancel or no room costs nothing. Not enough diamonds: the button dims and a toast says how many are needed. |
| Counters | 💎 pill at the top-left in gameplay (under the menu button) and on the main menu (next to settings). It updates live and pops when the amount grows. |
| Not changed | Missions, ranks and achievements still pay power-up charges. |

Tests:
- EditMode `DiamondEconomyTests` (4): one payout per ID and saved at once; never negative; interrupted daily claim not paid twice; shipped prices and calendar.
- PlayMode `DiamondFlowTests` (2): price badge, a miss or cancel is free, the purchase costs exactly 30, can't buy without enough; earning per 50 points, once.
- `ShippedCatalogs…` now accepts Diamonds as a real reward (it previously required power-ups only).

## Results (final runs)

- EditMode **226/226**, PlayMode **128/128**.
- Android build **succeeded**: 0 errors, 2 pre-existing `UAC0009` deprecation warnings.
- APK `Builds/Android/DesiMerge-rc-unsigned-test.apk`, 31.4 MB, debug-signed. **Not tested on a phone.**

## Ads (question answered, nothing added)

The game has **no ads and no reserved ad space**. Real banner ads need an ad SDK (e.g. AdMob), the INTERNET permission back, user consent, a privacy policy and Data Safety updates. That is a separate phase if wanted.

## Known small items

- The calendar note still says "Rewards are added to your power-ups"; it should also mention diamonds (text only).
- Prices and earning rates are starting values; they are tuned in `PowerUps.asset` and `GameplayLoop.PointsPerDiamond`.
