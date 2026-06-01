using UnityEngine;

/// <summary>
/// Verify that the Fisherman Animation System is set up correctly
/// Run this in Play mode to check for common issues
/// </summary>
public class FishermanAnimationVerifier : MonoBehaviour
{
    public bool VerifySetup()
    {
        Debug.Log("\n========== FISHERMAN SETUP VERIFICATION ==========\n");

        bool allGood = true;

        // Check 1: FishermanAnimationController
        FishermanAnimationController animController = GetComponent<FishermanAnimationController>();
        if (animController != null)
        {
            Debug.Log("✅ FishermanAnimationController found");
        }
        else
        {
            Debug.LogError("❌ FishermanAnimationController NOT found");
            allGood = false;
        }

        // Check 2: FishermanHatSystem
        FishermanHatSystem hatSystem = GetComponent<FishermanHatSystem>();
        if (hatSystem != null)
        {
            Debug.Log("✅ FishermanHatSystem found");
        }
        else
        {
            Debug.LogError("❌ FishermanHatSystem NOT found");
            allGood = false;
        }

        // Check 3: Sprite renderers
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"ℹ Found {renderers.Length} sprite renderers");

        // Check 4: Head sprite renderer disabled
        Transform headTransform = transform.Find("head");
        if (headTransform != null)
        {
            SpriteRenderer headRenderer = headTransform.GetComponent<SpriteRenderer>();
            if (headRenderer != null && !headRenderer.enabled)
            {
                Debug.Log("✅ Head sprite renderer is DISABLED (correct)");
            }
            else
            {
                Debug.LogWarning("⚠ Head sprite renderer is ENABLED (should be disabled to avoid duplicate head)");
                allGood = false;
            }
        }

        // Check 5: Sprite sheets exist
        string[] sheetNames = { "GreenBody", "Arms", "Boat", "Rods" };
        foreach (string name in sheetNames)
        {
            #if UNITY_EDITOR
            UnityEngine.Texture2D sheet = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(
                $"Assets/Animations/Fisher Man Animations/Sprite Sheets/FishermansAnimations-{name}_Sheet.png"
            );
            if (sheet != null)
            {
                Debug.Log($"✅ Sprite sheet found: {name}");
            }
            else
            {
                Debug.LogWarning($"⚠ Sprite sheet NOT found: {name}");
            }
            #endif
        }

        Debug.Log("\n================================================\n");

        if (allGood)
        {
            Debug.Log("✅ SETUP LOOKS GOOD! All checks passed.");
        }
        else
        {
            Debug.LogError("❌ SETUP HAS ISSUES! Check warnings above.");
        }

        return allGood;
    }
}
