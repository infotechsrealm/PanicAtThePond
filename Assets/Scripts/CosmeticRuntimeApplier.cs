using UnityEngine;

public class CosmeticRuntimeApplier : MonoBehaviour
{
    public const string SelectedFishHatPrefKey = "SelectedFishHatCosmetic";
    public const string SelectedFishermanHatPrefKey = "SelectedFishermanHatCosmetic";
    public const string SelectedFishermanHairPrefKey = "SelectedFishermanHairCosmetic";

    private const string FishHatChildName = "Applied Fish Hat Cosmetic";
    private const string FishermanHatChildName = "Applied Fisherman Hat Cosmetic";
    private const string FishermanHairChildName = "Applied Fisherman Hair Cosmetic";
    private const string ShopSpritesResourcePath = "ShopUI";

    private static Sprite selectedFishHat;
    private static Sprite selectedFishermanHat;
    private static Sprite selectedFishermanHair;
    private static Sprite[] cachedShopSprites;

    private SpriteRenderer rootRenderer;
    private SpriteRenderer cosmeticRenderer;
    private Animator rootAnimator;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalRotation;
    private Vector3 baseLocalScale;
    private bool followsFishermanAnimation;

    private struct CosmeticTransform
    {
        public readonly Vector3 Position;
        public readonly Vector3 Rotation;
        public readonly Vector3 Scale;

        public CosmeticTransform(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
    }

    public static void SelectFishHat(Sprite sprite)
    {
        selectedFishHat = sprite;
        SaveSelectedSpriteName(SelectedFishHatPrefKey, sprite);
    }

    public static void SelectFishermanHat(Sprite sprite)
    {
        selectedFishermanHat = sprite;
        selectedFishermanHair = null;
        SaveSelectedSpriteName(SelectedFishermanHatPrefKey, sprite);
        SaveSelectedSpriteName(SelectedFishermanHairPrefKey, null);
    }

    public static void SelectFishermanHair(Sprite sprite)
    {
        selectedFishermanHair = sprite;
        selectedFishermanHat = null;
        SaveSelectedSpriteName(SelectedFishermanHairPrefKey, sprite);
        SaveSelectedSpriteName(SelectedFishermanHatPrefKey, null);
    }

    public static void ApplyToFish(GameObject fish)
    {
        EnsureSelectionsLoaded();

        if (fish == null || selectedFishHat == null)
        {
            return;
        }

        CosmeticTransform cosmeticTransform = GetFishHatTransform(fish, selectedFishHat);
        CreateOrUpdateCosmetic(fish, FishHatChildName, selectedFishHat, cosmeticTransform.Position, cosmeticTransform.Rotation, cosmeticTransform.Scale, 2, false);
    }

    public static void ApplyToFisherman(GameObject fisherman)
    {
        EnsureSelectionsLoaded();

        if (fisherman == null)
        {
            return;
        }

        if (selectedFishermanHair != null)
        {
            RemoveCosmetic(fisherman, FishermanHatChildName);
            CosmeticTransform hairTransform = GetFishermanHairTransform(selectedFishermanHair);
            CreateOrUpdateCosmetic(fisherman, FishermanHairChildName, selectedFishermanHair, hairTransform.Position, hairTransform.Rotation, hairTransform.Scale, 3, true);
            return;
        }

        if (selectedFishermanHat != null)
        {
            RemoveCosmetic(fisherman, FishermanHairChildName);
            CosmeticTransform hatTransform = GetFishermanHatTransform(selectedFishermanHat);
            CreateOrUpdateCosmetic(fisherman, FishermanHatChildName, selectedFishermanHat, hatTransform.Position, hatTransform.Rotation, hatTransform.Scale, 3, true);
        }
    }

    private static void CreateOrUpdateCosmetic(GameObject owner, string childName, Sprite sprite, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, int sortingOffset, bool followsFishermanAnimation)
    {
        Transform cosmetic = FindDirectChild(owner.transform, childName);
        if (cosmetic == null)
        {
            cosmetic = new GameObject(childName).transform;
            cosmetic.SetParent(owner.transform, false);
        }

        cosmetic.localPosition = localPosition;
        cosmetic.localEulerAngles = localEulerAngles;
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
        applier.baseLocalRotation = localEulerAngles;
        applier.baseLocalScale = localScale;
        applier.followsFishermanAnimation = followsFishermanAnimation;
    }

    private void LateUpdate()
    {
        if (rootRenderer == null || cosmeticRenderer == null)
        {
            return;
        }

        if (followsFishermanAnimation)
        {
            cosmeticRenderer.flipY = rootRenderer.flipY;
            ApplyFishermanAnimationOffset();
        }
        else if (gameObject.name == FishHatChildName)
        {
            ApplyFishAnimationOffset();
        }
        else
        {
            cosmeticRenderer.flipX = rootRenderer.flipX;
            cosmeticRenderer.flipY = rootRenderer.flipY;
        }
    }

