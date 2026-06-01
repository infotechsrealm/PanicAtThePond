# Fisherman Hierarchy Structure Guide

## Expected Prefab Hierarchy

```
FisherMan (Root) ← Add these components here:
├─ FishermanController (already exists)
├─ FishermanAnimationManager (NEW)
├─ FishermanAnimationIntegration (NEW)
├─ FishermanSpriteSheetParser (NEW)
├─ PhotonView (already exists)
├─ SpriteRenderer (optional, for fallback display)
└─ Animator (optional, if using legacy animator system)
    │
    ├─ Head ← Each child needs SpriteRenderer
    │  ├─ SpriteRenderer (for head/face)
    │  └─ hat Cosmetic (child of Head)
    │     └─ SpriteRenderer (for hat display)
    │
    ├─ chest ← Each child needs SpriteRenderer
    │  └─ SpriteRenderer (for torso/body)
    │
    ├─ LeftHand (or Left road) ← Each child needs SpriteRenderer
    │  └─ SpriteRenderer (for left arm/hand)
    │
    ├─ RightHand (or Right road) ← Each child needs SpriteRenderer
    │  └─ SpriteRenderer (for right arm/hand)
    │
    ├─ Boat ← Each child needs SpriteRenderer
    │  └─ SpriteRenderer (for boat body)
    │
    ├─ Oars ← Each child needs SpriteRenderer
    │  └─ SpriteRenderer (for oars)
    │
    └─ Rods ← Each child needs SpriteRenderer
       └─ SpriteRenderer (for fishing rods)
```

## Component Assignment

### FisherMan (Root GameObject)

| Component | Status | Purpose |
|-----------|--------|---------|
| Transform | Existing | Position/rotation/scale |
| FishermanController | Existing | Main game logic |
| Animator | Existing/Optional | Legacy animation system |
| PhotonView | Existing | Network sync |
| **FishermanAnimationManager** | **NEW** | **Orchestrates multi-part animations** |
| **FishermanAnimationIntegration** | **NEW** | **Bridges controller to animation system** |
| **FishermanSpriteSheetParser** | **NEW** | **Loads sprite sheets and creates frames** |

### Each Child Part (Head, chest, etc.)

| Component | Status | Purpose |
|-----------|--------|---------|
| Transform | Existing | Local position/rotation/scale |
| SpriteRenderer | Required | Displays the sprite frame |
| Animator | Optional | Can remove if using FishermanAnimationManager |

## Part Naming Convention

For the animation system to work properly, use these exact names:

```
PARTS CONTAINING ARM/HAND ANIMATIONS:
- "LeftHand"
- "RightHand"
- "Left road"  (alternative name)
- "Right road" (alternative name)

PARTS CONTAINING BODY ANIMATIONS:
- "chest"
- "body"
- "Body"

PARTS CONTAINING HEAD ANIMATIONS:
- "Head"
- "head"

PARTS CONTAINING BOAT ANIMATIONS:
- "Boat"
- "boat"

PARTS CONTAINING OARS ANIMATIONS:
- "Oars"
- "oars"

PARTS CONTAINING ROD ANIMATIONS:
- "Rods"
- "rods"
- "Rod"
```

If your names differ, update them in:
1. The prefab hierarchy
2. The sprite sheet parser configuration
3. Animation registration code

## Parent-Child Positioning

```
FisherMan (0, 0, 0) ← Root at origin
├─ Head (0, 0.5, 0) ← Higher up
│  └─ hat Cosmetic (0, 0.1, 0) ← Slightly above head
├─ chest (0, 0.2, 0) ← Center torso
├─ LeftHand (-0.2, 0.1, 0) ← Left side
├─ RightHand (0.2, 0.1, 0) ← Right side
├─ Boat (0, -0.3, 0) ← Below body
├─ Oars (-0.1, -0.2, 0) ← Left of boat
└─ Rods (0.1, -0.1, 0) ← Right of boat
```

## Z-Sorting (Depth/Layer)

For proper visual layering:

```
Z Position (back to front):

-5: Boat (background)
-4: Oars (behind character)
-3: Rods (behind character)
-2: chest (torso)
-1: LeftHand + RightHand (arms)
0: Head (face)
1: hat Cosmetic (front-most)
```

Alternative (using Sorting Order in SpriteRenderer):

```
Sorting Order (back to front):

0: Boat
1: Oars
2: Rods
3: chest
4: LeftHand
5: RightHand
6: Head
7: hat Cosmetic
```

## Sprite Renderer Settings

Each child part should have these SpriteRenderer settings:

```
SpriteRenderer Component:
├─ Sprite: (assigned by FishermanAnimationManager)
├─ Color: White (255, 255, 255, 255)
├─ Flip X: false
├─ Flip Y: false
├─ Sorting Order: (see Z-Sorting above)
├─ Material: Default (Sprites/Default)
└─ Render Mode: Auto
```

## Hat Cosmetic Special Setup

The `hat Cosmetic` child of Head needs special handling:

```
hat Cosmetic (Prefab Instance)
├─ Transform
│  └─ Parent: Head
│  └─ Local Position: (0, 0.1, 0)
│  └─ Local Rotation: (0, 0, 0)
│  └─ Local Scale: (1, 1, 1)
│
├─ SpriteRenderer
│  └─ Sorting Order: 7 (or higher than Head)
│
└─ Animator (if animating the hat)
   └─ Animation Controller: (custom hat animations)
```

## Cosmetic Application Flow

```
1. CosmeticRuntimeApplier.cs detects cosmetic selection
2. Applies hat prefab as child of Head
3. Hat sprite renders on top of Head sprite
4. FishermanAnimationManager updates all parts INCLUDING the hat
```

## Animation Sync Across Parts

All parts must update simultaneously:

```
FishermanAnimationManager.Update()
├─ Check current animation
├─ Get current frame index
└─ For each BodyPart:
   └─ Update SpriteRenderer with frame[index]

Timeline (synchronized):
Frame 0:  All parts show frame 0
Frame 1:  All parts show frame 1
Frame 2:  All parts show frame 2
...
```

## Troubleshooting Hierarchy

### Problem: Some parts don't animate

**Check:**
- [ ] Part is a child of FisherMan (not sibling)
- [ ] Part has SpriteRenderer component
- [ ] Part name matches animation configuration
- [ ] SpriteRenderer is not disabled

### Problem: Hat cosmetic disappears

**Check:**
- [ ] hat Cosmetic is child of Head (not root)
- [ ] hat Cosmetic has SpriteRenderer
- [ ] Sorting Order of hat is higher than Head
- [ ] hat is not being disabled by cosmetic system

### Problem: Wrong animation plays

**Check:**
- [ ] Part names match sprite sheet configuration
- [ ] Frame dimensions (256×192) match your sprites
- [ ] Total frames count is correct for each animation
- [ ] Animation names match in parser configuration

## Reference: Your Current Structure (from images)

From your reference image, you have:
```
FisherMan (1)
├─ chest
├─ right road
├─ Left road
├─ Boat
├─ LeftHand
├─ Head
│  ├─ hat Cosmetic
│  └─ bar
└─ RightHand
```

**To use with this system:**
- Rename "right road" → "RightHand"
- Rename "Left road" → "LeftHand"
- Keep others as-is
- Ensure each has a SpriteRenderer
