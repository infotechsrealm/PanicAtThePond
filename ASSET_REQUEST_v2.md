# Art Spec v2 — Panic At The Pond

> Client message + the measured numbers behind it.
> Every figure verified in-editor on 2026-08-08 (Unity 6000.5.7f1). Nothing here is estimated.

---

## PART 0 — Corrections to the previous draft

Two numbers in the first version of this spec were wrong. Both are fixed below.

| Claim | Was stated | Actually measured | Effect |
|---|---|---|---|
| Fisherman PPU | 100 | **25** | the earlier size maths was 4× off |
| Fisherman target frame | 256 × 256 | **320 × 320** | 256 would still be an upscale on screen |
| Fish target frame | 220 × 140 | **110 × 70** | 220 was 2× larger than needed |

The 100 PPU figure came from reading the *head sheet* importer; the fisherman prefab that actually
ships uses a different sprite at 25 PPU.

---

## PART 1 — The measurement that drives everything

```
Camera:            orthographic, size 5   ->  visible world height = 10 units
Screen:            1920 x 1080            ->  1 world unit = 108 px on screen
```

| Character | Source sprite | PPU | Prefab scale | World height | On screen @1080p | Scaling |
|---|---|---|---|---|---|---|
| Fisherman | 64 × 64 | 25 | 1.25 | 3.20 units | **346 px** | **5.40× upscale** |
| Fish | 55 × 35 | 50 | 1.0 | 0.70 units | **76 px** | **2.16× upscale** |

**That 5.40× is the real problem.** A 64 px sprite blown up 5.4× is why the fisherman looks soft and
blocky, and because 5.40 is not a whole number some source pixels land 5 screen-pixels wide and
others 6 — which is the shimmer you see when he moves.

### The fix: one PPU for everything, and exact 1:1 pixels

Set **every character sprite to 100 PPU** and the camera to **orthographicSize 5.4**. Then:

```
1 world unit = 100 px on screen, exactly
```

…and any asset's correct pixel size becomes just **world size × 100**. No per-asset maths ever again.

| Character | World size (unchanged) | Required source | Result |
|---|---|---|---|
| Fisherman | 3.2 × 3.2 units | **320 × 320 px** | exactly 1:1, no scaling |
| Fish | 1.1 × 0.7 units | **110 × 70 px** | exactly 1:1, no scaling |

Prefab scales also become a clean **1.0**.

> **One trade-off to accept:** `orthographicSize` 5 → 5.4 shows about 8 % more of the world
> vertically. That is the price of pixel-exact rendering. If you would rather not change the framing,
> keep size 5 and the art still improves hugely — it is just a 1.08× upscale instead of a perfect 1.0.

---

## PART 2 — Message to send to the client

Hi — I've measured everything properly on my side so you can set canvases up once and never have to
guess. Short version: **everything at 100 pixels-per-unit**, and the sizes below.

### The one rule

The game will run at **100 pixels per world unit**. So an asset's pixel size is simply how big it
should be in the world × 100. Once you have that, every size below follows automatically, and any
future asset is trivial to size correctly.

### Characters (in-game)

| Asset | Current | Please deliver |
|---|---|---|
| Fisherman, per frame | 64 × 64 | **320 × 320** |
| Fish, per frame | 55 × 35 | **110 × 70** |

The fisherman is currently being blown up **5.4×** on screen, which is why he looks soft. At 320 × 320
he renders at exactly 1:1 — every pixel you draw is one pixel on screen.

**Please deliver one PNG per animation, not one giant sheet.** 4 frames laid out horizontally, so
each file is **1280 × 320** for the fisherman and **440 × 70** for the fish. Named like
`Fisherman_Body_IdleLeft.png`. A single 4 × 24 sheet at this resolution would be 1280 × 7680, which
is past the safe texture limit on some hardware, and per-animation files mean adding an animation
later is just one new file.

### Modular fisherman — the important one

You asked the best way to make hair, clothes, oar, boat and rods swappable. One rule matters most:

> **Author every part on the full 320 × 320 character canvas, in the same frame order — and never
> crop or trim it.**

```
Fisherman_Body_IdleLeft.png      1280 x 320
Fisherman_Hair_IdleLeft.png      1280 x 320
Fisherman_Hat_IdleLeft.png       1280 x 320
Fisherman_Clothes_IdleLeft.png   1280 x 320
Fisherman_Oar_IdleLeft.png       1280 x 320
Fisherman_Boat_IdleLeft.png      1280 x 320
Fisherman_Rods_IdleLeft.png      1280 x 320
```

Each one transparent except for its own part. The game stacks them at 0,0 and they line up
automatically, on every frame, forever.

This solves a real problem. Hats are currently delivered as small cropped icons, so the game has to
*guess* where on the head each one sits — and the result is exactly what you'd expect: some sit too
high, some too small, some too tight. Right now every new hat needs someone to hand-tune its
position across all 24 animations. With full-canvas layers that entire class of bug disappears,
because the hat is already in the right place inside its own file.

It also means adding hat #14, a new hair colour, or a different boat is **drop in a PNG** — no code,
no repositioning, no testing pass.

**Please don't bake hats into the animations.** Separate layers is exactly what makes it expandable,
which is what you were asking about.

The fish works the same way: fish hats on the same **110 × 70** canvas as the fish body.

### Animation list — 24 animations, 4 frames each

```
CastingLeft      FishingLeft        MoveReverseForward
CastingRight     FishingRight       OarToLeftPole
CryLeft          IdleLeft           OarToRightPole
CryRight         IdleRight          ReelingLeft
FightingLeft     LeftPoleToOar      ReelingRight
FightingRight    MoveBackwards      RightToLeftPole
FishGotOffLeft   MoveForward        WinningLeft
FishGotOffRight  MoveReverseBack    WinningRight
```

