# Panic At The Pond — Asset Specification

**Version 2.0 · 2026-08-08 · Unity 6000.5.7f1 · Build resolution 1920 × 1080 (16:9)**

Every "current" figure was measured in-editor across **all 643 image files** under `Assets/_Project`.
Nothing here is estimated. This is the single source of truth for asset sizes.

---

## 0. Corrections in v2.0

A full sweep of the project turned up three errors in v1.0.

| # | v1.0 said | Actually | Why it matters |
|---|---|---|---|
| 1 | **24** animations | **26** | 24 "Default" folders + `LeftToRightPole` and `RightPoleToOar`, which live in the BoatFacingLeft set |
| 2 | **4** frames per animation, always | **4, but `Dead` is 6 and worm `Dance` is 5** | a flat "4 frames" brief would have under-delivered |
| 3 | Fisherman is 6 part sheets | Also **336 loose 64 × 64 frames**, fully duplicated per hair colour | this is the real source art, and the duplication is the core problem |

---

## 1. The one rule

> ## Everything is **100 pixels per world unit**.
> ### An asset's pixel size = **its size in the game world × 100**.

That is the whole specification. No per-asset scale values, no maths per item.

**Assets already following it:** `Boot` (58 × 64) and `Tire` (62 × 64) are authored at 100 PPU and
render pixel-for-pixel today. `BG2_Frames` are already 1920 × 1080. We are standardising on what
parts of the project already do.

### Where the number comes from

```
Camera:  orthographic, size 5.4   →  visible world height = 10.8 units
Screen:  1920 × 1080              →  1 world unit = 1080 / 10.8 = 100 px
```

One source pixel = one screen pixel. No upscaling, no blur, no uneven pixel widths.

---

## 2. Every in-game object

Target = measured world size × 100.

| Object | Current | PPU | World size | **Target** | Scaling now | Action |
|---|---|---|---|---|---|---|
| **Fisherman** (+boat) | 64 × 64 | 25 | 3.20 × 3.20 | **320 × 320** | **5.40× up** | 🔴 remake |
| **Worm** | 15 × 21 | 35 | 0.43 × 0.60 | **43 × 60** | 2.86× up | 🔴 remake |
| **Hook worm** | 15 × 21 | 35 | 0.43 × 0.60 | **43 × 60** | 2.86× up | 🔴 remake |
| **Bass** | 55 × 35 | 50 | 1.10 × 0.70 | **110 × 70** | 2.00× up | 🟠 remake |
| **Golden Fish** | 55 × 35 | 50 | 1.10 × 0.70 | **110 × 70** | 2.00× up | 🟠 remake |
| **Trout** | 48 × 30 | 100 | 0.82 × 0.51 | **82 × 51** | 1.70× up | 🟠 remake |

> **Fish targets above are content sizes.** All three are drawn inside the shared **200 × 140** fish
> canvas (§4), not on canvases of their own. Golden Fish is measured as bass-sized today — same
> 55 × 35 art at 50 PPU with no scale-down — so if it is meant to read smaller, that is a design
> change still to be confirmed, not a measurement error.
| Hook | 274 × 423 | 500 | 0.55 × 0.85 | 55 × 85 | 4.98× **down** | 🟢 keep |
| Water drop / bubble | 164 × 164 | 200 | 0.82 × 0.82 | 82 × 82 | 2.00× **down** | 🟢 keep |
| Junk — Boot | 58 × 64 | 100 | 0.58 × 0.64 | 58 × 64 | **1.00× exact** | ✅ correct |
| Junk — Tire | 62 × 64 | 100 | 0.62 × 0.64 | 62 × 64 | **1.00× exact** | ✅ correct |
| Bucket of worms | 64 × 64 | — | *world size TBC* | size × 100 | — | 🟡 verify |
| Environment tiles | 32 × 32, 160 × 96, 32 × 256 | — | *world size TBC* | size × 100 | — | 🟡 verify |

Six different PPU values are in use — **25, 35, 50, 100, 200, 500**. That spread is the root cause of
every size inconsistency. All new art is 100.