    private void ApplyFishAnimationOffset()
    {
        string clipName = GetCurrentClipName();
        string state = string.IsNullOrEmpty(clipName) ? string.Empty : clipName.ToLowerInvariant();
        int frameIndex = GetCurrentSpriteFrameIndex();
        Vector3 targetPos = baseLocalPosition;
        Vector3 targetRot = baseLocalRotation;

        bool isDead = state.Contains("dead") || (rootAnimator != null && rootAnimator.GetBool("isDead"));

        if (isDead)
        {
            targetPos = new Vector3(-0.05f, -0.29f, -0.01f);
            targetRot = new Vector3(180f, 0f, 0f);
        }
        else
        {
            targetPos += GetFishHeadBobOffset(frameIndex);
        }

        transform.localPosition = targetPos;
        transform.localEulerAngles = targetRot;
        transform.localScale = baseLocalScale;
        
        cosmeticRenderer.flipX = rootRenderer.flipX;
        cosmeticRenderer.flipY = rootRenderer.flipY;
    }

    private void ApplyFishermanAnimationOffset()
    {
        string clipName = GetCurrentClipName();
        string state = string.IsNullOrEmpty(clipName) ? string.Empty : clipName.ToLowerInvariant();
        int frameIndex = GetCurrentSpriteFrameIndex();
        bool isLeft = state.Contains("left") || (rootRenderer != null && rootRenderer.flipX && !state.Contains("right"));

        if (gameObject.name == FishermanHatChildName || gameObject.name == FishermanHairChildName)
        {
            transform.localPosition = baseLocalPosition + GetFishermanHeadBobOffset(state, frameIndex);
            transform.localEulerAngles = isLeft
                ? new Vector3(baseLocalRotation.x, 0f, baseLocalRotation.z)
                : baseLocalRotation;
            transform.localScale = baseLocalScale;
            cosmeticRenderer.flipX = false;
        }
        else
        {
            Vector3 offset = GetFishermanHeadOffset(state, frameIndex);
            transform.localPosition = baseLocalPosition + offset;
            transform.localScale = baseLocalScale;
            cosmeticRenderer.flipX = rootRenderer.flipX;
        }
    }

    private static CosmeticTransform GetFishHatTransform(GameObject fish, Sprite sprite)
    {
        if (fish == null || sprite == null)
        {
            return new CosmeticTransform(
                new Vector3(0f, 0.28f, -0.01f),
                Vector3.zero,
                Vector3.one * 2.7f);
        }

        return IsTroutFish(fish)
            ? GetTroutFishHatTransform(sprite)
            : GetDefaultFishHatTransform(sprite);
    }

    private static CosmeticTransform GetDefaultFishHatTransform(Sprite sprite)
    {
        string name = NormalizeSpriteName(sprite);
        switch (name)
        {
            case "fishermanhatdefaultfishinghat":
                return new CosmeticTransform(
                    new Vector3(-0.005f, 0.27f, -0.01f),
                    new Vector3(0f, 0f, -6f),
                    new Vector3(2.05f, 1.95f, 2.05f));
            case "hat":
                return new CosmeticTransform(
                    new Vector3(0.02f, 0.26f, -0.01f),
                    new Vector3(0f, 168f, -18f),
                    Vector3.one * 2.15f);
            case "hat2":
                return new CosmeticTransform(
                    new Vector3(-0.055f, 0.285f, -0.01f),
                    new Vector3(0f, 0f, 5f),
                    new Vector3(2.15f, 1.95f, 2.15f));
            case "cap":
                return new CosmeticTransform(
                    new Vector3(-0.055f, 0.255f, -0.01f),
                    new Vector3(0f, 0f, -15f),
                    Vector3.one * 2.2f);
            case "paperboat":
                return new CosmeticTransform(
                    new Vector3(-0.01f, 0.29f, -0.01f),
                    new Vector3(0f, 0f, -15f),
                    Vector3.one * 1.9f);
            default:
                return new CosmeticTransform(
                    new Vector3(0f, 0.27f, -0.01f),
                    Vector3.zero,
                    Vector3.one * 2f);
        }
    }

    private static CosmeticTransform GetTroutFishHatTransform(Sprite sprite)
    {
        string name = NormalizeSpriteName(sprite);
        switch (name)
        {
            case "fishermanhatdefaultfishinghat":
                return new CosmeticTransform(
                    new Vector3(-0.027f, 0.12f, -0.01f),
                    new Vector3(0f, 0f, 2.321f),
                    new Vector3(1.221683f, 1.201655f, 1.602207f));
            case "hat":
                return new CosmeticTransform(
                    new Vector3(0.002f, 0.135f, -0.01f),
                    new Vector3(0f, 168f, -21.54f),
                    Vector3.one * 1.339713f);
            case "hat2":
                return new CosmeticTransform(
                    new Vector3(-0.08999f, 0.164f, -0.01f),
                    new Vector3(0f, 0f, 6.591f),
                    new Vector3(1.255108f, 1.145286f, 1.255108f));
            case "cap":
                return new CosmeticTransform(
                    new Vector3(-0.03f, 0.116f, -0.01f),
                    new Vector3(0f, 0f, -15f),
                    Vector3.one * 1.435158f);
            case "paperboat":
                return new CosmeticTransform(
                    new Vector3(0f, 0.15f, -0.01f),
                    new Vector3(0f, 0f, -15f),
                    Vector3.one * 1.3f);
            default:
                return new CosmeticTransform(
                    new Vector3(0f, 0.15f, -0.01f),
                    Vector3.zero,
                    Vector3.one * 1.35f);
        }
    }

