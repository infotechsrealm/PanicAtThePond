# Client message — art spec (final)

*Paste-ready. Full technical detail lives in `ASSET_SPECIFICATION.md`.*

---

Hi,

I've gone through the whole project and measured every asset — all 643 image files — so you can set
your canvases up once and never have to guess. Everything below is exact.

## The one rule

**The game runs at 100 pixels per world unit.** So an asset's pixel size is just how big it should
be in the game world × 100. Once you have that, every size below follows, and anything new is easy
to size correctly.

Some of your assets already follow this exactly — the boot (58 × 64) and tire (62 × 64) are spot on,
and the BG2 background frames are already 1920 × 1080. We're standardising on what those already do.

## 1. Characters and objects

| Asset | Current | Please deliver | Why |
|---|---|---|---|
| **Fisherman** (per frame) | 64 × 64 | **320 × 320** | being blown up 5.4× on screen |
| **Worm** | 15 × 21 | **43 × 60** | blown up 2.9× |
| **Bass** | 55 × 35 | **110 × 70** | blown up 2.0× |
| **Golden Fish** | 55 × 35 | **110 × 70** | blown up 2.0× |
| **Trout** | 48 × 30 | **82 × 51** | blown up 1.7× |

*(Fish sizes above are the drawing itself. All fish are drawn inside one shared **200 × 140** canvas
— see §3.)*
| Hook, water drop, boot, tire | — | **no change needed** | already correct or oversized |

The fisherman is the urgent one. At 64 × 64 he's being enlarged 5.4× on screen, which is why he
looks soft — and because 5.4 isn't a whole number, some of your pixels land 5 screen-pixels wide and
others 6, which is the shimmer when he moves. At 320 × 320 he renders 1:1: every pixel you draw is
exactly one pixel on screen.

## 2. Animations — 26, and the frame counts vary

There are **26 fisherman animations**, not 24 (the two pole-transition ones live in a separate
folder, which is easy to miss):

```
CastingLeft       FishGotOffFacingLeft   MoveBackwards          RightPoleToOar
CastingRight      FishGotOffFacingRight  MoveForward            RightToLeftPole
CryLeft           FishingLeft            MoveReverseBackwards   WinningLeft
CryRight          FishingRight           MoveReverseForward     WinningRight
FightingLeft      IdleLeft               OarToLeftPole
FightingRight     IdleRight              OarToRightPole
                  LeftPoleToOar          ReelingLeft
                  LeftToRightPole        ReelingRight
```

**Frame counts aren't uniform** — please keep the existing ones:

- Most animations: **4 frames**
- **Dead** (bass and golden fish): **6 frames**
- Worm **Dance**: **5 frames**

**One PNG per animation**, frames side by side:

| | Strip size |
|---|---|
| Fisherman (4 frames) | **1280 × 320** |
| Any fish — bass / golden / trout (4 frames) | **800 × 140** |
| Any fish — Dead (6 frames) | **1200 × 140** |
| Worm (4 frames) | **172 × 60** |
| Worm — Dance (5 frames) | **215 × 60** |

Not one big combined sheet — a full fisherman grid at this resolution would be 1280 × 8320, which is
past the safe texture limit on some hardware.

## 3. Modular fisherman — the important part

You asked the best way to make hair, clothes, oar, boat and rods swappable. Here's the number that
makes the case:

> **The fisherman is currently 336 separate frames**, because the entire 112-frame animation set is
> redrawn once for the default, once for black hair, and once for red hair.
>
> **Adding one more hair colour today means drawing 112 new frames.**

The fix is one rule:

> **Author every part on the full 320 × 320 character canvas, in the same frame order, and never
> crop or trim it.**

```
Fisherman_Boat_IdleLeft.png       1280 × 320
Fisherman_Oars_IdleLeft.png       1280 × 320
Fisherman_Body_IdleLeft.png       1280 × 320
Fisherman_Clothes_IdleLeft.png    1280 × 320
Fisherman_Arms_IdleLeft.png       1280 × 320
Fisherman_Head_IdleLeft.png       1280 × 320
Fisherman_Hair_IdleLeft.png       1280 × 320
Fisherman_Hat_IdleLeft.png        1280 × 320
Fisherman_Rods_IdleLeft.png       1280 × 320
```

Each transparent except for its own part. The game stacks them and they line up automatically on
every frame. **A new hair colour becomes 26 files instead of 112.**

