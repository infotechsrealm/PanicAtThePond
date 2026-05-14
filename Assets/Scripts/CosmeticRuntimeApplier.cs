using UnityEngine;

public class CosmeticRuntimeApplier : MonoBehaviour
{
    public const string SelectedFishHatPrefKey = "SelectedFishHatCosmetic";
    public const string SelectedFishermanHatPrefKey = "SelectedFishermanHatCosmetic";
    public const string SelectedFishermanHairPrefKey = "SelectedFishermanHairCosmetic";

    private const string FishHatChildName = "Applied Fish Hat Cosmetic";
    private const string FishermanHatChildName = "Applied Fisherman Hat Cosmetic";
    private const string FishermanHairChildName = "Applied Fisherman Hair Cosmetic";

    private static Sprite selectedFishHat;
    private static Sprite selectedFishermanHat;
    private static Sprite selectedFishermanHair;

    private SpriteRenderer rootRenderer;
    private SpriteRenderer cosmeticRenderer;
    private Animator rootAnimator;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private bool followsFishermanAnimation;

    public static void SelectFishHat(Sprite sprite)
    {
        selectedFishHat = sprite;
        SaveSelectedSpriteName(SelectedFishHatPrefKey, sprite);
    }

    public static void SelectFishermanHat(Sprite sprite)
    {
        selectedFishermanHat = sprite;
        SaveSelectedSpriteName(SelectedFishermanHatPrefKey, sprite);
    }

    public static void SelectFishermanHair(Sprite sprite)
    {
        selectedFishermanHair = sprite;
        SaveSelectedSpriteName(SelectedFishermanHairPrefKey, sprite);
    }

    public static void ApplyToFish(GameObject fish)
    {
        if (fish == null || selectedFishHat == null)
        {
            return;
        }

        CreateOrUpdateCosmetic(fish, FishHatChildName, selectedFishHat, new Vector3(0f, 0.28f, -0.01f), Vector3.one * 0.45f, 2, false);
    }

    public static void ApplyToFisherman(GameObject fisherman)
    {
        if (fisherman == null)
        {
            return;
        }

        if (selectedFishermanHair != null)
        {
            CreateOrUpdateCosmetic(fisherman, FishermanHairChildName, selectedFishermanHair, new Vector3(0f, 0.66f, -0.01f), Vector3.one * 1.45f, 2, true);
        }

        if (selectedFishermanHat != null)
        {
            CreateOrUpdateCosmetic(fisherman, FishermanHatChildName, selectedFishermanHat, new Vector3(0f, 0.82f, -0.02f), Vector3.one * 3.5f, 3, true);
        }
    }

    private static void CreateOrUpdateCosmetic(GameObject owner, string childName, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOffset, bool followsFishermanAnimation)
    {
        Transform cosmetic = FindDirectChild(owner.transform, childName);
        if (cosmetic == null)
        {
            cosmetic = new GameObject(childName).transform;
            cosmetic.SetParent(owner.transform, false);
        }

        cosmetic.localPosition = localPosition;
        cosmetic.localRotation = Quaternion.identity;
        cosmetic.localScale = localScale;

        SpriteRenderer renderer = cosmetic.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = cosmetic.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;

        SpriteRenderer ownerRenderer = owner.GetComponent<SpriteRenderer>();
        if (ownerRenderer != null)
        {
            renderer.sortingLayerID = ownerRenderer.sortingLayerID;
            renderer.sortingOrder = ownerRenderer.sortingOrder + sortingOffset;
            renderer.flipX = ownerRenderer.flipX;
            renderer.flipY = ownerRenderer.flipY;
        }

        CosmeticRuntimeApplier applier = cosmetic.GetComponent<CosmeticRuntimeApplier>();
        if (applier == null)
        {
            applier = cosmetic.gameObject.AddComponent<CosmeticRuntimeApplier>();
        }

        applier.rootRenderer = ownerRenderer;
        applier.cosmeticRenderer = renderer;
        applier.rootAnimator = owner.GetComponent<Animator>();
        applier.baseLocalPosition = localPosition;
        applier.baseLocalScale = localScale;
        applier.followsFishermanAnimation = followsFishermanAnimation;
    }

    private void LateUpdate()
    {
        if (rootRenderer == null || cosmeticRenderer == null)
        {
            return;
        }

        cosmeticRenderer.flipX = rootRenderer.flipX;
        cosmeticRenderer.flipY = rootRenderer.flipY;

        if (followsFishermanAnimation)
        {
            ApplyFishermanAnimationOffset();
        }
    }

    private void ApplyFishermanAnimationOffset()
    {
        string clipName = GetCurrentClipName();
        int frameIndex = GetCurrentSpriteFrameIndex();
        Vector3 offset = GetFishermanHeadOffset(clipName, frameIndex);

        transform.localPosition = baseLocalPosition + offset;
        transform.localScale = baseLocalScale;
    }

    private string GetCurrentClipName()
    {
        if (rootAnimator == null)
        {
            return string.Empty;
        }

        AnimatorClipInfo[] clips = rootAnimator.GetCurrentAnimatorClipInfo(0);
        return clips != null && clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : string.Empty;
    }

    private int GetCurrentSpriteFrameIndex()
    {
        if (rootRenderer == null || rootRenderer.sprite == null)
        {
            return 0;
        }

        return Mathf.Abs(rootRenderer.sprite.name.GetHashCode()) % 4;
    }

    private static Vector3 GetFishermanHeadOffset(string clipName, int frameIndex)
    {
        string state = string.IsNullOrEmpty(clipName) ? string.Empty : clipName.ToLowerInvariant();
        float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;

        if (state.Contains("left"))
        {
            return new Vector3(-0.05f, bob, 0f);
        }

        if (state.Contains("right"))
        {
            return new Vector3(0.05f, bob, 0f);
        }

        if (state.Contains("move"))
        {
            return new Vector3(0f, bob + 0.03f, 0f);
        }

        if (state.Contains("cast") || state.Contains("fish") || state.Contains("reel") || state.Contains("fight"))
        {
            return new Vector3(0f, bob - 0.02f, 0f);
        }

        return new Vector3(0f, bob, 0f);
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static void SaveSelectedSpriteName(string key, Sprite sprite)
    {
        if (sprite == null)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetString(key, sprite.name);
        PlayerPrefs.Save();
    }
}