    private static CosmeticTransform GetFishermanHatTransform(Sprite sprite)
    {
        string name = NormalizeSpriteName(sprite);
        switch (name)
        {
            case "fishermanhatbluecap":
                return new CosmeticTransform(
                    new Vector3(-0.005f, 0.67f, 0f),
                    new Vector3(0f, -168f, -1.767f),
                    new Vector3(5.137857f, 4.707734f, 3.9f));
            case "fishermanhatredcap":
                return new CosmeticTransform(
                    new Vector3(0.04f, 0.7f, -0.01f),
                    new Vector3(0f, -168f, 2.5f),
                    new Vector3(4.668904f, 4.27908f, 4.27908f));
            case "fishermanhatchefhat":
                return new CosmeticTransform(
                    new Vector3(0f, 0.78f, 0f),
                    new Vector3(0f, -168f, 20.4f),
                    Vector3.one * 6.25f);
            case "fishermanhatsodahat":
                return new CosmeticTransform(
                    new Vector3(-0.005f, 0.73f, 0f),
                    new Vector3(0f, -168f, 2.5f),
                    Vector3.one * 4f);
            default:
                return new CosmeticTransform(
                    new Vector3(-0.005f, 0.77f, 0f),
                    new Vector3(0f, -168f, 2.5f),
                    Vector3.one * 3.9f);
        }
    }

    private static CosmeticTransform GetFishermanHairTransform(Sprite sprite)
    {
        return new CosmeticTransform(
            new Vector3(0.04f, -0.2f, 0f),
            new Vector3(0f, 0f, 2.5f),
            new Vector3(4.86f, 5.72f, 10.99f));
    }

    private static bool IsTroutFish(GameObject fish)
    {
        string objectName = fish.name.ToLowerInvariant();
        if (objectName.Contains("fish 2") || objectName.Contains("trout"))
        {
            return true;
        }

        SpriteRenderer renderer = fish.GetComponent<SpriteRenderer>();
        return renderer != null
            && renderer.sprite != null
            && renderer.sprite.name.ToLowerInvariant().Contains("trout");
    }

    private static string NormalizeSpriteName(Sprite sprite)
    {
        if (sprite == null)
        {
            return string.Empty;
        }

        string name = sprite.name.ToLowerInvariant();
        if (name.EndsWith("_0"))
        {
            name = name.Substring(0, name.Length - 2);
        }

        return name
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private static Vector3 GetFishHeadBobOffset(int frameIndex)
    {
        switch (frameIndex)
        {
            case 1:
                return new Vector3(0f, 0.018f, 0f);
            case 2:
                return new Vector3(0.006f, 0.026f, 0f);
            case 3:
                return new Vector3(0f, 0.01f, 0f);
            default:
                return Vector3.zero;
        }
    }

    private static Vector3 GetFishermanHeadBobOffset(string clipName, int frameIndex)
    {
        Vector3 offset = GetFishermanHeadOffset(clipName, frameIndex);
        offset.x *= 0.35f;
        return offset;
    }

    private static void EnsureSelectionsLoaded()
    {
        if (selectedFishHat == null)
        {
            selectedFishHat = LoadSelectedSprite(SelectedFishHatPrefKey);
        }

        if (selectedFishermanHair == null)
        {
            selectedFishermanHair = LoadSelectedSprite(SelectedFishermanHairPrefKey);
        }

        if (selectedFishermanHair == null && selectedFishermanHat == null)
        {
            selectedFishermanHat = LoadSelectedSprite(SelectedFishermanHatPrefKey);
        }
    }

    private static Sprite LoadSelectedSprite(string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            return null;
        }

        string selectedSpriteName = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(selectedSpriteName))
        {
            return null;
        }

        if (cachedShopSprites == null || cachedShopSprites.Length == 0)
        {
            cachedShopSprites = Resources.LoadAll<Sprite>(ShopSpritesResourcePath);
        }

        string normalizedSelectedName = NormalizeSpriteName(selectedSpriteName);
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && NormalizeSpriteName(sprite.name) == normalizedSelectedName)
            {
                return sprite;
            }
        }

        return null;
    }

    private static string NormalizeSpriteName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return string.Empty;
        }

        string name = spriteName.ToLowerInvariant();
        if (name.EndsWith("_0"))
        {
            name = name.Substring(0, name.Length - 2);
        }

        return name
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private static void RemoveCosmetic(GameObject owner, string childName)
    {
        Transform cosmetic = owner != null ? FindDirectChild(owner.transform, childName) : null;
        if (cosmetic == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(cosmetic.gameObject);
        }
        else
        {
            DestroyImmediate(cosmetic.gameObject);
        }
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
