# Handoff — Cosmetic hat/animation sync rebuild

**Project:** `D:\PanicAtThePond` · Unity **6000.5.7f1** · URP · uGUI (UI Toolkit is waived on this project)
**Written:** 2026-08-09
**Status:** problem fully diagnosed, three partial fixes landed, **the real fix is not implemented yet**
**Audience:** the next session, which should implement the plan in §4

> Read §1–§3 before touching code. Three previous attempts each fixed a *real* bug and still did not
> solve the reported symptom, because the underlying approach cannot solve it. Do not start by
> looking for a fourth bug in the same place.

---

## 1. The problem

Cosmetic hats do not stay attached to the head while the character animates. Reported repeatedly by
the user against live builds, for **both** the fisherman and the fish. The hat drifts, lags, or sits
detached during movement.

There is a second, related structural problem: the fisherman is meant to be built from separately
animated parts (body, arms, head, oars, rods, boat, hair, hat) and those parts are not reliably
frame-locked to each other either.

---

## 2. What was already tried — all three were real bugs, none was the cause

Do **not** re-investigate these. They are fixed, tested, and committed to the working tree.

### 2.1 Off-by-one frame index — FIXED

`CosmeticRuntimeApplier.GetCurrentSpriteFrameIndex()` returned `trailingNumber % 4`. Every shipping
clip names frames **1-based** (`IdleLeft1`…`IdleLeft4`), but every bob table is **0-based**, so the
lookup was one frame late and frame 4 wrapped onto frame 0.

Fixed by distinguishing the project's two naming conventions: digits after `_` are a 0-based sheet
slice (`..._Sheet_6`); digits attached directly to a state name are a 1-based frame (`IdleLeft2`).

### 2.2 Bob magnitude — FIXED

The pixel→world conversion was hard-coded `* 0.01f`, which is only correct at 100 PPU.

| | Sprite PPU | 1 px = | Code used | Error |
|---|---|---|---|---|
| Fisherman | **25** | 0.0400 units | 0.0100 | **4× too small** |
| Fish | **50** | 0.0200 units | 0.0100 | **2× too small** |

Fixed with a `UnitsPerPixel` property read from the body sprite at runtime.

### 2.3 The offset table describes the wrong artwork — FIXED, still not enough

`HeadCenterYGrid` (a hand-maintained 24×4 constant) was measured from
`FishermansAnimations-Head_Sheet.png`. **The shipping fisherman renders composited sprites**
(`IdleLeft1`, `CastingLeft2`, …) whose head moves differently:

| Clip | Real head Δ | Table Δ | |
|---|---|---|---|
| `AC_IdelLeft` | 0, −1, −1, 0 | 0, −1, **+1, +1** | ✗ |
| `AC_CastingLeft` | 0, **+6**, +1, 0 | 0, **0**, +1, +1 | ✗ 6 px out |
| `AC_IdelRight` | 0, −1, **−1**, 0 | 0, −1, **0**, 0 | ✗ |
| `AC_MoveForward` | 0, −1, −1, 0 | 0, −1, −1, 0 | ✓ coincidence |

Replaced with `HeadCrownTable` — a ScriptableObject baked from the sprites that actually render
(**149 crowns**), regenerated via **Panic At The Pond ▸ Rebuild Head Crown Table**.

**A trap worth keeping:** a plain "topmost opaque pixel" scan does **not** find the head — the
fishing rod and line reach above it in casting frames and the hat swings wildly. The bake requires
the first row whose widest continuous opaque run is **≥ 6 px**, which skips thin structures.

### 2.4 A separate bug found and fixed along the way — fish hat vanishing

Unrelated to sync, already fixed, mentioned so it is not re-broken. A real save contained
`SelectedFishHatCosmetic = FisherMan_Hat_-Default_-_Fishing_Hat` — a fisherman hat in the fish slot,
which cannot resolve, so `ApplyFishHatByName` removed the hat and the fish rendered bare.

Cause: `ShopManager`'s hierarchy routing had a fall-through — a button under neither cosmetics root
kept `isFishermanCosmetic` from the *previous* click. Fixed in three layers (routing fallback by
`shop_config.json` category, a setter guard, and a self-heal on load).

> **Do not use loose name matching for category checks.** `AreSpritesMatching(..., exactOnly: false)`
> classifies the fish hat `cap` as the fisherman hat `FisherMan_Hat_-Blue_Cap`, because one contains
> the other once separators are stripped. Use exact normalised-id comparison. There is a test for it.