> **Upscaled assets look soft and shimmer. Downscaled ones are only wasteful.** Only the 🔴/🟠 rows
> need new art — six objects, not twelve.

---

## 3. Animations — counts and frames

**26 animations** for the fisherman, matching `ANIM_FisherManRedHair`:

```
CastingLeft       FishGotOffFacingLeft   MoveBackwards        RightPoleToOar
CastingRight      FishGotOffFacingRight  MoveForward          RightToLeftPole
CryLeft           FishingLeft            MoveReverseBackwards WinningLeft
CryRight          FishingRight           MoveReverseForward   WinningRight
FightingLeft      IdleLeft               OarToLeftPole
FightingRight     IdleRight              OarToRightPole
                  LeftPoleToOar          ReelingLeft
                  LeftToRightPole        ReelingRight
```

**Frame counts are not uniform.** Please keep the existing counts:

| Character | Animations | Frames | Strip size (at target res) |
|---|---|---|---|
| Fisherman | 26 | **4** each | **1280 × 320** |
| Bass | Idle, Move, Eat, Fight, Joyful | **4** each | **800 × 140** |
| Bass | **Dead** | **6** | **1200 × 140** |
| Golden Fish | Idle, Move, Eat | **4** each | **800 × 140** |
| Golden Fish | **Dead** | **6** | **1200 × 140** |
| Trout | Idle, Move, Eat, Fight, Joyful | **4** each | **800 × 140** |
| Trout | **Dead** | **6** | **1200 × 140** |
| Worm | Idle | **4** | **172 × 60** |
| Worm | **Dance** | **5** | **215 × 60** |

**One PNG per animation**, frames laid out horizontally. Not one combined sheet — a 26 × 4 fisherman
grid at 320 px would be 1280 × 8320, past the safe texture limit on some hardware.

---

## 4. Fisherman parts — and why this matters most

### The problem, in numbers

The fisherman is currently **336 loose 64 × 64 frames**, because the entire animation set is redrawn
for every hair colour:

| Variant | Animations | Frames |
|---|---|---|
| `Used animation ui` (default) | 26 | 112 |
| `Default animation ui (Black Hair)` | 26 | 112 |
| `Default animation ui (Red Hair)` | 26 | 112 |
| | | **336 total** |

> **Adding one new hair colour today means drawing 112 new frames.**
> With layers it is **26 files** — the hair only.

That is the entire argument for the layered approach, and it gets worse with every cosmetic added.

### The rule

> **Every part is authored on the FULL 320 × 320 character canvas, same frame order, never cropped.**

| # | Part | Exists today | Swappable | Draw order |
|---|---|---|---|---|
| 1 | **Boat** | ✅ `FishermansAnimations-Boat_Sheet` | ✅ | 0 (back) |
| 2 | **Oars** | ✅ `FishermansAnimations-Oars_Sheet` | ✅ | 1 |
| 3 | **Body / torso** | ✅ `FishermansAnimations-GreenBody_Sheet` | base | 2 |
| 4 | **Clothes** | ❌ new | ✅ | 3 |
| 5 | **Arms** | ✅ `FishermansAnimations-Arms_Sheet` | base | 4 |
| 6 | **Head / face** | ✅ `FishermansAnimations-Head_Sheet` | base | 5 |
| 7 | **Hair** | ⚠ only static 64 × 64 `Red_Hair` / `Black_Hair` | ✅ | 6 |
| 8 | **Hat** | ❌ only cropped icons | ✅ | 7 |
| 9 | **Rods** | ✅ `FishermansAnimations-Rods_Sheet` | ✅ | 8 (front) |

Each file **1280 × 320**, transparent except for its own part. The game stacks them at 0,0 and they
align automatically on every frame.

**Hair is the key gap** — it exists only as two static images, which is why it cannot animate with
the head and why the whole frame set had to be duplicated per colour.

**Never bake a hat or hair into a body animation.**

### Volume

