using UnityEngine;

/// <summary>
/// Handles hat cosmetic display on the fisherman's head
/// Works with the animation system to keep hat visible during all animations
/// </summary>
public class FishermanHatSystem : MonoBehaviour
{
    [SerializeField] private Transform headTransform;
    [SerializeField] private int hatSortingOrder = 100; // Above all other sprites

    private GameObject currentHatInstance;
    private SpriteRenderer currentHatRenderer;

    private void Start()
    {
        Debug.Log("🎩 [FISHERMAN HAT SYSTEM] Initializing...");

        // Find head transform
        if (headTransform == null)
        {
            headTransform = transform.Find("head");
            if (headTransform == null)
            {
                Debug.LogError("❌ Could not find 'head' child object!");
                return;
            }
        }

        // Disable the original head sprite renderer to avoid duplication
        SpriteRenderer headRenderer = headTransform.GetComponent<SpriteRenderer>();
        if (headRenderer != null)
        {
            headRenderer.enabled = false;
            Debug.Log("✓ Disabled original head sprite renderer");
        }

        // Load and apply saved hat cosmetic
        ApplySavedCosmetic();

        Debug.Log("✅ [FISHERMAN HAT SYSTEM] Ready!");
    }

    /// <summary>
    /// Apply a hat cosmetic by name
    /// </summary>
    public void ApplyHat(string hatName)
    {
        if (headTransform == null)
        {
            Debug.LogError("❌ Head transform not found!");
            return;
        }

        // Remove current hat
        if (currentHatInstance != null)
        {
            Destroy(currentHatInstance);
            currentHatInstance = null;
            currentHatRenderer = null;
        }

        if (string.IsNullOrEmpty(hatName) || hatName == "None")
        {
            Debug.Log("🎩 Hat removed");
            return;
        }

        // Load hat prefab from Resources
        string hatPath = $"Cosmetics/Hats/{hatName}";
        GameObject hatPrefab = Resources.Load<GameObject>(hatPath);

        if (hatPrefab == null)
        {
            Debug.LogWarning($"⚠ Hat prefab not found at: {hatPath}");
            return;
        }

        // Instantiate hat as child of head
        currentHatInstance = Instantiate(hatPrefab, headTransform);
        currentHatInstance.name = hatName;

        // Ensure hat renders on top
        currentHatRenderer = currentHatInstance.GetComponent<SpriteRenderer>();
        if (currentHatRenderer != null)
        {
            currentHatRenderer.sortingOrder = hatSortingOrder;
        }

        // Disable physics if present
        Collider collider = currentHatInstance.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        Rigidbody rb = currentHatInstance.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Debug.Log($"✅ Applied hat: {hatName}");
    }

    /// <summary>
    /// Load and apply the local player's saved cosmetic
    /// </summary>
    private void ApplySavedCosmetic()
    {
        string savedHat = CosmeticRuntimeApplier.GetSelectedFishermanHatName();

        if (!string.IsNullOrEmpty(savedHat))
        {
            Debug.Log($"🔄 Applying saved hat: {savedHat}");
            ApplyHat(savedHat);
        }
        else
        {
            Debug.Log("ℹ No saved hat cosmetic");
        }
    }

    /// <summary>
    /// Remove current hat
    /// </summary>
    public void RemoveHat()
    {
        if (currentHatInstance != null)
        {
            Destroy(currentHatInstance);
            currentHatInstance = null;
            currentHatRenderer = null;
            Debug.Log("🎩 Hat removed");
        }
    }

    /// <summary>
    /// Get the current hat name
    /// </summary>
    public string GetCurrentHat()
    {
        return currentHatInstance != null ? currentHatInstance.name : "";
    }

    /// <summary>
    /// Update hat sorting order (in case other objects need to layer above hat)
    /// </summary>
    public void SetHatSortingOrder(int order)
    {
        hatSortingOrder = order;
        if (currentHatRenderer != null)
        {
            currentHatRenderer.sortingOrder = order;
        }
    }
}