---

## 3. Why the current design can never be 100% — read this before proposing a fix

Everything in `CosmeticRuntimeApplier` is **reconstruction**. Every `LateUpdate` it reads the body
sprite's name, parses a frame index, and looks up an offset to guess where the head is. That is not
fixable by better data:

1. **It only solves the vertical axis.** The head also moves horizontally and tilts. A crown row
   cannot express X or rotation.
2. **Silhouette ≠ attachment point.** The top of the head is not where a hat sits, and the correct
   sit point differs per hat — a cap sits on the forehead, a top hat on the crown, a soda hat perches
   above it.
3. **It is a frame behind by construction.** Inferring animation state from the already-rendered
   sprite breaks during transition blends, when two clips share a sprite, and when a name does not
   parse.
4. **Every new cosmetic needs new hand data**, which is the opposite of the stated requirement.

Measurement made it closer. It cannot make it exact.

---

## 4. The plan

### 4.1 Key the attachment point inside the animation itself

Add a child transform — `HeadAnchor` — to the fisherman and the fish prefabs. **Animate that
transform's `localPosition` (and `localRotation` where the head tilts) inside the same AnimationClips
that already swap the sprites.** Cosmetics become plain children of `HeadAnchor`.

Why this is exactly correct rather than approximately:

> The anchor and the sprite are keyed on **one timeline** and sampled by **the same evaluator in the
> same frame**. There is no code between them. Desync is structurally impossible, not merely unlikely.

Consequences — note that the correct fix **removes** code:

- Delete the LateUpdate positioning logic: frame-index parsing, PPU maths, `HeadCenterYGrid`, the
  per-hat branches for ranger / turtle / blue cap, and the `GetFisherman*Offset` family.
- `HeadCrownTable` stops being a runtime dependency. **Keep it** — it is the seed data for the curves
  (§4.4) and is still useful for validation.
- A hat carries at most **one** "sit offset" from the anchor, not a per-frame table.
- Works during blends, at any framerate, for any animation added later.

### 4.2 Frame-lock the fisherman's parts

Same root problem in a different place. The parts currently have **separate Animators** kept in step
by `FishermanChildAnimatorSync` — whose `SyncChildStates()` is an empty no-op with a comment
explaining it could not be made to work. Separate animators drift; that is inherent.

> **One AnimationClip per animation, keying every part's `SpriteRenderer.sprite` as its own curve on
> the same timeline** — body, arms, head, oars, rods, boat, hair, hat.

One clip, many curves, one playhead. Deletes `FishermanChildAnimatorSync` as well.

### 4.3 Make cosmetics drop-in

A `CosmeticCatalog` ScriptableObject already exists in the project and is underused. The target is:

1. Drop the art in a folder
2. Add one catalog row — id, display name, category, price, icon, sprite/layer reference, optional sit offset
3. Done

No code, no offsets, no per-animation work. The catalog's `category` field is also the correct
long-term fix for fish/fisherman routing, replacing the name-prefix guard added in §2.4.

### 4.4 Suggested order of work

1. Add `HeadAnchor` to `Resources/Fisherman` and `Resources/Fish`.
2. Write an **editor tool** that seeds a `localPosition.y` curve into each of the 26 fisherman clips
   (and the fish clips) from `HeadCrownTable`. This gets every frame close automatically.
3. Hand-correct in the Animation window. Budget real attention for `AC_CastingLeft`,
   `AC_CastingRight`, `AC_FightingLeft/Right` — the head genuinely moves 6+ px and leans there.
4. Re-point cosmetic creation so hats parent to `HeadAnchor`.
5. **Delete** the placement tables and per-hat branches.
6. Then §4.2 (one clip, many curves) and §4.3 (catalog).

Steps 1–5 are roughly a day. Step 6 is a larger, separable piece.

---

## 5. What NOT to do

| Don't | Why |
|---|---|
| Add another offset table or per-hat special case | This is the fourth attempt at that; see §3 |
| Infer the animation frame from the sprite name in `LateUpdate` | Reconstruction — breaks on blends, shared sprites, unparseable names |
| Enable Read/Write on the frame textures to measure at runtime | 104 distinct fisherman textures; the bake already solves it at zero runtime cost |
| Use "topmost opaque pixel" to find the head | The fishing rod reaches higher; use the ≥6 px run rule |
| Use loose name matching for cosmetic categories | `cap` matches `FisherMan_Hat_-Blue_Cap`; use exact id comparison |
| Trust screenshots, or tests that assert only direction | Both let a 4×-too-small bob pass. See §6 |
| Bake hats into body animations | Kills expandability; the client has been told not to |
| Keep separate Animators per body part | They drift; that is why the sync script exists and fails |
| Restart Unity without restarting Claude Code | The MCP server respawns with Unity and orphans the session |