- Base fisherman: **9 parts × 26 animations = 234 files**
- Each swappable variant (a hat, a hair colour, a boat, a clothing set): **26 files**

Unity packs these into a sprite atlas at build time, so a high file count costs nothing at runtime.

### Fish parts

| Part | Canvas |
|---|---|
| Fish body | **200 × 140** — shared canvas, every species |
| Fish hat | **200 × 140** — same canvas, never cropped |

> **200 × 140 is the frame, not the fish.** Every species and every hat is drawn inside this one
> canvas, at whatever size that species should actually be, with transparency around it. A bigger
> canvas does not make a fish bigger.

| Zone | Area | Purpose |
|---|---|---|
| Fish body | bottom **200 × 100** | up to 2.00 × 1.00 world units — roughly double today's bass |
| Hat headroom | top **40 px** | hats sit above the fish; without reserved space tall hats clip |

**Content size per species — drawn inside the shared canvas, not stretched to fill it:**

| Species | Content size | Notes |
|---|---|---|
| Bass | 110 × 70 | unchanged from today |
| Trout | 82 × 51 | unchanged from today |
| Golden Fish | **TBD** | it is bass-sized today (110 × 70); confirm intended size |

**Position matters as much as size.** Every fish sits on the **same baseline and the same horizontal
centre** inside the canvas. Consistent size alone is not enough — if one species floats high in its
frame and another sits low, a single hat file cannot line up on both.

*(This frame was 110 × 70 in the previous revision. It is going back up not because that number was
wrong, but because larger species and taller hats are now planned and both need the room.)*

---

## 5. UI, shop and backgrounds

UI is authored against the **1920 × 1080** canvas at 100 reference PPU, so a UI sprite's pixel size
is its intended on-screen size at 1080p.

### Already correct — do not change ✅

| Asset | Size | Count |
|---|---|---|
| `BG2_Frames` animated background | **1920 × 1080** | 8 |
| Achievement icons | **256 × 256** | 7 of 8 |
| Region icons | **500 × 500** | 3 |
| Flag icons | **500 × 500** | 3 |
| Cosmetic preview composites | **500 × 500** | 32 |
| Cosmetic cell box | **256 × 256** | 2 |

### Needs fixing

| Asset | Current | **Target** |
|---|---|---|
| Shop background (6 frames) | 768 × 384 (2:1) | **1920 × 1080, no signs painted in** |
| Shop hat icon — fisherman | 64 × 64, 20–38 % fill | **128 × 128, centred, ~90 % fill** |
| Shop hat icon — fish | **18 × 13, 21 × 16, 24 × 15, 20 × 14** | **128 × 128, centred, ~90 % fill** |
| Hair icon | 64 × 64 | **128 × 128** |
| Coin | 64 × 64 | **128 × 128** |
| Lock / padlock | 185 × 280 | **128 × 128** |
| Picture frames | 365 × 350 **and** 360 × 345 | **256 × 256** (both) |
| Achievement icon (odd one out) | 64 × 64 | **256 × 256** |
| Signs | 38 × 23, 42 × 23, 69 × 23, 132 × 19 | **all 32 px tall**, one sign per file |

Fish hat icons are up to **12× smaller** than fisherman hat icons. That is why the game has to
alpha-trim and rescale every icon at runtime just to make one shelf row look even.

### Signage — two requirements

1. **One sign per file.** The delivered PNG had "fishing supplies" and "sal-T shop" stacked in one
   132 × 42 image.
2. **The shop background must have no signs painted into it.** They are currently part of the
   background image, so they cannot be moved or resized and are stuck at whatever the background is
   scaled to. As separate PNGs on a clean background they can be placed exactly to the mockup.

---

## 6. Delivery conventions

| | |
|---|---|
| Format | PNG, straight (non-premultiplied) alpha |
| Scaling | Nearest-neighbour only — never upscale with smoothing |
| Effects | No baked drop shadows, glows or outlines |
| Naming | `Character_Part_Animation.png` → `Fisherman_Hat_IdleLeft.png` |
| Icons | `<cosmetic-id>.png`, lowercase, matching `shop_config.json` ids |

