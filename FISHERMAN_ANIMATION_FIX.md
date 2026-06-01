# 🔧 FISHERMAN ANIMATION - COMPLETE FIX GUIDE

## Problems to Fix

### ❌ Problem 1: Duplicate Heads (Two Heads Visible)
**Cause**: Old SpriteRenderer + new animation system both active
**Fix**: Disable the original head sprite renderer

### ❌ Problem 2: Animations Not Playing
**Cause**: Sprite sheets not loaded properly
**Fix**: Use NEW simplified FishermanAnimationSystem

### ❌ Problem 3: Hat Cosmetic Not Applied
**Cause**: Hat system separate from animation
**Fix**: Create cosmetic integration layer

---

## STEP-BY-STEP FIX (15 Minutes)

### STEP 1: Delete Old Animation Components (2 min)

1. **Open Fisherman Prefab**
   - `Assets/Resources/FisherMan (2).prefab`

2. **Remove these components from root FisherMan:**
   - ❌ FishermanAnimationManager (DELETE)
   - ❌ FishermanAnimationIntegration (DELETE)
   - ❌ FishermanSpriteSheetParser (DELETE)

3. **Keep these:**
   - ✓ FishermanController
   - ✓ Animator
   - ✓ PhotonView
   - ✓ Network components

---

### STEP 2: Add NEW Animation System (1 min)

1. **Select root FisherMan GameObject**

2. **Add Component → FishermanAnimationSystem**

3. **In Inspector, assign these Sprite Sheets:**
   - Arms Sheet: `FishermansAnimations-Arms_Sheet`
   - Boat Sheet: `FishermansAnimations-Boat_Sheet`
   - Body Sheet: `FishermansAnimations-GreenBody_Sheet`
   - Oars Sheet: `FishermansAnimations-Oars_Sheet`
   - Rods Sheet: `FishermansAnimations-Rods_Sheet`

   (Located at: `Assets/Animations/Fisher Man Animations/Sprite Sheets/`)

---

### STEP 3: Fix Duplicate Head (2 min)

1. **Expand Hierarchy:**
   ```
   FisherMan (root)
   └─ head (FIND THIS)
   ```

2. **Select "head" GameObject**

3. **In Inspector, find the SpriteRenderer component**

4. **DISABLE it:**
   - Uncheck the checkbox next to "SpriteRenderer"
   - Or set Sprite to None

5. **Result:** Only the animated head will show now

---

### STEP 4: Setup Hat Cosmetic (3 min)

**Create new script:** `Assets/Scripts/FishermanCosmeticSystem.cs`

```csharp
using UnityEngine;

public class FishermanCosmeticSystem : MonoBehaviour
{
    [SerializeField] private Transform headTransform;
    private GameObject activeHatInstance;

    private void Start()
    {
        // Find head child
        if (headTransform == null)
        {
            headTransform = transform.Find("head");
        }

        // Apply saved cosmetic on scene load
        ApplySavedCosmetic();
    }

    public void ApplyHatCosmetic(string hatName)
    {
        // Clear old hat
        if (activeHatInstance != null)
        {
            Destroy(activeHatInstance);
        }

        if (string.IsNullOrEmpty(hatName) || hatName == "None")
        {
            return;
        }

        // Load hat prefab from resources
        GameObject hatPrefab = Resources.Load<GameObject>($"Cosmetics/Hats/{hatName}");

        if (hatPrefab != null && headTransform != null)
        {
            activeHatInstance = Instantiate(hatPrefab, headTransform);
            activeHatInstance.name = hatName;

            // Ensure hat sprite renders on top
            SpriteRenderer hatRenderer = activeHatInstance.GetComponent<SpriteRenderer>();
            if (hatRenderer != null)
            {
                hatRenderer.sortingOrder = 100; // Render on top
            }

            Debug.Log($"✓ Applied hat: {hatName}");
        }
        else
        {
            Debug.LogWarning($"⚠ Hat prefab not found: {hatName}");
        }
    }

    private void ApplySavedCosmetic()
    {
        string savedHat = PlayerPrefs.GetString("SelectedHat", "");
        if (!string.IsNullOrEmpty(savedHat))
        {
            ApplyHatCosmetic(savedHat);
        }
    }

    public void ClearHat()
    {
        if (activeHatInstance != null)
        {
            Destroy(activeHatInstance);
            activeHatInstance = null;
        }
    }
}
```

