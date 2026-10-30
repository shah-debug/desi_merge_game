# Desi Merge: Screen Flow and Navigation (Phase 6.6)

```
App start ─► Bootstrap.unity: services + saves load ─► Splash (logo intro ≤ 0.9 s, runs while MainMenu loads)
                                                            │
                                                            ▼
                                                     Main Menu ◄──────────────────────────┐
     ┌───────────┬───────────┬───────────┬──────────────┼─────────────┐                    │
 Rank card   Daily Rewards  Missions   Collection     Awards      Settings                 │
 (Ranks)     (7-day)       (Daily/Weekly/Completed)                                         │
     └─── each opens one panel; the reward popup opens above it after a claim ───┘          │
                                                            │ PLAY                           │
                                                            ▼                                │
                                                 Gameplay ─► first game only: How to Play (4 pages, skippable)
                                                    │  ├─► Pause ─► Resume / Restart (confirm) / Main Menu ──┤
                                                    │  └─► Game Over (results) ─► Play Again / Main Menu ─────┘
```

- The splash exists only in `Bootstrap.unity`, so it is shown once per app launch, never when returning from gameplay. Saves are loaded by the bootstrapper **before** the menu is activated.
- **No artificial delay:** the menu activates when both the load and the short logo intro are done.
- The tutorial shows only when `settings.tutorialCompleted` is false **and** the player has no finished game. Skip, the last page or Android back ends it for good.

## Rules

| Rule | How it is enforced |
|---|---|
| One screen at a time | `MainMenuEntryPoint.CanOpenPanel()`: no panel opens while another panel or the reward popup is open, or while a scene loads |
| No double scene loads | `SceneNavigator.IsLoading`; menu buttons disable after Play |
| No UI input into gameplay | HUD pointer blockers plus the tutorial overlay; modals use full-screen raycast dims |
| No repeated subscriptions | Panels unbind before binding, and unsubscribe in `OnDestroy`; services subscribe once in their constructors |
| Progress never lost on exit | `FlushAll` at game over, when leaving gameplay, and on app pause/focus loss/quit; reward claims save immediately |

## Android back button

| Where | Back does |
|---|---|
| Menu with the reward popup | Collect (close the popup) |
| Menu with a panel open | Close that panel |
| Bare menu | Quit the app (saves first) |
| Gameplay, tutorial visible | Close the tutorial |
| Gameplay playing / paused | Pause / resume (existing) |
| Hammer targeting | Cancel targeting (existing) |
| Game over | Ignored (choose a button) |

## Screens

| Screen | Contents |
|---|---|
| Splash | Truck-art background, mango with a flower and glow, "Desi Merge" plate, tagline; spinner only if loading takes over 1.2 s |
| Main menu | Rank card (current rank, progress bar, next rank, dot), Settings, title plate, best score, PLAY, Daily Rewards (dot), Missions (dot), Collection, Awards |
| Daily Rewards | 7 tiles (claimed / today / upcoming), streak, missed-day rule, Claim, storage-full and clock messages |
| Missions | Tabs Daily / Weekly / Completed; reset countdown; Today's Challenge; mission cards with progress, reward and Claim; empty states |
| Ranks | 7 rank cards: reached and claimable, done, or locked with the target score |
| Reward popup | Title, reason, reward icons and amounts, Collect |
| Collection / Awards / Settings | Phase 6.5 screens; collection hints; award rows show their reward |
| Gameplay HUD | Phase 6.5 HUD; discovery toast with item art; mission, award and rank toasts |
| Game over | Score, improvement over the old best, NEW BEST, best, summary (merges, new items, new rank, awards, mission rewards waiting), Continue (only with an ad provider), Play Again, Main Menu |

The shop is not present (see `RewardAndMissionSystem.md` → Economy).
