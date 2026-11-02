# Desi Merge: Skins, Bigger Collection and 3D Item Art (2026-11-02)

Owner's choices: collection grows through skins (merge chain unchanged); skins are bought with Diamonds; the skins are Mithai, Truck Art, Numbers and Street Food.

## Skins

| Skin | Price | Items (level 1 → 10) |
|---|---|---|
| Desi Bazaar (default) | owned | Patti … Bus (the original items) |
| Numbers | 250 💎 | 2, 4, 8 … 1024 on glossy coloured balls |
| Mithai | 400 💎 | Peda, Ladoo, Gulab Jamun, Barfi, Jalebi, Cham Cham, Rasmalai, Kheer, Mithai Box, Mithai Thaal |
| Street Food | 500 💎 | Pakora, Samosa, Gol Gappa, Chaat, Bun Kabab, Paratha Roll, Seekh Kabab, Nihari, Biryani, Food Cart |
| Truck Art | 600 💎 | Each original item in a truck-art medallion |

- **Skin tag** (saffron, under NEXT): pauses the game and opens **Skins** directly. Closing it resumes the game.
- **Skins screen:** preview, name, progress ("3 / 10 found"), and **In use** ✓ / **Use** / **💎 price** (dimmed when you can't afford it). There are no ads and no real-money purchases.
- **Buying:** spends the price once and saves at once (a double tap never charges twice), then switches to the skin.
- **Switching skin** re-skins items already on the board, the drop preview, NEXT and the discovery toasts at once. Purely visual: sizes, physics and scores are identical for every skin.
- **Data:** `ScriptableObjects/Progression/Cosmetics.asset` (prices are editable there); `cosmetics.json` gains `discovered` ("skin:level").

## Collection: 10 → 50 items

- The Collection has **◀ SKIN NAME ▶** pages, one per skin.
- A skin item is collected when you drop or make it while using that skin. The default page uses the existing saved discoveries.
- The counter shows "x / 10 discovered • y / 50 in all"; a locked skin's page says how to unlock it.

## 3D item art

`ArtCanvas.Body` now lights each shape from its own geometry: a rounded dome height and surface normal, diffuse light from the upper left, a glossy specular highlight, and edge depth. This applies to all 10 original items and all 40 skin items; item outlines and sizes are unchanged. The art is original and procedural (no copied artwork). It is more 3D than before, but still a stylised, cartoon look, not photo-realistic.

## Tests (final runs)

- EditMode **230/230**:
  - `SkinTests`: buy once and restart; not enough diamonds changes nothing; discoveries per skin are saved; all 40 skin artworks stay inside the physics circle. This test first found 8 drawings poking outside; they were fixed.
- PlayMode **129/129**:
  - `SkinFlowTests`: Skin tag → pause → buy → in use → board, NEXT and preview re-skinned → close resumes → collection page per skin.
  - `SceneFlowTests`: the collection counter check now allows the "in all" total.
- Android build: **success, 0 errors, 0 warnings**; APK 33.3 MB. **Not tested on a phone.**
