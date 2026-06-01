# Fisherman Multi-Part Animation Implementation Checklist

## Quick Start (In Unity Editor)

### Phase 1: Setup (5 minutes)

- [ ] **1. Open Your Fisherman Prefab**
  - `Assets/Resources/FisherMan.prefab` or `FisherMan (2).prefab`
  - In Unity Editor, double-click to edit prefab

- [ ] **2. Add Three New Components**
  - Select root `FisherMan` GameObject
  - In Inspector → Add Component:
    - `FishermanAnimationManager`
    - `FishermanAnimationIntegration`
    - `FishermanSpriteSheetParser`

- [ ] **3. Verify Sprite Sheets Are Imported**
  - Check: `Assets/Animations/Fisher Man Animations/Sprite Sheets/`
  - Should see 5 PNG files:
    - ✓ FishermansAnimations-Arms_Sheet.png
    - ✓ FishermansAnimations-Boat_Sheet.png
    - ✓ FishermansAnimations-GreenBody_Sheet.png
    - ✓ FishermansAnimations-Oars_Sheet.png
    - ✓ FishermansAnimations-Rods_Sheet.png

### Phase 2: Configure Animations (10 minutes)

- [ ] **4. Configure FishermanSpriteSheetParser**
  - In Inspector, find `FishermanSpriteSheetParser` component
  - Set `Animation Configs > Size: 9` (for 9 animations)
  
- [ ] **5. Add Animation Configurations**
  
  For each animation below, click "+" and fill in:

  **Animation 0: Idle**
  - Animation Name: `idle`
  - Sprite Sheet: `FishermansAnimations-GreenBody_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `1`

  **Animation 1: Move Forward**
  - Animation Name: `moveForward`
  - Sprite Sheet: `FishermansAnimations-GreenBody_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `4`

  **Animation 2: Move Backward**
  - Animation Name: `moveBackward`
  - Sprite Sheet: `FishermansAnimations-GreenBody_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `4`

  **Animation 3: Casting**
  - Animation Name: `casting`
  - Sprite Sheet: `FishermansAnimations-Rods_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `3`

  **Animation 4: Fishing**
  - Animation Name: `fishing`
  - Sprite Sheet: `FishermansAnimations-Rods_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `2`

  **Animation 5: Fighting**
  - Animation Name: `fighting`
  - Sprite Sheet: `FishermansAnimations-Rods_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `4`

  **Animation 6: Crying**
  - Animation Name: `crying`
  - Sprite Sheet: `FishermansAnimations-Boat_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `3`

  **Animation 7: Win**
  - Animation Name: `win`
  - Sprite Sheet: `FishermansAnimations-Arms_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `1`

  **Animation 8: Reeling**
  - Animation Name: `reeling`
  - Sprite Sheet: `FishermansAnimations-Rods_Sheet`
  - Frame Width: `256`
  - Frame Height: `192`
  - Total Frames: `3`

- [ ] **6. Set Animation Manager Frame Rate**
  - Find `FishermanAnimationManager` component
  - Set `Frame Rate: 0.1` (10 FPS for smooth animation)

### Phase 3: Test (5 minutes)

- [ ] **7. Test in Play Mode**
  - Enter Play Mode (Ctrl+P)
  - Press arrow keys to move
  - Press W/S to select rod
  - Press X+V to cast
  - Watch all body parts animate together

- [ ] **8. Debug if Needed**
  - Open Console (Ctrl+`)
  - Look for "✓" messages confirming initialization
  - If animations don't play, check:
    - [ ] Sprite sheets are assigned correctly
    - [ ] Frame dimensions match your sprites (256×192)
    - [ ] All body parts have SpriteRenderer components
    - [ ] "Generated Sprites" count matches total frames

### Phase 4: Integration (Optional - for advanced setup)

- [ ] **9. Integrate with FishermanController (if using old animator system)**
  - In `FishermanController.cs`, add at line ~42:
  ```csharp
  private FishermanAnimationIntegration animationIntegration;
  ```
  
  - In `Start()`, after existing code (around line ~75):
  ```csharp
  animationIntegration = GetComponent<FishermanAnimationIntegration>();
  ```

  - Replace animator calls with:
  ```csharp
  // Example: In FisherManMovement() where you set movement animations
  // OLD: animator.SetBool("moveForward_l", moveInput < 0);
  // NEW:
  if (animationIntegration != null)
  {
      animationIntegration.PlayMovementAnimation(moveInput < 0);
  }
  ```

## Animation Frame Structure

Your sprite sheets should have this layout (1536 × 256):

```
Row 0: Frame 1 | Frame 2 | Frame 3 | Frame 4 | Frame 5 | Frame 6 | Frame 7 | Frame 8
       (192px high, 256px wide each)

If 8 animations total:
- Rows 0-191px: Animation 1 (Idle)
- Rows 192-383px: Animation 2 (Move Forward)
- Rows 384-575px: Animation 3 (Move Backward)
- Rows 576-767px: Animation 4 (Casting)
- Rows 768-959px: Animation 5 (Fishing)
- Rows 960-1151px: Animation 6 (Fighting)
- Rows 1152-1343px: Animation 7 (Crying)
- Rows 1344-1535px: Animation 8 (Winning)
```

## Troubleshooting Quick Fix

If animations aren't working:

1. **Check Sprite Import Settings**
   - Select each PNG file in Assets
   - Inspector → Texture Type: `Sprite (2D and UI)`
   - Sprite Mode: `Single` (NOT `Multiple`)
   - Apply

2. **Verify Child Objects Have SpriteRenderers**
   - Select FisherMan prefab
   - Expand hierarchy
   - Each part (Head, Arms, Body, etc.) should have a SpriteRenderer

3. **Check Console Output**
   - Play game, look for initialization messages
   - "✓ Fisherman Animation Manager: Initialized X body parts"
   - "✓ Registered animation: X (Y frames)"

4. **Reset Frame Rate**
   - Try Frame Rate: 0.15 if animation seems jittery
   - Try Frame Rate: 0.05 if animation seems too slow

## Next Steps

Once basic animations work:

1. **Add Direction Flipping** - Flip sprites for left/right facing
2. **Synchronize Body Parts** - Ensure all parts use same animation timing
3. **Add Audio Sync** - Play sounds when animations reach certain frames
4. **Cosmetic Integration** - Apply hat/hair cosmetics on top of animations

---

**Need Help?**
- Check `FISHERMAN_ANIMATION_SETUP.md` for detailed documentation
- Check console for error messages starting with ✗ or ⚠
- Verify sprite sheet import settings (Texture Type = Sprite)