This also fixes the hats. They're currently cropped icons, so the game has to *guess* where on the
head each one sits — which is why some sit too high, some too small, some too tight, and why every
new hat needs hand-tuning across all 26 animations. On the full canvas the hat is already in the
right place.

**Hair is the biggest gap** — right now it only exists as two static 64 × 64 images, which is why it
can't animate with the head and why the frames had to be duplicated in the first place.

Please **don't bake hats or hair into the body animations**. Separate layers is exactly what makes it
expandable.

The fish works the same way, on a **single shared canvas of 200 × 140** — every species and every
fish hat, so a new fish or a new hat is a straight file swap.

**200 × 140 is the frame, not the fish.** Each species is drawn at whatever size it should actually
be inside that frame, with transparency around it. A bigger canvas does not make a fish bigger:

| | |
|---|---|
| Fish zone | bottom **200 × 100** — up to roughly double today's bass |
| Hat headroom | top **40 px** — hats sit above the fish and will clip without it |
| Bass content | 110 × 70 (unchanged) |
| Trout content | 82 × 51 (unchanged) |
| Golden Fish | **TBD** — see note below |

Two rules that matter as much as the size:

- **Never crop to the drawing.** Full canvas every frame, transparent everywhere else.
- **Same baseline, same horizontal centre** for every species. Consistent size alone isn't enough —
  if one fish floats high in its frame and another sits low, one hat file can't line up on both.

Strips come out at **800 × 140** for 4-frame animations and **1200 × 140** for the 6-frame Dead ones.

**On the golden fish:** it's currently the *same size as the bass* — identical 55 × 35 source art at
the same pixel density, no scale-down. The trout is the only genuinely smaller one. So if the golden
fish is meant to read smaller, just draw it smaller inside the same 200 × 140 frame and tell us
roughly how much (half a bass? two thirds?).

## 4. Shop icons — please make these consistent

This is the messiest area right now:

- fisherman hat icons: **64 × 64**, art filling only 20–38 % of the frame
- fish hat icons: **18 × 13, 21 × 16, 24 × 15, 20 × 14** — up to **12× smaller** than the fisherman ones
- lock 185 × 280, coin 64 × 64, picture frames 365 × 350 **and** 360 × 345 (not even matching each other)

The game currently measures and rescales every icon at runtime just to make one shelf row look even.

| Asset | Please deliver |
|---|---|
| All hat icons (fish **and** fisherman) | **128 × 128**, art centred, ~90 % fill |
| Hair icons | **128 × 128** |
| Coin, lock | **128 × 128** |
| Picture frames | **256 × 256** (both the same) |

## 5. Backgrounds and signage

- Full-screen art: **1920 × 1080 (16:9)**. Your BG2 frames are already correct — the shop background
  is 768 × 384 (2:1), which is why it's never filled the screen.
- **The shop background needs a version with no signs painted into it.** "fishing supplies" and
  "sal-T shop" are currently part of the background image, so we can't move or resize them and
  they're stuck at whatever the background is scaled to. As separate PNGs over a clean background we
  can place them exactly like your reference.
- **One sign per file** — the one you sent had "fishing supplies" and "sal-T shop" stacked in a
  single PNG.
- All signs the same pixel height (**32 px**) so the lettering matches across back / close /
  sal-T shop / fishing supplies. They're currently 23, 23, 23 and 19.

## 6. Already correct — please don't change

Achievement icons (256 × 256), region and flag icons (500 × 500), the cosmetic preview composites
(500 × 500), and the BG2 background frames (1920 × 1080).

## 7. File format

PNG, straight (non-premultiplied) alpha, no baked drop shadows, nearest-neighbour scaling only —
please don't upscale with smoothing.

## 8. Existing assets

Now the sizes are pinned down, it's worth **remaking the fisherman, worm, bass, golden fish and
trout at the new resolutions** rather than upscaling — upscaled pixel art will look noticeably worse
next to the new work. That's five characters; everything else is either already correct or only
oversized, which is harmless.

Keeping the low-resolution art for the main menu is completely fine — we'll hold that in a separate
folder so the two sets never get mixed.

---

Let me know if **320 × 320 per fisherman frame** and **128 × 128 for shop icons** work for you, and
I'll confirm before you start the batch so nothing has to be redone.
