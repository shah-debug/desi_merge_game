# Desi Merge: Visual Asset Manifest (Phase 6.5)

"Original" here means drawn by this project's own Editor code (`ArtCanvas` vector rasterizer), with no downloaded, traced or copied artwork. Regenerate it with **Desi Merge > Setup > Apply Visual Redesign**; the output is deterministic, and existing GUIDs are kept.

## 1. Completed original assets

### Merge items: `Assets/_Project/Art/Sprites/Items/` (`ItemArtGenerator`)

| File | Item (stable ID) | Description |
|---|---|---|
| `01_patti.png` | patti | Heart-shaped paan leaf with veins and stem |
| `02_nimbu.png` | nimbu | Lime with pores and a small leaf |
| `03_pyaz.png` | pyaz | Red onion with meridian stripes and a papery tip |
| `04_tamatar.png` | tamatar | Tomato with a five-point calyx and stem |
| `05_roti.png` | roti | Roti with an irregular edge and char spots |
| `06_aam.png` | aam | Mango (kidney shape) with blush, stem and leaf |
| `07_chai.png` | chai | Cup on a saucer, truck-art band, chai surface, steam |
| `08_mango_shake.png` | mango_shake | Glass of shake, cream top, striped straw, mango slice |
| `09_rickshaw.png` | rickshaw | Front view: canopy with tassels, windshield, headlight, flower decals |
| `10_bus.png` | bus | Front view of a truck-art bus: scalloped crown, flower, split windshield, headlights, bumper |

All items share the same specification:

| Property | Value |
|---|---|
| Size | 512×512 px, transparent background |
| Pixels per unit | 512 (sprite = 1 world unit wide, matching the prefab's 1-unit collider diameter; the item transform scales it to 2 × radius) |
| Pivot | Centre |
| Import | Sprite (Single), Tight mesh, mipmaps on, bilinear, max size 512, compression = platform default (ASTC on Android), alpha is transparency |
| Sorting | Item `Visual` child, order 8: above all board art (field −10, danger line 4, walls 5, floor 6, caps 7); preview 8, ring 9, sparkles 10 |
| Collider relationship | The silhouette, including its outline, stays inside the collider circle (verified by `VisualContentTests`). The collider is the root `CircleCollider2D` (radius 0.5, unchanged); the sprite lives on the child, so visual scaling never changes collision geometry. |
| Tint | Drawn untinted (`tintSprite` off). The definition's tint remains the accent colour for sparkles, rings and popups. |
| PNG size | about 60–145 KB each (about 1 MB source total). Art is drawn at 97% of the circle for an anti-aliasing margin. |

### UI and playfield: `Assets/_Project/Art/UI/Game/` and `Art/UI/Icons/` (`GameArtGenerator`)

| File | Size | PPU / border | Use |
|---|---|---|---|
| `Panel.png` | 128² | 100 / 44 (9-slice) | Cards, bars, field panel (world, sliced) |
| `ButtonBase.png` | 128² | 100 / 46-56-46-46 | Every button (lip at the bottom) |
| `Pill.png` | 128² | 100 / 62 | Hints, toast, tags, badges, progress tracks |
| `Disc.png` | 128² | 100 | Badges, the next-item well, slider knob |
| `Glow.png` | 128² | 100 | Next-item glow, selected power-up glow |
| `Ring.png` | 128² | 100 | Merge ring particle (via `MergeRing.mat`) |
| `Ornament.png` | 256² | 100 | Truck-art flower: ribbons, title plate, wall caps, faint backgrounds |
| `BorderStrip.png` | 128×32, repeat | 100 | Tiled trim |
| `Background.png` | 32×256 | 100 | Stretched gradient (menu UI and world backdrop) |
| `WallTile.png` | 64×128, repeat | 128 | Tiled walls and floor (0.5 × 1 world-unit tile) |
| `DangerDash.png` | 64×16, repeat | 128 | Tiled dashed danger line |
| `Icons/*.png` (17) | 128² each | 100 | Pause, Play, Hammer, Shuffle, Gear, Home, Restart, Trophy, Grid, Close, Crown, Star, Music, Speaker, Lock, Check, Video |
| `Art/AppIcon.png` | 512² | — (Default texture) | Player Settings default icon (mango on a magenta truck-art sunburst) |

UI sprites are uncompressed RGBA, without mipmaps, and use the FullRect mesh required for slicing and tiling. They total about 80 KB of PNG (about 1.6 MB uncompressed in memory, mostly the 256² ornament). None of them has a collider. Board art is on `Board/Art` and always sorts below the items (a test checks this).

## 2. Existing reused assets

| Asset | Status |
|---|---|
| `Art/UI/Sparkle.png` + `Materials/Sparkle.mat` | Reused for merge sparkles |
| `Art/Placeholder/Circle.png` | Kept only as a **fallback**: the item prefab's default sprite before a definition is applied, the Collection placeholder for an item without art, and the HUD next-image default before the first update. No configured item uses it. |
| `Art/Placeholder/Square.png` | Fallback block sprite for boards without wall art (tests/tools) |
| `Art/UI/RoundedPanel.png` | Achievements progress fill |
| `Art/UI/PaintedFrame.png`, `TruckArtPattern.png`, `Flower.png`, `PauseIcon.png` | **Superseded**; referenced only by old theme fields, not by any scene or prefab, so they are not included in builds. Kept, not deleted. |
| Audio (`Audio/*`, synthesized in Phase 3) | Unchanged; see §4 |

## 3. Licensed third-party assets

| Asset | License | Source | Notes |
|---|---|---|---|
| `Art/Fonts/LilitaOne-Regular.ttf` | SIL Open Font License 1.1 (© 2011 Juan Montoreano, Reserved Font Name "Lilita") | `github.com/google/fonts`, `ofl/lilitaone` (downloaded 2026-10-28) | Allows commercial use and embedding in a game. The license text ships as `Art/Fonts/LilitaOne-OFL.txt` and must stay with the font. Not renamed or modified. |
| Unity built-in `LegacyRuntime.ttf` | Part of the Unity engine | Unity | Body text |

No other third-party art, icons, packages or SDKs were added.

## 4. Temporary placeholders (not production assets)

| Item | Why it is still a placeholder |
|---|---|
| Sound effects and music | Synthesized in Phase 3 as placeholders. No new audio was added because no licensed assets were available. |
| Android **adaptive** icon layers | Only the legacy/default icon is set. Android 8+ launchers show it inside the adaptive mask, which may crop or pad it; this was not checked on a device. |
| Splash screen | Unity default; not customised. |
| Body font | Unity built-in. Adequate, but a licensed rounded body face would match the display font better. |

## 5. Still to be created

- Adaptive icon foreground/background (432×432 safe zone) and a round icon. Check them on a device.
- Store listing art: feature graphic, phone screenshots, promo art.
- Professional or licensed SFX and music.
- Optional: an illustrator pass over the generated item art (same IDs, sizes and pivots, dropped in at the same paths). It will survive "Create All Content", but not "Apply Visual Redesign", which overwrites generated art.
- Optional: Urdu localisation of UI text (would need a font with Urdu glyphs; Lilita One is Latin-only).
