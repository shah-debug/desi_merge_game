# Store screenshots (Google Play)

Made 2026-11-03 with [goldie](https://github.com/software-mansion-labs/goldie) 0.5.0, an open-source tool that frames app
captures with a device bezel, a background and a headline, then checks them against store size rules.

| Folder | Contents |
|---|---|
| `google-play/` | 5 final screenshots (menu, play, missions, daily, skins), 1080 × 1920 PNG, opaque. Passed `goldie verify`. |
| `goldie/goldie.config.ts` | Theme (game colours, Lilita One font), headlines and scene order. |

How they were made:

* goldie's own capture step drives an **emulator** through argent flows. This machine has no Android emulator, so
  captures were taken from the real phone with `adb exec-out screencap -p`, and a hand-written
  `out/raw/pixel-10-pro/manifest.json` pointed goldie at them. Only goldie's `frame` and `verify` steps were used.
* The Pixel 7 Pro screen (1440 × 3120) is slightly taller than goldie's Pixel 10 Pro bezel (1280 × 2856), so the
  capture is cover-cropped by a few percent top and bottom.
* goldie ran from a local install in a temporary folder with a portable ffmpeg; nothing was added to the game
  project or installed system-wide.

To regenerate: install `goldie` (Node ≥ 20) and ffmpeg, put new captures + manifest under `out/raw/pixel-10-pro/`
next to the config, then run `goldie frame` and `goldie verify`.

Not done: App Store (iPhone/iPad) sets need macOS + iOS simulator; no preview video was made.
