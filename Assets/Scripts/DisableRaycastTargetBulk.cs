using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisableRaycastTargetBulk : MonoBehaviour
{
    // === METHOD 1: DISABLE ALL GRAPHICS IN SCENE ===
    public void DisableRaycastOnAllGraphics()
    {
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (Graphic g in allGraphics)
        {
            if (g.raycastTarget)
            {
                g.raycastTarget = false;
                disabledCount++;
                Debug.Log($"✓ Disabled Raycast Target on: {g.gameObject.name}");
            }
        }

        Debug.Log($"\n=== DISABLED {disabledCount} RAYCAST TARGETS ===");
    }

    // === METHOD 2: DISABLE RAYCAST ON SPECIFIC OBJECT NAMES ===
    public void DisableRaycastOnObjectsByName(string[] objectNames)
    {
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (Graphic g in allGraphics)
        {
            foreach (string name in objectNames)
            {
                if (g.gameObject.name == name && g.raycastTarget)
                {
                    g.raycastTarget = false;
                    disabledCount++;
                    Debug.Log($"✓ Disabled Raycast Target on: {g.gameObject.name}");
                    break;
                }
            }
        }

        Debug.Log($"\n=== DISABLED {disabledCount} RAYCAST TARGETS ===");
    }

    // === METHOD 3: DISABLE RAYCAST ON TEXT MESH PRO ONLY ===
    public void DisableRaycastOnTMPTexts()
    {
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text.raycastTarget)
            {
                text.raycastTarget = false;
                disabledCount++;
                Debug.Log($"✓ Disabled Raycast Target on TMP: {text.gameObject.name}");
            }
        }

        Debug.Log($"\n=== DISABLED {disabledCount} TMP RAYCAST TARGETS ===");
    }

    // === METHOD 4: DISABLE RAYCAST ON IMAGES (EXCEPT BUTTONS) ===
    public void DisableRaycastOnImages()
    {
        Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (Image img in allImages)
        {
            // Don't disable on buttons
            if (img.GetComponent<Button>() != null) continue;

            if (img.raycastTarget)
            {
                img.raycastTarget = false;
                disabledCount++;
                Debug.Log($"✓ Disabled Raycast Target on Image: {img.gameObject.name}");
            }
        }

        Debug.Log($"\n=== DISABLED {disabledCount} IMAGE RAYCAST TARGETS ===");
    }

    // === METHOD 5: SMART FIX - DISABLE RAYCAST EXCEPT INPUTS & BUTTONS ===
    public void SmartDisableRaycast()
    {
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (Graphic g in allGraphics)
        {
            // KEEP Raycast Target ON for these types
            if (g.GetComponent<Button>() != null) continue; // Buttons
            if (g.GetComponent<TMP_InputField>() != null) continue; // Input fields
            if (g.GetComponent<InputField>() != null) continue; // Standard input fields
            if (g.GetComponent<Toggle>() != null) continue; // Toggles
            if (g.GetComponent<Slider>() != null) continue; // Sliders

            // DISABLE Raycast Target on everything else
            if (g.raycastTarget)
            {
                g.raycastTarget = false;
                disabledCount++;
                Debug.Log($"✓ Disabled Raycast Target on: {g.gameObject.name} ({g.GetType().Name})");
            }
        }

        Debug.Log($"\n=== SMART FIX COMPLETE: DISABLED {disabledCount} RAYCAST TARGETS ===");
        Debug.Log($"Kept Raycast ON for: Buttons, InputFields, Toggles, Sliders");
    }

    // === METHOD 6: DISABLE RAYCAST ON CHILDREN OF SPECIFIC PARENT ===
    public void DisableRaycastOnChildrenOf(Transform parent)
    {
        Graphic[] childGraphics = parent.GetComponentsInChildren<Graphic>();
        int disabledCount = 0;

        foreach (Graphic g in childGraphics)
        {
            // Don't disable on input field itself
            if (g.GetComponent<TMP_InputField>() != null) continue;

            if (g.raycastTarget)
            {
                g.raycastTarget = false;
                disabledCount++;
                Debug.Log($"✓ Disabled Raycast Target on: {g.gameObject.name}");
            }
        }

        Debug.Log($"\n=== DISABLED {disabledCount} RAYCAST TARGETS IN {parent.name} ===");
    }

    // === QUICK FIXES FOR YOUR SPECIFIC CASE ===

    // Fix 1: Disable on these specific objects from your debug output
    public void FixFishermanInputField()
    {
        string[] objectsToDisable = {
            "Fisherman",
            "Text",
            "Placeholder",
            "Caret",
            "Water Drops",
            "icon",
            "BG"
        };

        Debug.Log("Fixing Fisherman InputField raycast blocking...");
        DisableRaycastOnObjectsByName(objectsToDisable);
    }

    // Fix 2: Disable ALL text elements raycast
    public void DisableRaycastOnAllTexts()
    {
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text.raycastTarget)
            {
                text.raycastTarget = false;
                disabledCount++;
            }
        }

        Debug.Log($"✓ Disabled Raycast on {disabledCount} TextMeshPro elements");
    }

    // Fix 3: Disable ALL image raycast except buttons
    public void DisableRaycastOnAllNonButtonImages()
    {
        Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
        int disabledCount = 0;

        foreach (Image img in allImages)
        {
            if (img.GetComponent<Button>() == null && img.raycastTarget)
            {
                img.raycastTarget = false;
                disabledCount++;
            }
        }

        Debug.Log($"✓ Disabled Raycast on {disabledCount} non-button Images");
    }
}