# Desi Merge: Game Navigation (In-Game Menu)

## Gameplay

```
Top bar:  [☰ menu]  SCORE  👑 BEST  ............  (NEXT)  [⏸ pause]
           │
           └─ tap ─► game pauses (time, physics, drops, timers, music) ─► MENU dropdown (top-left)
                       RECORDS · AWARDS · COLLECTION · DAILY (•) · MISSIONS (•) · SETTINGS · HOW TO PLAY · HOME
                       [ RETURN TO GAME ]
                       each item opens the real screen inside the Gameplay scene (no scene load)
```

- **Records** opens on the Score tab; **Awards** opens Records on the Achievements tab.
- **Home** returns to the main menu through the existing flow, which saves first.
- **How to Play** shows the tutorial over the paused game; closing it returns to the menu.
- The **pause** button is unchanged: Resume, Restart with confirmation, Main Menu.
- Dots on Daily and Missions appear when a reward is waiting.

## How the menu closes

| Action | Result |
|---|---|
| Return to Game, the X button, or a tap outside the menu card | Menu closes and the game resumes |
| Android back with a screen open (popup, Records, Collection…) | That screen closes; the menu stays |
| Android back on the bare menu | Menu closes and the game resumes |
| Android back with the tutorial open | Tutorial closes |
| Menu button tapped again while open | Nothing; there is never a second menu |
| Item tapped while another screen is open | Ignored (one screen at a time) |
| Outside tap or Return while a screen is open | Ignored (close the screen first) |

## Architecture

- `GameplayLoop.OpenMenu()` is the same pause as `Pause()` (time scale 0, drops disabled, music paused, state Paused), but it shows the menu instead of the pause card. `Resume()` closes either. The loop alone owns game state; UI never changes time or physics.
- `IGameplayView` gained `MenuRequested`, `ShowMenu()` and `HideMenu()`. `GameplayHud` implements them with a `GameplayMenu`.
- `GameplayMenu` is one reusable component: it owns the dropdown and the real screens (`RecordsPanel`, `CollectionPanel`, `SettingsPanel`, `DailyRewardsPanel`, `MissionsPanel`, `RewardPresenter`). They are bound once to the app's existing services (`Bind(GameServices)`) and never create data of their own. Listeners are added in `Awake`/`Bind` and removed in `OnDestroy`; there is no `Update` polling.
- The main menu keeps its own panels (it was already one-at-a-time with back support). Its **Awards** tile became **Records**.
- Animations: `PanelTransition` (scale and fade on unscaled time, so it works while paused) and `ButtonFeedback` for presses. Tab switches are instant. No new tween package.