### Shop / UI icons — please make these consistent

This is the messiest part today:

- fisherman hat icons are **64 × 64** with art filling only 20–38 % of the frame
- fish hat icons are **18 × 13** and **24 × 15** — roughly 12× smaller than the fisherman ones
- the lock is 185 × 280, the picture frames are 365 × 350 and 360 × 345, the coin is 64 × 64

Because of that spread the game has to measure and rescale every icon at runtime just to make one
shelf look even. Please deliver **every shop icon on a 128 × 128 canvas, art centred, filling about
90 %** — fish hats and fisherman hats alike, one canvas size for all.

### Backgrounds and signage

- Full-screen art at **1920 × 1080 (16:9)**. The current shop background is 768 × 384 (2:1), which is
  why it has never filled the screen.
- **The shop background needs a version with no signs painted into it.** "fishing supplies" and
  "sal-T shop" are currently part of the background image, so we can't position or resize them and
  they're stuck at whatever the background is scaled to. As separate PNGs on a clean background we
  can place them exactly like your reference.
- **One sign per file.** The file you sent had "fishing supplies" and "sal-T shop" stacked in one
  PNG; I've split them, but separate files from here please.
- All signs should share **one pixel height** (e.g. all 32 px tall) so the lettering is the same
  physical size across back / close / sal-T shop / fishing supplies. They're currently 23, 23, 23 and
  19.

### File format

PNG, straight (non-premultiplied) alpha, no baked drop shadows, no padding beyond the canvas rules
above. Nearest-neighbour only — please don't upscale with smoothing.

### Existing assets

Now that the sizes are pinned down, it's worth **remaking the current hats and the fisherman at these
resolutions** rather than upscaling them — upscaled pixel art will look noticeably worse next to the
new work. Keeping the low-resolution art for the main menu is completely fine; we'll hold that in a
separate folder so the two sets never get mixed.

Let me know if **320 × 320 per fisherman frame** and **128 × 128 for shop icons** work, and I'll
confirm before you start the batch so nothing has to be redone.

---

## PART 3 — Full size table

| Asset | Current | Requested | Notes |
|---|---|---|---|
| **Fisherman frame** | 64 × 64 @ 25 PPU | **320 × 320 @ 100 PPU** | 1:1 on screen |
| Fisherman animation strip | — | **1280 × 320** | 4 frames horizontal |
| Fisherman parts | body/head only | body, hair, hat, clothes, oar, boat, rods | each on the full canvas |
| **Fish frame** | 55 × 35 @ 50 PPU | **110 × 70 @ 100 PPU** | 1:1 on screen |
| Fish animation strip | — | **440 × 70** | 4 frames horizontal |
| Fish hat | cropped icon | **110 × 70** | same canvas as the fish |
| Shop background (6 frames) | 768 × 384 (2:1) | **1920 × 1080**, no signs baked in | |
| Shop hat icon (fisherman) | 64 × 64, 20–38 % fill | **128 × 128**, ~90 % fill | |
| Shop hat icon (fish) | 18 × 13 / 24 × 15 | **128 × 128**, ~90 % fill | |
| Lock / padlock | 185 × 280 | **128 × 128** | |
| Coin | 64 × 64 | **128 × 128** | |
| Picture frames | 365 × 350, 360 × 345 | **256 × 256** | |
| Signs | 38 × 23 … 132 × 19 | consistent **32 px** tall, one per file | |

### Verified project constants

| | | Source |
|---|---|---|
| Build resolution | 1920 × 1080 | `PlayerSettings.defaultScreen` |
| Camera | orthographic, size 5 → **5.4 proposed** | Play + Dash `Main Camera` |
| World-to-screen | 108 px/unit → **100 px/unit proposed** | derived |
| Dash canvas | ScaleWithScreenSize, **1920 × 1080**, match 0.5, refPPU 100 | `Canvas_Dash` |
| Play canvases | ScaleWithScreenSize, **800 × 600**, match 0.5 | `Canvas_Play`, `Canvas_PlayBackground` |
| Frames per animation | 4 | animator clips |
| Animations | 24 | `ANIM_FisherManRedHair` |

> ⚠ **Internal, not for the client:** the Play scene canvases use a **800 × 600** reference
> resolution while Dash uses **1920 × 1080**. That inconsistency means in-game UI scales differently
> from menu UI. It should be unified to 1920 × 1080, but doing so rescales every element in the Play
> scene and needs its own verification pass — it is not part of the art request.

---

## PART 4 — Why this is future-proof (internal)

Cost of adding a hat **today**: author a cropped icon, then hand-add an entry to the per-hat
placement table in `CosmeticRuntimeApplier` — position, rotation and scale, with bespoke branches per
animation state. There are already special cases for the ranger hat, turtle hat and blue cap. That is
why hats fit inconsistently, and it does not scale.

Cost **under this spec**:

1. Drop `Fisherman_Hat_<Name>_<Animation>.png` (24 files, full canvas) into the hats folder.
2. Drop a 128 × 128 shop icon into the icons folder.
3. Add one row to the cosmetic catalog (id, display name, category, price, in-rotation).

No code, no offsets, no per-hat tuning. The placement table gets **deleted**, not extended, because
every layer is aligned by construction.

**Sequencing:** lock the art contract first (this document). The code simplification — replacing the
offset tables with a zero-offset layer renderer driven by a catalog ScriptableObject — should land
against the first batch of conforming art so it can be compiled and tested on real assets rather than
written blind.

**Still outstanding from the original ASSET_REQUEST.md:** the 1920 × 1080 shop background (item 1) has
not been delivered — `shelf.png` is 256 × 128, still 2:1.