---

## 6. How to verify — and why earlier verification failed

Earlier suites passed a badly broken build. Understand why before writing new tests:

- The first suite asserted only **direction** (`Is.GreaterThan` / `Is.LessThan`). A 4×-too-small bob
  passes that.
- The PlayMode suite sampled **one** animation (`AC_IdelLeft`) — one of the few clips where the wrong
  table coincidentally had the right sign.
- Screenshots sample whichever frame the renderer happened to be on. The user correctly called this
  out.

**The verification that does work** is already in the repo as
`Scripts/Tests/EditMode/HatTracksHeadTests.cs`: it walks **every clip** on the shipping prefabs and,
for each frame, compares how far the head really moved (from `HeadCrownTable`) against how far the
hat moved, **in pixels**, requiring 0 error.

Current result — and the bar the rebuild must still clear:

```
Fisherman : 102 frames checked   WORST mismatch 0 px
Fish      :  24 frames checked   WORST mismatch 0 px
```

Keep this test working through the rebuild. It is independent of *how* the hat is positioned, so it
validates the anchor approach just as well as the table approach.

**Full suite must stay green: EditMode 45/45.**

Note the honest limitation: this proves the maths, not the artistic result. A human still has to look
at casting and fighting. Screenshot only to *confirm* something the measurement already established —
never as the primary evidence.

---

## 7. Project facts the next session will need

### Characters

| | Prefab | Sprite | PPU | Prefab scale | World size |
|---|---|---|---|---|---|
| Fisherman | `Resources/Fisherman` | `IdleLeft1` 64×64 | **25** | 1.25 | 3.20 × 3.20 |
| Fish (Bass) | `Resources/Fish` | `Idle1` 55×35 | **50** | 1.0 | 1.10 × 0.70 |

Six different PPU values exist across the project (25, 35, 50, 100, 200, 500). Do not assume 100.

**The shipping `Resources/Fisherman` prefab is NOT modular** — it is a single `SpriteRenderer` with
one Animator (`ANIM_FisherManRedHair`, **26 clips**, 4 frames each). The modular path
(`FishermanAnimationManager`, `FishermanChildAnimatorSync`, `head`/`chest`/`oar` children) is **dead
code for in-game play**. §4.2 is about building the modular rig properly, not repairing that one.

Frame counts are not uniform: most animations are 4 frames, fish `Dead` is **6**, worm `Dance` is **5**.

### Engine settings already changed (do not undo)

- Camera `orthographicSize` **5 → 5.4** in Play and Dash → exactly **100 px per world unit** at 1080p.
- Canvas reference resolution unified to **1920 × 1080**; `Canvas_Play` moved from 800 × 600 with all
  15 root children compensated ×2.07846 (exact at every resolution).

### Key files

| Path | Note |
|---|---|
| `Scripts/Shop/CosmeticRuntimeApplier.cs` | ~2,200 lines. The placement logic to delete. |
| `Scripts/Shop/ShopManager.cs` | ~4,600 lines. Cosmetic selection + routing. |
| `Scripts/Data/HeadCrownTable.cs` | Baked crown data; seed for the curves. |
| `Scripts/Editor/HeadCrownTableBuilder.cs` | Regenerates it. Menu: Panic At The Pond ▸ Rebuild Head Crown Table |
| `Scripts/Controllers/FishermanChildAnimatorSync.cs` | To be deleted in §4.2 |
| `Scripts/Tests/EditMode/HatTracksHeadTests.cs` | The per-frame audit |
| `Scripts/Editor/ProjectBuilder.cs` | Menu: Panic At The Pond ▸ Build Windows Player |
| `Assets/_Project/project_overview.md` | Living audit — **append every session** |
| `ASSET_SPECIFICATION.md` | Measured asset spec (100 PPU rule) |
| `CLIENT_MESSAGE.md` | Paste-ready art brief |

### Working with the Editor

- Unity MCP (`ai-game-developer`) is connected, **79/79 tools enabled**.
- **Restarting Unity orphans the Claude session's MCP connection** — the server respawns with Unity
  and Claude only connects at startup. Symptom: Unity green, MCP server green, **AI agent orange**.
  Fix: restart Claude Code with Unity already running.
