# Fisherman Multi-Part Animation System Setup Guide

## Overview
This system synchronizes animations across multiple body parts (Head, Arms, Body, Boat, Oars, Rods) of your fisherman character. Each body part animates in sync based on the fisherman's current action.

## Architecture

```
FishermanController (existing)
    ↓
FishermanAnimationIntegration (adapter)
    ↓
FishermanAnimationManager (orchestrator)
    ↓
BodyPartAnimators (individual part animators)
```

## Step 1: Prepare Your Sprite Sheets

### Sprite Sheet Format
Your sprite sheets are already in the correct format:
- **Resolution**: 256×1536 px (width × height)
- **Format**: PNG, 8-bit RGBA
- **Location**: `Assets/Animations/Fisher Man Animations/Sprite Sheets/`

Your sprites:
- `FishermansAnimations-Arms_Sheet.png` - Arms/Hands animation
- `FishermansAnimations-Boat_Sheet.png` - Boat animation
- `FishermansAnimations-GreenBody_Sheet.png` - Body/Torso animation
- `FishermansAnimations-Oars_Sheet.png` - Oars animation
- `FishermansAnimations-Rods_Sheet.png` - Fishing rods animation

### Frame Analysis
With 256×1536 resolution and typical animation frames:
- **Frame Size**: 256×192 px (if using 8 frames vertically)
- **Total Frames per Sheet**: 8 frames (1536÷192 = 8)
- **Animations per Sheet**: 8 different animations stacked vertically

## Step 2: Fisherman Prefab Structure

Your prefab should have this hierarchy:
```
FisherMan (Root)
├── Head
│   └── hat Cosmetic
├── chest
├── LeftHand (or Left road)
├── RightHand (or Right road)
├── Boat
├── Oars
└── Rods
```

Each part needs a **SpriteRenderer** component.

## Step 3: Set Up the Animation Manager

### 3a. Add Components to Fisherman Prefab

1. **Add `FishermanAnimationManager` script**:
   - GameObject: `FisherMan` (root)
   - This orchestrates all animations

2. **Add `FishermanAnimationIntegration` script**:
   - GameObject: `FisherMan` (root)
   - This bridges FishermanController to AnimationManager

3. **Add `FishermanSpriteSheetParser` script**:
   - GameObject: `FisherMan` (root)
   - This loads and parses sprite sheets

### 3b. Configure Animation Clips in Inspector

1. Select `FisherMan` prefab
2. In Inspector, find `FishermanSpriteSheetParser` component
3. Add animation configurations for each body part:

```
Animation Configurations:

1. Arms_Idle
   - Sprite Sheet: FishermansAnimations-Arms_Sheet
   - Frame Width: 256
   - Frame Height: 192
   - Total Frames: 1
   - Start Frame: 0

2. Arms_MovingForward
   - Sprite Sheet: FishermansAnimations-Arms_Sheet
   - Frame Width: 256
   - Frame Height: 192
   - Total Frames: 4
   - Start Frame: 1

3. Arms_Casting
   - Sprite Sheet: FishermansAnimations-Arms_Sheet
   - Frame Width: 256
   - Frame Height: 192
   - Total Frames: 3
   - Start Frame: 5

4. Arms_Fighting
   - Sprite Sheet: FishermansAnimations-Arms_Sheet
   - Frame Width: 256
   - Frame Height: 192
   - Total Frames: 4
   - Start Frame: 0

5. Arms_Crying
   - Sprite Sheet: FishermansAnimations-Arms_Sheet
   - Frame Width: 256
   - Frame Height: 192
   - Total Frames: 3
   - Start Frame: 0
```

### Repeat for each body part:
- Body (GreenBody_Sheet)
- Boat (Boat_Sheet)
- Oars (Oars_Sheet)
- Rods (Rods_Sheet)
- Head (use a separate sheet or keep current sprite)

## Step 4: Integrate with FishermanController

Add this to `FishermanController.cs`:

```csharp
// At the top of the class
private FishermanAnimationIntegration animationIntegration;

// In Start() method, after existing initialization
animationIntegration = GetComponent<FishermanAnimationIntegration>();

// Replace existing animator calls with these:

// For movement animations
// OLD: animator.SetBool("moveForward_l", moveInput < 0);
// NEW:
if (animationIntegration != null)
{
    animationIntegration.PlayMovementAnimation(moveInput < 0);
}

// For crying
// OLD: animator.SetBool("isCrying_r", res);
// NEW:
if (animationIntegration != null)
{
    animationIntegration.PlayCryingAnimation();
}

// For fighting
// OLD: animator.SetBool("isFighting_r", res);
// NEW:
if (animationIntegration != null)
{
    animationIntegration.PlayFightingAnimation();
}

// For casting
// OLD: animator.SetTrigger("casting_l");
// NEW:
if (animationIntegration != null)
{
    animationIntegration.PlayCastingAnimation();
}

// For win
// OLD: animator.SetBool("isWin_r", true);
// NEW:
if (animationIntegration != null)
{
    animationIntegration.PlayWinAnimation();
}
```

## Step 5: Animation State Mapping

Map each animation state to all body parts:

### Animation States
```
IDLE
  - Arms: Hands at rest
  - Body: Upright stance
  - Boat: Stationary
  - Oars: Resting position
  - Rods: Held neutral

MOVING_FORWARD
  - Arms: Rowing motion (cycle 4 frames)
  - Body: Leaning forward
  - Boat: Moving
  - Oars: Rowing motion
  - Rods: Held up

MOVING_BACKWARD
  - Arms: Reverse rowing (cycle 4 frames)
  - Body: Leaning backward
  - Boat: Moving
  - Oars: Reverse motion
  - Rods: Held back

CASTING
  - Arms: Throwing motion (3 frames)
  - Body: Bending forward
  - Boat: Rocking
  - Oars: Static
  - Rods: Extended

FISHING
  - Arms: Holding steady
  - Body: Leaning with tension
  - Boat: Rocking slightly
  - Oars: Steady
  - Rods: Bent under tension

FIGHTING
  - Arms: Struggling (cycle 4 frames)
  - Body: Tensing up
  - Boat: Rocking violently
  - Oars: Gripping
  - Rods: Bending dramatically

CRYING
  - Arms: Covering face (3 frames)
  - Body: Slouching
  - Boat: Tilted
  - Oars: Slack
  - Rods: Dropped

WINNING
  - Arms: Raised in victory
  - Body: Standing tall
  - Boat: Stable
  - Oars: Raised
  - Rods: Held high
```

## Step 6: Testing

1. Enter Play Mode
2. Test each animation state:
   - Press arrow keys for movement
   - Press W/S for rod selection
   - Press X+V for casting
   - Watch all body parts animate in sync

## Troubleshooting

### Animations not playing
- Check SpriteRenderer components on all child objects
- Verify sprite sheets are in the correct folder
- Check console for errors in FishermanSpriteSheetParser

### Body parts not synchronized
- Ensure all body parts have the same frame rate
- Check that `FishermanAnimationManager.frameRate` is set to 0.1f

### Cosmetics not showing
- Hat Cosmetic will be handled separately by `CosmeticRuntimeApplier`
- Ensure hat is a child of the Head object

## Performance Optimization

- Use object pooling for frequently changed sprites
- Consider using a shader-based animation system for better performance
- Cache sprite references to avoid repeated lookups

## Future Enhancements

- Add blending between animation states
- Implement direction flipping (left/right facing)
- Add sound effects synchronized with animations
- Create animation previewer for testing
