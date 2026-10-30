# Desi Merge: Visual Design System

The single source of truth is `Assets/_Project/Settings/UI/UITheme.asset`. `ContentSetup.ApplyDesignSystem` writes the values below into it, and every screen is generated from the theme by `UiFactory`. To restyle the game, change the theme or the generators, then run **Desi Merge > Setup > Apply Visual Redesign** (or `Rebuild Generated Scenes and Prefabs` for layout-only changes).

## Direction

A premium casual puzzle game with Pakistani truck-art accents used as **trim, not wallpaper**: border strips, flower ornaments, patterned walls and tassels on the rickshaw. Play areas stay calm (cream field, soft gradient), so the illustrated items are the most saturated things on screen.

## Colour

| Token | Hex | Role |
|---|---|---|
| `background` | `#FFF4DE` | Warm cream base (camera clear colour) |
| `backgroundGradient` | `#FFF7E8` → `#FCE3C2` | Full-screen gradient behind menu and board |
| `card` | `#FFF9EE` | Modal cards, daily card |
| `deep` | `#1B1F4B` | Navy: top bar, header ribbons, dark buttons, primary text on light surfaces |
| `primary` | `#E0218A` | Magenta: main action (Play, Resume, Play Again, Hammer), score on the game-over card |
| `secondary` | `#0E9F8A` | Emerald/teal: secondary action (Restart, Main Menu, Shuffle, Collection), daily header |
| `accent` | `#FFB627` | Saffron: rims, captions on navy, count badges, selected state, NEXT tag |
| `danger` | `#FF5E57` | Coral: danger line, error and save-recovery notices |
| `textDark` | `#2A1A33` | Ink: body text on cream, outlines on items and titles |
| `mutedText` | `#5C5473` | Secondary text and captions on cream |
| `overlay` | `#120D24` at 70% | Modal dim |

Contrast rules: white or saffron on navy/magenta/teal; navy or ink on cream/saffron. Never use saffron text on cream.

## Typography

| Use | Font | Size (1080-wide reference) |
|---|---|---|
| Title | Lilita One | 160, ink outline + drop shadow |
| Card heading (ribbon) | Lilita One | 84, saffron, ink outline |
| Big numbers (score) | Lilita One | 96 (HUD), 170 (game over) |
| Buttons | Lilita One | 54–112 by importance |
| Captions / labels | Lilita One | 28–40 |
| Body / descriptions / hints | Built-in LegacyRuntime (bold) | 22–40 |

Lilita One is display-only; long or small text uses the body font. `UiFactory.Text(..., body: true)` selects it.

## Components (built by `UiFactory`)

| Component | Sprite | Notes |
|---|---|---|
| Panel | `Panel.png` (9-slice, radius ≈ 42) | Subtle top-to-bottom shading. `Panel(fill, rim)` adds a coloured rim (6–10 px). |
| Button | `ButtonBase.png` (9-slice with a 10 px darker "lip") | Label lifted by `buttonLip`; optional icon left of the label, or centred when there is no label. |
| Icon button / tile | Button at a square size | Pause (124), menu tiles (190) with a caption below, power-ups (156×150) with icon, label and count badge. |
| Pill | `Pill.png` | Hints, toast, tags, best-score badge, progress tracks. |
| Badge | `Disc.png` | Count badges (navy ring + saffron fill), completed mark. |
| Modal card | `Modal(title)` | Dim, cream card with saffron rim and shadow, navy header ribbon with title, border strip and two ornaments. |
| Border strip | `BorderStrip.png` (tiled) | Truck-art "chamak patti" trim under bars and ribbons, and at the top and bottom of the menu. |
| Ornament | `Ornament.png` | Flower motif: ribbon ends, title plate, wall caps; large and faint in backgrounds (10–20% alpha). |

Phase 6.6 components (runtime class → builder):