- MCP call timeout is 10 s. Long operations (builds) must be invoked so they outlive the call;
  `EditorApplication.delayCall` from a *dynamically compiled* snippet is silently dropped — call into
  a real compiled Editor assembly instead.
- Two-client testing: build to `Build/`, run one instance from the Editor and one from the build.
  Editor and player have **separate PlayerPrefs stores** (`HKCU\Software\Unity\UnityEditor\...` vs
  `HKCU\Software\InfoTechsRealm\Panic At The Pond`) — setting a cosmetic in the Editor does **not**
  affect the build.

---

## 8. The requirement that drives all of it

> **There will be many more cosmetics — hats, hair, clothes, boats, oars, rods. Adding one must be
> easy and must not require code.**

Judge every design decision against that. Concretely, adding a cosmetic should be:

1. Drop the art
2. Add one catalog row
3. Ship

If a proposed solution requires touching a table, an offset, a switch statement, or an animation for
each new cosmetic, **it is the wrong solution** — that is precisely the trap the current code fell
into, and there are already bespoke branches for the ranger hat, turtle hat and blue cap to prove it.

### How this converges with the incoming art

The client is remaking all character art at higher resolution as **full-canvas layered parts** (see
`ASSET_SPECIFICATION.md`). Under that contract a hat is already drawn in the right place in its own
file and composites at 0,0 — needing no offset at all.

These plans do not compete:

| | Now (current art) | After layered art |
|---|---|---|
| Hat placement | `HeadAnchor` keyed in-clip | composites at 0,0; anchor not needed for hats |
| Part sync | one clip, many curves | same |
| Adding a cosmetic | catalog row + art | catalog row + art |

Building the anchor now is **not throwaway** — held items, particles and any future attachment still
need it, and the one-clip-many-curves structure is exactly what the layered art wants.

---

## 9. Definition of done

- [ ] Hat parented to `HeadAnchor`; no positioning code in `LateUpdate`
- [ ] `HeadCenterYGrid` and the per-hat placement branches **deleted** from `CosmeticRuntimeApplier`
- [ ] `HatTracksHeadTests` still reports **0 px** worst mismatch on both characters
- [ ] EditMode suite green (45/45 or more)
- [ ] A new hat can be added with art + one catalog row, demonstrated end to end
- [ ] Visually confirmed in a running build on casting and fighting animations
- [ ] `project_overview.md` updated with what changed and how it was verified

**The strongest signal of success is that the code gets smaller.** Roughly 2,200 lines of placement
logic should collapse to "parent the hat to the anchor."

---

## 10. Corrections from 2026-08-09 (work reverted; read before trusting §2, §6 or §9)

This document was written before the anchor was built. Two of its claims are now known to be wrong,
and one to be incomplete. The implementing work was reverted to `632ce062`, so nothing here is fixed
in code — see `Assets/_Project/project_overview.md` §26–§27 for the full record.

**§6 is wrong.** `HatTracksHeadTests` was described as "the verification that does work". It could
not fail: the runtime positioned the hat from `HeadCrownTable` and the test computed the expected
head movement from the same table, so the base row cancels and it asserted `A == A`. It reported
"0 px worst mismatch" on a build with a hat 20 px above the head. It also rounded errors to whole
pixels, and stepped the animator with `animator.Play(clip.name)` — a *state* name is required, so 26
clips all re-measured the same four idle sprites.

**§2.3's measurement is wrong.** "`AC_CastingLeft` real head Δ = 0, +6, +1, 0" is the fishing rod,
not the head. The rod is exactly 6 px wide, so the `≥ 6 px` rule in §2.3's own trap note reports the
rod on casting frames. The real head is 11 px wide at x≈30.5, row 11–12, in every frame.

**§4.1 is incomplete.** Deleting the placement branches also deletes the *mirroring*: the fisherman
never flips via `flipX`, and three hats carry `y = -160°` in their authored rotation as their mirror.
The branches also held per-state rotation the hats disagree on by up to 21°, so a single anchor
rotation curve cannot reproduce them — that data was per-hat styling, not head lean.

**§9's checklist should add:** X keying (the head moves up to 4 px horizontally), both fish species
(`Fish 2` is selectable and has its own placement), and an absolute-placement check — tracking tests
compare movement only, so a constant offset is invisible to them.