**Unity import settings (our side):** Point filter · Compression None · Mip Maps off · PPU 100 ·
Read/Write on for anything the shop measures.

---

## 7. Adding a cosmetic — before and after

**Today:** author a cropped icon → hand-add position, rotation and scale entries to the placement
table in `CosmeticRuntimeApplier`, with bespoke branches per animation state (the ranger hat, turtle
hat and blue cap already have special cases) → test all 26 animations.

**Under this spec:**

1. Drop 26 × `Fisherman_Hat_<Name>_<Anim>.png` (1280 × 320) into the hats folder.
2. Drop a 128 × 128 shop icon.
3. Drop a 500 × 500 preview composite.
4. Add one row to the cosmetic catalog.

**No code. No offsets. No per-hat tuning.** The placement table is deleted, not extended.

---

## 8. Engine changes — status

| # | Change | Status |
|---|---|---|
| 1 | Camera `orthographicSize` 5 → **5.4** | ✅ **DONE** — Play + Dash, saved. 100 px per world unit. |
| 2 | Unify sprite import PPU to **100** | ⏳ **Gated on art** — changes world sizes, must land with the new assets. |
| 3 | Canvas reference **1920 × 1080** everywhere | ✅ **DONE** — `Canvas_Play` + `Canvas_PlayBackground` from 800 × 600, all 15 root children compensated ×2.07846. |
| 4 | Replace the per-hat placement table | ⏳ **Gated on art** — the table is deleted, which only makes sense with layers to test against. |

**Bonus from #1:** at `orthographicSize` 5.4 the *current* 64 × 64 fisherman renders at exactly
**5.00×** instead of 5.40×. Integer scaling with Point filtering means the movement shimmer is
already gone, before any new art arrives.

---

## 9. Where to tune the Sal-T shop UI

The overlay is now **real scene objects**:

```
--- UI --- / Canvas_Dash / SaltShop Overlay
├── Picture Frame 1 / 2
├── Back Sign · Close Sign
├── Coin Icon · Coin Amount
├── SaltShop Items          ← dynamic, items generate here
└── Buy Popup (Title / Coin / Price / Status / Yes / No)
```

Select any child and move or resize it normally. **Keep the names** — click handlers are re-bound by
name each time the shop opens. To regenerate, use **Bake Overlay Into Scene** on the `SaltShopUI`
component (`Canvas_Dash/ShopItemsPanel/Sal -t Image BackGround`).

**Not editable:** the "fishing supplies" / "sal-T shop" titles are painted into the background art.

---

## 10. Quick reference card

```
RULE:              pixels = world size × 100        (100 PPU everywhere)

IN-GAME
  Fisherman frame    320 × 320      strip 1280 × 320
  ALL FISH canvas    200 × 140      strip  800 × 140  (Dead: 1200 × 140)
    fish zone          bottom 200 × 100      hat headroom  top 40 px
    content: bass 110 × 70 · trout 82 × 51 · golden fish TBD
    same baseline + centre in every frame, never cropped
  Worm                43 × 60       strip  172 × 60   (Dance: 215 × 60)
  Junk boot / tire    58 × 64 / 62 × 64               ✅ already correct
  Hook                55 × 85
  Water drop          82 × 82

ANIMATIONS         26 fisherman · 4 frames each (Dead 6, worm Dance 5)

FISHERMAN PARTS    boat · oars · body · clothes · arms · head · hair · hat · rods
                   9 layers on the FULL 320 × 320 canvas, never cropped
                   234 files base · 26 per swappable variant

UI
  Shop icon        128 × 128        centred, ~90 % fill
  Coin / lock      128 × 128
  Picture frame    256 × 256
  Achievement      256 × 256        ✅ mostly correct
  Preview          500 × 500        ✅ correct
  Region / flag    500 × 500        ✅ correct
  Signs            32 px tall       one sign per file
  Backgrounds     1920 × 1080       ✅ BG2_Frames already correct
                                    ❌ shop bg is 768 × 384, needs signs removed
```