| Component | Runtime | Notes |
|---|---|---|
| Menu panel | `MenuPanel` (base) | Modal card, round coral close (X) button sticking out of the top-right corner, Android back closes it |
| Reward card (calendar day) | `DailyRewardTile` | Claimed: mint fill, faded icon, teal check badge. Today: saffron glow, cream-yellow fill. Upcoming: white. Day 7 is double width with ornaments. |
| Mission / rank card | `ProgressCard` | Title, one-line description, animated progress bar (saffron on a navy-12% pill), "n / target", reward icon and amount, Claim (magenta) or check badge |
| Progress bar | Pill track and filled Image | Animates with `MoveTowards`, unscaled time, stops itself when it reaches the value |
| Badge / notification dot | Coral disc with "!" | Punch on appear; event-driven |
| Tabs | Three buttons | Selected: navy with saffron label; normal: sand (`#F1E6D2`) with muted label |
| Reward popup | `RewardPresenter` | Magenta ribbon, rotating faint truck-art flower, 200 px reward discs, Collect; queue of 8 at most |
| Toast | `ToastPresenter` | Navy pill, saffron text, optional item-art icon (discoveries) |
| Confirmation dialog | Pause → Restart confirm (6.5) | |
| Loading state | Splash spinner (ornament), only after 1.2 s | |
| Empty state | Muted body text inside the list ("Claimed missions appear here.") | |
| Locked item card | Collection slot (6.5): dark silhouette + lock, plus a hint label | |
| Tutorial card | `TutorialOverlay` | Teal ribbon "HOW TO PLAY", one illustration per page, page dots, NEXT / LET'S PLAY, SKIP below |

Sizes: icon 44–60 inside rows, 110 on tiles, 200 discs in the reward popup; touch targets ≥ 100 px (row Claim 176×100, close 112×112, tabs 286×100). Card radius ≈ 42 (Panel sprite). Row height 150 with a 14 px gap. Modal cards join `ResponsiveScaler`, so they shrink on short (3:4) screens.

Iconography: 23 icons (`Art/UI/Icons`), white, filled, rounded geometry, all drawn on the same 128 px grid. 6.6 added Gift, Calendar, Missions, Medal, Back and Unlock. Line weight comes from about 0.1–0.19 unit strokes, so they read alike at small sizes. They are tinted per use (white on coloured buttons, navy on saffron).

## States

| State | Visual |
|---|---|
| Normal | Full colour |
| Pressed | Colour × 0.84 and scale `pressedScale` (0.92), unscaled time (works while paused) |
| Selected (Hammer waiting for a target) | Saffron glow and ring behind the button; magenta targeting banner with Cancel |
| Disabled / unavailable | Button tint (0.62, 0.62, 0.68); power-up content dimmed to 50% |
| Hidden | Features that are not offered (e.g. Continue without an ad provider) are not shown at all |

## Spacing and layout

Reference 1080×1920, portrait. Side margin 24 for bars and 40 elsewhere; minimum touch target 120 (the smallest real targets are Cancel 176×84 and Claim 260×96). The top HUD (bar, hint, power-ups, banner, toast) is one `TopHud` container: the safe area keeps it clear of notches, and `ResponsiveScaler` shrinks it uniformly on short screens (3:4).

## Motion

| Animation | Timing |
|---|---|
| Panel in/out | 0.22 s, OutCubic fade, card OutBack from 0.8 |
| Button press | Spring to 0.92 at 18/s |
| Score punch | 0.25 s, +12% |
| Next item punch | 0.2 s, +25% |
| Merge pop | 0.26 s, +22% with 12% squash-and-stretch, **visual child only** |
| Merge ring | 0.32 s, grows 0.45→1 × (3.2 × radius), fades from 30% of its life |
| Sparkles | 14 + 4 particles, 0.35–0.65 s |
| Menu entrance | Staggered 0–0.46 s slide and fade |
| Game-over title | 0.45 s punch, unscaled |
| Splash | 0.9 s: logo scale 0.7→1 (OutBack), fade (OutCubic) |
| Reward popup | Panel in (0.22 s), reward discs punch +18% over 0.4 s, background flower 24°/s |
| Calendar tile state change | +12% punch over 0.3 s |
| Progress bars | Fill moves at 2.5 / s |
| Notification dot | +20% punch over 0.4 s when it appears |

Rules: animations never block input, never touch the physics root, and animation code paths make no per-frame allocations. Gameplay effects use scaled time (they freeze while paused); UI uses unscaled time.

## Item art style

Ink outline (`#2A1A33`, 0.05 of the radius), light from the upper left, darker rim band, one glossy highlight, and a small friendly face (eyes with glints, smile, blush). Each silhouette stays inside the item's collider circle (a test checks this).
