# Desi Merge: Player Profile and HUD

## Profile badge (main menu, top-left)

The old "New Player" text card is now a compact profile badge:

| Part | New player | Returning player | Source |
|---|---|---|---|
| Avatar (in a saffron ring) | Default face icon (original "Profile" icon) + a small magenta **NEW** badge | Art of the **biggest item they have made** (the badge disappears) | `Progress.HighestItemId` |
| Name line | "New Player" | Their **rank title**, e.g. "Naya Khiladi", "Chai Champion" | `IMilestoneService.Current` |
| Progress bar + "Next: … at …" | Progress to the first rank | Progress to the next rank | `IMilestoneService` |
| Red "!" dot | — | A rank reward is waiting | `Milestones.ClaimableCount` |
| Tap | Opens Ranks | Opens Ranks | existing screen |

It updates through events (`Progress.Changed`, `Milestones.Changed`); there is no polling.

**Not implemented, by choice:**
- There is **no display-name entry**: the game has no name data and no accounts, so the rank title is the name line. Adding a local name would need a text field and a new saved field; it is left as an option.
- There is **no XP or level system**: the score ranks are the real progression, and no separate level was invented. Everything stays offline and local; there is no login.

## Gameplay HUD (top bar, 1080-wide reference)

```
[☰ 124]  SCORE        👑 BEST          (NEXT 160)  [⏸ 124]
         12345         4321
```

- **Menu button:** teal, 124 × 124 (≥ 48 dp touch target), left end of the bar, with the same press feedback as all buttons.
- **Score and best:** moved right to make room; sizes unchanged (score 96 px, best 60 px), so nothing was shrunk.
- **Next and pause:** unchanged on the right.
- **Power-ups and hint pill:** unchanged below the bar.
- The bar sits inside the safe area. On short screens the whole top HUD scales as one (`ResponsiveScaler`, as before). The menu card and every modal card scale the same way.
- HUD presses never drop items: the top bar is a pointer blocker, and the game is paused while the menu is open.

Screenshots: `Screenshots/phase6.6ui-*.png` (9:16, and 19.5:9 with a simulated notch).