---

### STEP 5: Connect Cosmetic System (2 min)

1. **Select FisherMan prefab root**

2. **Add Component → FishermanCosmeticSystem**

3. **In Inspector:**
   - Drag "head" child to "Head Transform" field

4. **Set Frame Rate on AnimationSystem:**
   - `FishermanAnimationSystem` → Frame Rate: `0.1`

---

### STEP 6: Test Animation Playback (3 min)

1. **Enter Play Mode** (Ctrl+P)

2. **Test Movement:**
   - Press LEFT/RIGHT arrow keys
   - Should see rowing animation
   - ✓ All body parts animate together
   - ✓ Only ONE head visible

3. **Test Casting:**
   - Press W or S (select rod)
   - Press X + V (cast)
   - Should see casting animation

4. **Test Other States:**
   - Moving forward/backward
   - Should play different frames

5. **Check Console:**
   - Should see "✓ Fisherman Animation System: Ready!"
   - Should see "Found X body parts"

---

### STEP 7: Apply Hat Cosmetic (2 min)

In your Shop or Cosmetic Selection script, when hat is purchased:

```csharp
// Get reference to fisherman
FishermanCosmeticSystem cosmeticSystem = FindObjectOfType<FishermanCosmeticSystem>();

// Apply the selected hat
cosmeticSystem.ApplyHatCosmetic("RedCap");  // or whatever hat name

// Save selection
PlayerPrefs.SetString("SelectedHat", "RedCap");
```

---

## Troubleshooting

### Problem: Still two heads visible
**Solution:**
- [ ] Select "head" child object
- [ ] Disable its SpriteRenderer component
- [ ] Check that FishermanAnimationSystem found 9 body parts

### Problem: Animations still not playing
**Solution:**
- [ ] Check sprite sheets are assigned in FishermanAnimationSystem inspector
- [ ] Verify sprite sheets are in: `Assets/Animations/Fisher Man Animations/Sprite Sheets/`
- [ ] Check console for errors
- [ ] Press Play, look for "✓ Fisherman Animation System: Ready!"

### Problem: Hat not showing
**Solution:**
- [ ] Create Resources/Cosmetics/Hats/ folder
- [ ] Put hat prefabs there
- [ ] Call `ApplyHatCosmetic("hatname")` after scene loads

### Problem: Hat covers face
**Solution:**
- Add this to FishermanCosmeticSystem:
```csharp
if (hatRenderer != null)
{
    hatRenderer.sortingOrder = 99;  // Increase if needed
}
```

### Problem: Animation speed wrong
**Solution:**
- Adjust Frame Rate in FishermanAnimationSystem:
  - Faster: `0.05`
  - Slower: `0.15`
  - Default: `0.1`

---

## Hierarchy Check

After fixes, your hierarchy should look like:

```
FisherMan (root) ← FishermanAnimationSystem + FishermanCosmeticSystem
├─ head ← SpriteRenderer DISABLED ❌
│  └─ [Hat will be instantiated here]
├─ chest ← SpriteRenderer ENABLED ✓
├─ boat ← SpriteRenderer ENABLED ✓
├─ right hand ← SpriteRenderer ENABLED ✓
├─ left hand ← SpriteRenderer ENABLED ✓
├─ oar ← SpriteRenderer ENABLED ✓
├─ right road ← SpriteRenderer ENABLED ✓
└─ left road ← SpriteRenderer ENABLED ✓
```

---

## Animation States Working

After fixes, these should work:
- ✓ Idle (standing still)
- ✓ Move Forward (rowing animation)
- ✓ Move Backward (reverse rowing)
- ✓ Casting (throwing line)
- ✓ Fishing (holding tension)
- ✓ Fighting (struggling)
- ✓ Crying (lost game)
- ✓ Win (caught enough)
- ✓ Hat Cosmetic (displayed on head)

---

## File Summary

| File | Purpose |
|------|---------|
| FishermanAnimationSystem.cs | NEW - Main animation controller |
| FishermanCosmeticSystem.cs | NEW - Hat cosmetic system |
| FishermanAnimationManager.cs | OLD - Can DELETE |
| FishermanAnimationIntegration.cs | OLD - Can DELETE |
| FishermanSpriteSheetParser.cs | OLD - Can DELETE |

---

**Total Time: ~15 minutes**
**Result: Smooth animations + hat cosmetics working together** ✓
