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
    private const string FishermanAnimatedHeadSheetName = "FishermansAnimations-Head_Sheet";

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
    private bool usesAnimatedFishermanHeadReplacement;
    private Sprite[] animatedFishermanHeadSprites;

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
        if (sprite != null && IsPreviewSprite(sprite.name))
        {
            Sprite cleanSprite = GetSpriteByName(sprite.name);
            if (cleanSprite != null)
            {
                sprite = cleanSprite;
            }
        }
        selectedFishHat = sprite;
        SaveSelectedSpriteName(SelectedFishHatPrefKey, sprite);
    }

    public static Sprite GetSelectedFishHat()
    {
        EnsureSelectionsLoaded();
        return selectedFishHat;
    }

    public static void SelectFishermanHat(Sprite sprite)
    {
        if (sprite != null && IsPreviewSprite(sprite.name))
        {
            Sprite cleanSprite = GetSpriteByName(sprite.name);
            if (cleanSprite != null)
            {
                sprite = cleanSprite;
            }
        }
        selectedFishermanHat = sprite;
        selectedFishermanHair = null;
        SaveSelectedSpriteName(SelectedFishermanHatPrefKey, sprite);
        SaveSelectedSpriteName(SelectedFishermanHairPrefKey, null);
    }

    public static Sprite GetSelectedFishermanHat()
    {
        EnsureSelectionsLoaded();
        return selectedFishermanHat;
    }

    public static void SelectFishermanHair(Sprite sprite)
    {
        if (sprite != null && IsPreviewSprite(sprite.name))
        {
            Sprite cleanSprite = GetSpriteByName(sprite.name);
            if (cleanSprite != null)
            {
                sprite = cleanSprite;
            }
        }
        selectedFishermanHair = sprite;
        selectedFishermanHat = null;
        SaveSelectedSpriteName(SelectedFishermanHairPrefKey, sprite);
        SaveSelectedSpriteName(SelectedFishermanHatPrefKey, null);
    }

    public static Sprite GetSelectedFishermanHair()
    {
        EnsureSelectionsLoaded();
        return selectedFishermanHair;
    }

    public static void ApplyToFish(GameObject fish)
    {
        EnsureSelectionsLoaded();

        if (fish == null)
        {
            return;
        }

        Animator anim = fish.GetComponent<Animator>();
        RemoveCosmetic(fish, FishHatChildName);

        if (selectedFishHat == null)
        {
            if (anim != null)
            {
                if (IsBassFish(fish))
                    anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("FishControllers/Fish 1 Default");
                else if (IsTroutFish(fish))
                    anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("FishControllers/Fish 2 Default");
            }
            return;
        }

        if (anim != null)
        {
            RuntimeAnimatorController newController = Resources.Load<RuntimeAnimatorController>("FishControllers/" + selectedFishHat.name);
            if (newController != null && anim.runtimeAnimatorController != newController)
            {
                anim.runtimeAnimatorController = newController;
            }
        }
    }

    public static void ApplyToFisherman(GameObject fisherman)
    {
        EnsureSelectionsLoaded();

        if (fisherman == null)
        {
            return;
        }

        // --- NEW MODULAR FISHERMAN LOGIC ---
        Transform headTransform = fisherman.transform.Find("head"); // Prefab (2) uses lowercase "head"
        if (headTransform == null) headTransform = fisherman.transform.Find("Head");

        if (headTransform != null)
        {
            // Destroy obsolete modular animation components at runtime to prevent logs/warnings and conflicts
            MonoBehaviour[] scripts = fisherman.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null)
                {
                    string typeName = script.GetType().Name;
                    if (typeName == "FishermanAnimationSystem" ||
                        typeName == "FishermanAnimationController" ||
                        typeName == "FishermanAnimationVerifier" ||
                        typeName == "FishermanHatSystem")
                    {
                        if (Application.isPlaying)
                            Destroy(script);
                        else
                            DestroyImmediate(script);
                    }
                }
            }

            // Ensure the root has a SpriteRenderer for the animation to play on!
            SpriteRenderer rootRenderer = fisherman.GetComponent<SpriteRenderer>();
            if (rootRenderer == null)
            {
                rootRenderer = fisherman.AddComponent<SpriteRenderer>();
                rootRenderer.sortingLayerName = "Default"; 
                rootRenderer.sortingOrder = 1; 
            }

            // Let's resolve the controller to play on the root based on selected hat and hair
            string hairName = selectedFishermanHair != null ? selectedFishermanHair.name.ToLowerInvariant() : "";
            string hatName = selectedFishermanHat != null ? selectedFishermanHat.name.ToLowerInvariant() : "";
            RuntimeAnimatorController newController = null;
            
            bool isPreBaked = IsHatPreBaked(hatName);

            if (isPreBaked)
            {
                if (hatName.Contains("yellow") || hatName.Contains("fishing_hat") || hatName.Contains("default")) 
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan Yellow Hat");
                else if (hatName.Contains("backwards_cap") || hatName.Contains("backwards"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Backwards Cap)");
                else if (hatName.Contains("blue_cap") || hatName.Contains("blue cap"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Blue Cap)");
                else if (hatName.Contains("frog") || hatName.Contains("griin"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Frog Hat)");
                else if (hatName.Contains("green_bucket_hat") || (hatName.Contains("green") && !hatName.Contains("pointed") && !hatName.Contains("griin")))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Green Bucket Hat)");
                else if (hatName.Contains("green_pointed_hat") || hatName.Contains("griin"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Green Pointed Hat)");
                else if (hatName.Contains("headphones") || hatName.Contains("headphone"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Headphones)");
                else if (hatName.Contains("silver_bucket_hat") || hatName.Contains("silver"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Silver Bucket Hat)");
                else if (hatName.Contains("straw_hat") || hatName.Contains("straw") || hatName.Contains("white hat"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Straw Hat)");
            }
            
            if (newController == null)
            {
                // Fallback to clean hair controller (no hat pre-baked)
                if (hairName.Contains("black"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Black Hair)");
                else 
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Red Hair)");
            }

            Animator rootAnim = fisherman.GetComponent<Animator>();
            if (rootAnim != null && newController != null)
            {
                if (rootAnim.runtimeAnimatorController != newController)
                {
                    rootAnim.runtimeAnimatorController = newController;
                }
            }

            // Enable root renderer only if pre-baked; otherwise disable to animate child parts
            rootRenderer.enabled = isPreBaked;

            // Find or setup the child hat cosmetic object
            Transform hatCosmeticTransform = headTransform.Find("hat Cosmetic");
            bool enableHatCosmetic = selectedFishermanHat != null && !isPreBaked;

            // Ensure the child head and hat Cosmetic GameObjects are kept active/enabled!
            headTransform.gameObject.SetActive(true);
            if (hatCosmeticTransform != null)
            {
                hatCosmeticTransform.gameObject.SetActive(true);
            }

            // If we are pre-baked, disable child modular renderers; if modular, enable them!
            SpriteRenderer[] allRenderers = fisherman.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in allRenderers)
            {
                if (sr.gameObject == fisherman) continue;
                
                if (hatCosmeticTransform != null && sr.gameObject == hatCosmeticTransform.gameObject)
                {
                    sr.enabled = enableHatCosmetic;
                    continue;
                }
                
                sr.enabled = !isPreBaked;
            }

            // If we have a custom non-prebaked hat, assign its sprite and setup bobbing
            if (enableHatCosmetic && hatCosmeticTransform != null)
            {
                SpriteRenderer hatSR = hatCosmeticTransform.GetComponent<SpriteRenderer>();
                if (hatSR != null)
                {
                    hatSR.sprite = selectedFishermanHat;
                    hatSR.sortingOrder = rootRenderer.sortingOrder + 10;
                }

                CosmeticRuntimeApplier applier = hatCosmeticTransform.GetComponent<CosmeticRuntimeApplier>();
                if (applier == null)
                {
                    applier = hatCosmeticTransform.gameObject.AddComponent<CosmeticRuntimeApplier>();
                }
                applier.rootRenderer = rootRenderer;
                applier.cosmeticRenderer = hatSR;
                applier.rootAnimator = rootAnim;
                applier.baseLocalPosition = new Vector3(-0.029f, 0.075f, -0.9f); // Perfect alignment as requested
                applier.baseLocalRotation = Vector3.zero;
                applier.baseLocalScale = new Vector3(0.73484f, 0.73484f, 0.73484f);
                applier.followsFishermanAnimation = true;
            }

            // Attach the flipper script to flip the root sprite automatically
            if (fisherman.GetComponent<FishermanDirectionFlipper>() == null)
            {
                fisherman.AddComponent<FishermanDirectionFlipper>();
            }

            return;
        }
        // --- END MODULAR LOGIC ---

        Animator anim = fisherman.GetComponent<Animator>();
        if (anim != null) 
        {
            string hairName = selectedFishermanHair != null ? selectedFishermanHair.name.ToLowerInvariant() : "";
            string hatName = selectedFishermanHat != null ? selectedFishermanHat.name.ToLowerInvariant() : "";
            RuntimeAnimatorController newController = null;
            
            if (hatName.Contains("yellow") || hatName.Contains("fishing_hat")) 
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan Yellow hat");
            else if (hatName.Contains("backwards_cap"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Backwards Cap)");
            else if (hatName.Contains("blue_cap"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Blue Cap)");
            else if (hatName.Contains("frog"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Frog Hat)");
            else if (hatName.Contains("green_bucket_hat"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Green Bucket Hat)");
            else if (hatName.Contains("green_pointed_hat"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Green Pointed Hat)");
            else if (hatName.Contains("headphones"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Headphones)");
            else if (hatName.Contains("silver_bucket_hat"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Silver Bucket Hat)");
            else if (hatName.Contains("straw_hat"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Straw Hat)");
            else if (hairName.Contains("black"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Black Hair)");
            else if (hairName.Contains("red"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Red Hair)");
                
            if (newController != null && anim.runtimeAnimatorController != newController)
            {
                anim.runtimeAnimatorController = newController;
            }
        }

        if (selectedFishermanHair != null)
        {
            string hairName = selectedFishermanHair.name.ToLowerInvariant();
            if (hairName.Contains("red") || hairName.Contains("black"))
            {
                RemoveCosmetic(fisherman, FishermanHairChildName);
                RemoveCosmetic(fisherman, FishermanHatChildName);
                return;
            }

            RemoveCosmetic(fisherman, FishermanHatChildName);
            CosmeticTransform hairTransform = GetFishermanHairTransform(selectedFishermanHair);
            CreateOrUpdateCosmetic(fisherman, FishermanHairChildName, selectedFishermanHair, hairTransform.Position, hairTransform.Rotation, hairTransform.Scale, 5, true);
            return;
        }

        if (selectedFishermanHat != null)
        {
            string hatNameCheck = selectedFishermanHat.name.ToLowerInvariant();
            if (hatNameCheck.Contains("yellow") || hatNameCheck.Contains("fishing_hat"))
            {
                RemoveCosmetic(fisherman, FishermanHairChildName);
                RemoveCosmetic(fisherman, FishermanHatChildName);
                return;
            }

            RemoveCosmetic(fisherman, FishermanHairChildName);
            CosmeticTransform hatTransform = GetFishermanHatTransform(selectedFishermanHat);
            CreateOrUpdateCosmetic(fisherman, FishermanHatChildName, selectedFishermanHat, hatTransform.Position, hatTransform.Rotation, hatTransform.Scale, 3, true);
        }
    }

    public static void ApplyFishermanCosmeticsByName(GameObject fisherman, string hatName, string hairName)
    {
        if (fisherman == null) return;

        Sprite hatSprite = GetSpriteByName(hatName);
        Sprite hairSprite = GetSpriteByName(hairName);

        // --- NEW MODULAR FISHERMAN LOGIC ---
        Transform headTransform = fisherman.transform.Find("head");
        if (headTransform == null) headTransform = fisherman.transform.Find("Head");

        if (headTransform != null)
        {
            // Destroy obsolete modular animation components at runtime to prevent logs/warnings and conflicts
            MonoBehaviour[] scripts = fisherman.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null)
                {
                    string typeName = script.GetType().Name;
                    if (typeName == "FishermanAnimationSystem" ||
                        typeName == "FishermanAnimationController" ||
                        typeName == "FishermanAnimationVerifier" ||
                        typeName == "FishermanHatSystem")
                    {
                        if (Application.isPlaying)
                            Destroy(script);
                        else
                            DestroyImmediate(script);
                    }
                }
            }

            // Ensure the root has a SpriteRenderer for the animation to play on!
            SpriteRenderer rootRenderer = fisherman.GetComponent<SpriteRenderer>();
            if (rootRenderer == null)
            {
                rootRenderer = fisherman.AddComponent<SpriteRenderer>();
                rootRenderer.sortingLayerName = "Default"; 
                rootRenderer.sortingOrder = 1; 
            }

            // Let's resolve the controller to play on the root based on selected hat and hair
            string normalizedHairName = hairSprite != null ? hairSprite.name.ToLowerInvariant() : "";
            string normalizedHatName = hatSprite != null ? hatSprite.name.ToLowerInvariant() : "";
            RuntimeAnimatorController newController = null;
            
            bool isPreBaked = IsHatPreBaked(normalizedHatName);

            if (isPreBaked)
            {
                if (normalizedHatName.Contains("yellow") || normalizedHatName.Contains("fishing_hat") || normalizedHatName.Contains("default")) 
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan Yellow Hat");
                else if (normalizedHatName.Contains("backwards_cap") || normalizedHatName.Contains("backwards"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Backwards Cap)");
                else if (normalizedHatName.Contains("blue_cap") || normalizedHatName.Contains("blue cap"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Blue Cap)");
                else if (normalizedHatName.Contains("frog") || normalizedHatName.Contains("griin"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Frog Hat)");
                else if (normalizedHatName.Contains("green_bucket_hat") || (normalizedHatName.Contains("green") && !normalizedHatName.Contains("pointed") && !normalizedHatName.Contains("griin")))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Green Bucket Hat)");
                else if (normalizedHatName.Contains("green_pointed_hat") || normalizedHatName.Contains("griin"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Green Pointed Hat)");
                else if (normalizedHatName.Contains("headphones") || normalizedHatName.Contains("headphone"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Headphones)");
                else if (normalizedHatName.Contains("silver_bucket_hat") || normalizedHatName.Contains("silver"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Silver Bucket Hat)");
                else if (normalizedHatName.Contains("straw_hat") || normalizedHatName.Contains("straw") || normalizedHatName.Contains("white hat"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Straw Hat)");
            }
            
            if (newController == null)
            {
                if (normalizedHairName.Contains("black"))
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Black Hair)");
                else 
                    newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Red Hair)");
            }

            Animator rootAnim = fisherman.GetComponent<Animator>();
            if (rootAnim != null && newController != null)
            {
                if (rootAnim.runtimeAnimatorController != newController)
                {
                    rootAnim.runtimeAnimatorController = newController;
                }
            }

            // Enable root renderer only if pre-baked; otherwise disable to animate child parts
            rootRenderer.enabled = isPreBaked;

            // Find or setup the child hat cosmetic object
            Transform hatCosmeticTransform = headTransform.Find("hat Cosmetic");
            bool enableHatCosmetic = hatSprite != null && !isPreBaked;

            // Ensure the child head and hat Cosmetic GameObjects are kept active/enabled!
            headTransform.gameObject.SetActive(true);
            if (hatCosmeticTransform != null)
            {
                hatCosmeticTransform.gameObject.SetActive(true);
            }

            // If we are pre-baked, disable child modular renderers; if modular, enable them!
            SpriteRenderer[] allRenderers = fisherman.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in allRenderers)
            {
                if (sr.gameObject == fisherman) continue;
                
                if (hatCosmeticTransform != null && sr.gameObject == hatCosmeticTransform.gameObject)
                {
                    sr.enabled = enableHatCosmetic;
                    continue;
                }
                
                sr.enabled = !isPreBaked;
            }

            // If we have a custom non-prebaked hat, assign its sprite and setup bobbing
            if (enableHatCosmetic && hatCosmeticTransform != null)
            {
                SpriteRenderer hatSR = hatCosmeticTransform.GetComponent<SpriteRenderer>();
                if (hatSR != null)
                {
                    hatSR.sprite = hatSprite;
                    hatSR.sortingOrder = rootRenderer.sortingOrder + 10;
                }

                CosmeticRuntimeApplier applier = hatCosmeticTransform.GetComponent<CosmeticRuntimeApplier>();
                if (applier == null)
                {
                    applier = hatCosmeticTransform.gameObject.AddComponent<CosmeticRuntimeApplier>();
                }
                applier.rootRenderer = rootRenderer;
                applier.cosmeticRenderer = hatSR;
                applier.rootAnimator = rootAnim;
                applier.baseLocalPosition = new Vector3(-0.029f, 0.075f, -0.9f); // Perfect alignment as requested
                applier.baseLocalRotation = Vector3.zero;
                applier.baseLocalScale = new Vector3(0.73484f, 0.73484f, 0.73484f);
                applier.followsFishermanAnimation = true;
            }

            return;
        }
        // --- END MODULAR LOGIC ---

        Animator anim = fisherman.GetComponent<Animator>();
        if (anim != null) 
        {
            string currentHairName = hairSprite != null ? hairSprite.name.ToLowerInvariant() : "";
            string currentHatName = hatSprite != null ? hatSprite.name.ToLowerInvariant() : "";
            RuntimeAnimatorController newController = null;
            
            if (currentHatName.Contains("yellow") || currentHatName.Contains("fishing_hat"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan Yellow hat");
            else if (currentHairName.Contains("black"))
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Black Hair)");
            else 
                newController = Resources.Load<RuntimeAnimatorController>("FishermanControllers/FisherMan (Red Hair)");
                
            if (newController != null && anim.runtimeAnimatorController != newController)
            {
                anim.runtimeAnimatorController = newController;
            }
        }

        if (hairSprite != null)
        {
            string currentHairName = hairSprite.name.ToLowerInvariant();
            if (currentHairName.Contains("red") || currentHairName.Contains("black"))
            {
                RemoveCosmetic(fisherman, FishermanHairChildName);
                RemoveCosmetic(fisherman, FishermanHatChildName);
                return;
            }

            RemoveCosmetic(fisherman, FishermanHatChildName);
            CosmeticTransform hairTransform = GetFishermanHairTransform(hairSprite);
            CreateOrUpdateCosmetic(fisherman, FishermanHairChildName, hairSprite, hairTransform.Position, hairTransform.Rotation, hairTransform.Scale, 5, true);
            return;
        }

        if (hatSprite != null)
        {
            string currentHatNameCheck = hatSprite.name.ToLowerInvariant();
            if (currentHatNameCheck.Contains("yellow") || currentHatNameCheck.Contains("fishing_hat"))
            {
                RemoveCosmetic(fisherman, FishermanHairChildName);
                RemoveCosmetic(fisherman, FishermanHatChildName);
                return;
            }

            RemoveCosmetic(fisherman, FishermanHairChildName);
            CosmeticTransform hatTransform = GetFishermanHatTransform(hatSprite);
            CreateOrUpdateCosmetic(fisherman, FishermanHatChildName, hatSprite, hatTransform.Position, hatTransform.Rotation, hatTransform.Scale, 3, true);
        }
    }

    public static void ApplyFishHatByName(GameObject fish, string spriteName)
    {
        if (fish == null || string.IsNullOrEmpty(spriteName)) return;

        Sprite hatSprite = GetSpriteByName(spriteName);
        if (hatSprite == null) return;

        CosmeticTransform cosmeticTransform = GetFishHatTransform(fish, hatSprite);
        CreateOrUpdateCosmetic(fish, FishHatChildName, hatSprite, cosmeticTransform.Position, cosmeticTransform.Rotation, cosmeticTransform.Scale, 2, false);
    }

    public static void RemoveFishHat(GameObject fish)
    {
        RemoveCosmetic(fish, FishHatChildName);
    }

    public static Sprite GetSpriteByName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        if (cachedShopSprites == null || cachedShopSprites.Length == 0)
        {
            cachedShopSprites = Resources.LoadAll<Sprite>(ShopSpritesResourcePath);
        }

        // Pass 1: Exact match, non-preview
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && !IsPreviewSprite(sprite.name) && AreSpritesMatching(sprite.name, spriteName, true))
            {
                return sprite;
            }
        }

        // Pass 2: Exact match, fallback
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && AreSpritesMatching(sprite.name, spriteName, true))
            {
                return sprite;
            }
        }

        // Pass 3: Loose match, non-preview
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && !IsPreviewSprite(sprite.name) && AreSpritesMatching(sprite.name, spriteName, false))
            {
                return sprite;
            }
        }

        // Pass 4: Loose match, fallback
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && AreSpritesMatching(sprite.name, spriteName, false))
            {
                return sprite;
            }
        }
        return null;
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
        applier.usesAnimatedFishermanHeadReplacement = childName == FishermanHairChildName && IsAnimatedFishermanHeadSelection(sprite);
        applier.animatedFishermanHeadSprites = applier.usesAnimatedFishermanHeadReplacement ? GetAnimatedFishermanHeadSprites() : null;
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
        FishermanAnimationManager animManager = GetComponentInParent<FishermanAnimationManager>();
        string state = "";
        int frameIndex = 0;

        if (animManager != null)
        {
            state = (animManager.CurrentAnimationName ?? "").ToLowerInvariant();
            frameIndex = animManager.CurrentFrameIndex;
        }
        else
        {
            string clipName = GetCurrentClipName();
            state = string.IsNullOrEmpty(clipName) ? string.Empty : clipName.ToLowerInvariant();
            frameIndex = GetCurrentSpriteFrameIndex();
        }

        bool isLeft = true;
        FishermanController fc = GetComponentInParent<FishermanController>();
        if (fc != null)
        {
            isLeft = fc.isLeft;
        }
        else
        {
            isLeft = state.Contains("left") || state == "move forward" || state == "move backwards" || (rootRenderer != null && rootRenderer.flipX && !state.Contains("right") && !state.Contains("reverse"));
        }

        if (usesAnimatedFishermanHeadReplacement)
        {
            ApplyAnimatedFishermanHeadReplacement(state);
            return;
        }

        if (gameObject.name == FishermanHatChildName || gameObject.name == FishermanHairChildName || gameObject.name == "hat Cosmetic")
        {
            Vector3 bobOffset = GetFishermanHeadBobOffset(state, frameIndex);
            
            if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("ranger"))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0105f, 0.7946f + bob, 0f);
                    transform.localEulerAngles = Vector3.zero;
                    transform.localScale = new Vector3(3.83101f, 3.635097f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.119f, 0.768f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -5.89f);
                    transform.localScale = new Vector3(3.808891f, 3.635097f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.011f, 0.808f + bob, -0.008f);
                    transform.localEulerAngles = Vector3.zero;
                    transform.localScale = new Vector3(3.924813f, 3.635097f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.085f, 0.8f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(3.88478f, 3.635097f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                
                if (state.Contains("move"))
                {
                    bobOffset.y -= 0.03f;
                }
            }
            else if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("turtle"))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.0125f, 0.785f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, -160f, 2.5f);
                    transform.localScale = new Vector3(3.9f, 3.9f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.0125f, 0.785f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, -160f, 2.5f);
                    transform.localScale = new Vector3(3.9f, 3.9f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.0125f, 0.785f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, -160f, 2.5f);
                    transform.localScale = new Vector3(3.9f, 3.9f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.0125f, 0.785f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(3.9f, 3.9f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
            }
            else if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("blue") && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("cap"))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.034f, 0.67f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 0.36f);
                    transform.localScale = new Vector3(4.565172f, 4.707734f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.0588f, 0.6626f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -5.12f);
                    transform.localScale = new Vector3(4.565172f, 4.707734f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.025f, 0.67f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 0.36f);
                    transform.localScale = new Vector3(4.565172f, 4.707734f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0642f, 0.6625f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 1.58f);
                    transform.localScale = new Vector3(4.565172f, 4.707734f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
            }
            else if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("red") && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("cap"))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.006f, 0.68f + bob, -0.01f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(4.538098f, 4.007359f, 4.27908f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.035f, 0.684f + bob, -0.01f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(4.538098f, 4.007359f, 4.27908f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.006f, 0.68f + bob, -0.01f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(4.538098f, 4.007359f, 4.27908f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.04f, 0.655f + bob, -0.01f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(4.538098f, 4.007359f, 4.27908f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
            }
            else if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("chef"))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.01f, 0.75f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 20.4f);
                    transform.localScale = new Vector3(4.647937f, 4.647937f, 4.647937f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0275f, 0.785f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 160f, 20.4f);
                    transform.localScale = new Vector3(4.647937f, 4.647937f, 4.647937f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0275f, 0.785f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 160f, 20.4f);
                    transform.localScale = new Vector3(4.647937f, 4.647937f, 4.647937f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.01f, 0.75f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 20.4f);
                    transform.localScale = new Vector3(4.647937f, 4.647937f, 4.647937f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
            }
            else if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("soda"))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0455f, 0.7612f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 10.1f);
                    transform.localScale = new Vector3(3.643995f, 4f, 4f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.09f, 0.74f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -7.29f);
                    transform.localScale = new Vector3(3.643995f, 4f, 4f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.0115f, 0.7655f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 0.85f);
                    transform.localScale = new Vector3(3.822506f, 4f, 4f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0225f, 0.73f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 2.5f);
                    transform.localScale = new Vector3(4f, 4f, 4f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("idle right") || state.Contains("idleright") || state.Contains("idel right") || state.Contains("idelright") || ((state == "idle" || state == "idel" || state.Contains("cast") || state.Contains("fishing")) && !isLeft))
                {
                    transform.localPosition = new Vector3(-0.005f, 0.765f, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -3.88f);
                    transform.localScale = new Vector3(4f, 4f, 4f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
            }
            else if (cosmeticRenderer != null && cosmeticRenderer.sprite != null && (cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("fish") || cosmeticRenderer.sprite.name.ToLowerInvariant().Contains("frog")))
            {
                if (state.Contains("move reverse backwards") || state.Contains("movereversebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0461f, 0.698f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 8.6f);
                    transform.localScale = new Vector3(3.672022f, 3.79665f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("move reverse forward") || state.Contains("movereverseforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(0.036f, 0.705f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -8.79f);
                    transform.localScale = new Vector3(3.672022f, 3.79665f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move backwards") || state.Contains("movebackwards"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0461f, 0.698f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -5.58f);
                    transform.localScale = new Vector3(3.672022f, 3.79665f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
                else if (state.Contains("move forward") || state.Contains("moveforward"))
                {
                    float bob = frameIndex == 1 || frameIndex == 2 ? 0.035f : 0f;
                    transform.localPosition = new Vector3(-0.0461f, 0.698f + bob, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, 8.86f);
                    transform.localScale = new Vector3(3.672022f, 3.79665f, 3.9f);
                    cosmeticRenderer.flipX = false;
                    return;
                }
                else if (state.Contains("idle right") || state.Contains("idleright") || state.Contains("idel right") || state.Contains("idelright") || ((state == "idle" || state == "idel" || state.Contains("cast") || state.Contains("fishing")) && !isLeft))
                {
                    transform.localPosition = new Vector3(0.01f, 0.729f, 0f);
                    transform.localEulerAngles = new Vector3(0f, 0f, -9.42f);
                    transform.localScale = new Vector3(3.621509f, 3.79665f, 3.9f);
                    cosmeticRenderer.flipX = true;
                    return;
                }
            }

            transform.localPosition = baseLocalPosition + bobOffset;
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
            case "beret":
                return new CosmeticTransform(
                    new Vector3(-0.045f, 0.285f, -0.01f),
                    new Vector3(0f, 0f, -8f),
                    new Vector3(2.1f, 1.95f, 2.1f));
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
            case "beret":
                return new CosmeticTransform(
                    new Vector3(-0.064f, 0.15f, -0.01f),
                    new Vector3(0f, 0f, -8f),
                    new Vector3(1.25f, 1.14f, 1.25f));
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
                    new Vector3(0f, -160f, -1.767f),
                    new Vector3(4.565172f, 4.707734f, 3.9f));
            case "fishermanhatredcap":
                return new CosmeticTransform(
                    new Vector3(0.04f, 0.7f, -0.01f),
                    new Vector3(0f, -160f, 2.5f),
                    new Vector3(4.538098f, 4.007359f, 4.27908f));
            case "fishermanhatchefhat":
                return new CosmeticTransform(
                    new Vector3(-0.01f, 0.75f, 0f),
                    new Vector3(0f, 0f, 20.4f),
                    new Vector3(4.647937f, 4.647937f, 4.647937f));
            case "fishermanhatsodahat":
                return new CosmeticTransform(
                    new Vector3(-0.0225f, 0.73f, 0f),
                    new Vector3(0f, 0f, 2.5f),
                    new Vector3(4f, 4f, 4f));
            case "turtlehat":
                return new CosmeticTransform(
                    new Vector3(0.0125f, 0.785f, 0f),
                    new Vector3(0f, -160f, 2.5f),
                    new Vector3(3.9f, 3.9f, 3.9f));
            case "fishermanhatrangerhat":
                return new CosmeticTransform(
                    new Vector3(-0.0444f, 0.8293f, 0f),
                    new Vector3(0f, 0f, 2.5f),
                    new Vector3(3.924813f, 3.635097f, 3.9f));
            case "fishermanhatfishhat":
                return new CosmeticTransform(
                    new Vector3(-0.0416f, 0.6991f, 0f),
                    new Vector3(0f, 0f, 2.5f),
                    new Vector3(3.621509f, 3.79665f, 3.9f));
            default:
                return new CosmeticTransform(
                    new Vector3(-0.005f, 0.77f, 0f),
                    new Vector3(0f, -160f, 2.5f),
                    Vector3.one * 3.9f);
        }
    }

    private static CosmeticTransform GetFishermanHairTransform(Sprite sprite)
    {
        if (IsAnimatedFishermanHeadSelection(sprite))
        {
            return new CosmeticTransform(
                new Vector3(-0.48f, 0.16f, -0.01f),
                Vector3.zero,
                Vector3.one * 4f);
        }

        return new CosmeticTransform(
            new Vector3(0.04f, -0.2f, 0f),
            new Vector3(0f, -160f, 2.5f),
            new Vector3(4.86f, 5.72f, 10.99f));
    }

    private void ApplyAnimatedFishermanHeadReplacement(string state)
    {
        if (animatedFishermanHeadSprites == null || animatedFishermanHeadSprites.Length == 0)
        {
            animatedFishermanHeadSprites = GetAnimatedFishermanHeadSprites(selectedFishermanHair);
        }

        int row = GetAnimatedFishermanHeadRow(state);
        int frameIndex = GetCurrentSpriteFrameIndex();
        int spriteIndex = row * 4 + Mathf.Clamp(frameIndex, 0, 3);

        if (spriteIndex >= 0 && spriteIndex < animatedFishermanHeadSprites.Length)
        {
            Sprite headSprite = animatedFishermanHeadSprites[spriteIndex];
            if (headSprite != null)
            {
                cosmeticRenderer.sprite = headSprite;
            }
        }

        // Apply bobbing offset and preserve local scale (including sign for flipX)
        transform.localPosition = baseLocalPosition + GetFishermanHeadBobOffset(state, frameIndex);
        float currentSignX = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(currentSignX * Mathf.Abs(baseLocalScale.x), baseLocalScale.y, baseLocalScale.z);
        transform.localEulerAngles = Vector3.zero;
        cosmeticRenderer.flipX = false;
        cosmeticRenderer.flipY = rootRenderer.flipY;
    }

    private void AlignTrimmedHeadSpriteToRootFrame(Sprite headSprite)
    {
        if (headSprite == null || rootRenderer == null || rootRenderer.sprite == null)
        {
            transform.localPosition = baseLocalPosition;
            transform.localScale = baseLocalScale;
            return;
        }

        Sprite rootSprite = rootRenderer.sprite;
        float rootPixelsPerUnit = rootSprite.pixelsPerUnit;
        float sourceFrameWidth = headSprite.texture != null
            ? Mathf.Max(1f, headSprite.texture.width / 4f)
            : Mathf.Max(1f, rootSprite.rect.width);
        float sourceFrameHeight = sourceFrameWidth;
        float headXInFrame = headSprite.rect.x % sourceFrameWidth;
        float headYInFrame = headSprite.rect.y % sourceFrameHeight;

        transform.localPosition = new Vector3(
            (headXInFrame - rootSprite.pivot.x) / rootPixelsPerUnit,
            (headYInFrame - rootSprite.pivot.y) / rootPixelsPerUnit,
            baseLocalPosition.z);

        float scale = headSprite.pixelsPerUnit / rootPixelsPerUnit;
        transform.localScale = Vector3.one * scale;
    }

    public static bool IsTroutFish(GameObject fish)
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

    public static bool IsBassFish(GameObject fish)
    {
        if (fish == null)
        {
            return false;
        }

        string objectName = fish.name.ToLowerInvariant();
        if (objectName.Contains("fish 1") || objectName.Contains("bass") || objectName == "fish" || objectName.StartsWith("fish(clone)"))
        {
            return true;
        }

        SpriteRenderer renderer = fish.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            return false;
        }

        string spriteName = renderer.sprite.name.ToLowerInvariant();
        return spriteName.Contains("fish 1") || spriteName.Contains("bass");
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

        // Pass 1: Exact match, non-preview
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && !IsPreviewSprite(sprite.name) && AreSpritesMatching(sprite.name, selectedSpriteName, true))
            {
                return sprite;
            }
        }

        // Pass 2: Exact match, fallback
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && AreSpritesMatching(sprite.name, selectedSpriteName, true))
            {
                return sprite;
            }
        }

        // Pass 3: Loose match, non-preview
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && !IsPreviewSprite(sprite.name) && AreSpritesMatching(sprite.name, selectedSpriteName, false))
            {
                return sprite;
            }
        }

        // Pass 4: Loose match, fallback
        for (int i = 0; i < cachedShopSprites.Length; i++)
        {
            Sprite sprite = cachedShopSprites[i];
            if (sprite != null && AreSpritesMatching(sprite.name, selectedSpriteName, false))
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

        if (rootAnimator.IsInTransition(0))
        {
            AnimatorClipInfo[] nextClips = rootAnimator.GetNextAnimatorClipInfo(0);
            if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null)
            {
                return nextClips[0].clip.name;
            }
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

        string spriteName = rootRenderer.sprite.name;
        if (spriteName.EndsWith("_0"))
        {
            spriteName = spriteName.Substring(0, spriteName.Length - 2);
        }
        int trailingNumber = 0;
        int multiplier = 1;
        bool foundDigit = false;

        for (int i = spriteName.Length - 1; i >= 0; i--)
        {
            char c = spriteName[i];
            if (c < '0' || c > '9')
            {
                break;
            }

            foundDigit = true;
            trailingNumber += (c - '0') * multiplier;
            multiplier *= 10;
        }

        if (foundDigit)
        {
            return Mathf.Clamp(trailingNumber - 1, 0, 3);
        }

        return 0;
    }

    private static bool IsAnimatedFishermanHeadSelection(Sprite sprite)
    {
        if (sprite == null) return false;
        string name = NormalizeSpriteName(sprite);
        return name == "redhair" || name == "blackhair" || name.Contains("hair") || name.StartsWith(NormalizeSpriteName(FishermanAnimatedHeadSheetName));
    }

    private static string GetHeadSheetPrefixForHair(Sprite hairSprite)
    {
        if (hairSprite == null)
        {
            return "FishermansAnimations-Head_Sheet";
        }

        string hairName = hairSprite.name;
        if (hairName.StartsWith("FishermansAnimations-Head"))
        {
            int underscoreIdx = hairName.LastIndexOf('_');
            if (underscoreIdx > 0 && char.IsDigit(hairName[hairName.Length - 1]))
            {
                return hairName.Substring(0, underscoreIdx);
            }
            return hairName;
        }

        string normalizedHairName = hairName.ToLowerInvariant().Replace("_", "").Replace("-", "");
        if (normalizedHairName.Contains("blackhair"))
        {
            return "FishermansAnimations-Head-BlackHair-Sheet";
        }
        if (normalizedHairName.Contains("redhair"))
        {
            return "FishermansAnimations-Head_Sheet";
        }

        string cleanName = hairName.Replace("_", "").Replace("-", "");
        if (cleanName.EndsWith("Hair", System.StringComparison.OrdinalIgnoreCase))
        {
            return "FishermansAnimations-Head-" + cleanName + "-Sheet";
        }

        return "FishermansAnimations-Head_Sheet";
    }

    private static Sprite[] GetAnimatedFishermanHeadSprites(Sprite hairSprite = null)
    {
        if (hairSprite == null)
        {
            EnsureSelectionsLoaded();
            hairSprite = selectedFishermanHair;
        }

        if (cachedShopSprites == null || cachedShopSprites.Length == 0)
        {
            cachedShopSprites = Resources.LoadAll<Sprite>(ShopSpritesResourcePath);
        }

        if (cachedShopSprites == null || cachedShopSprites.Length == 0)
        {
            return new Sprite[0];
        }

        string sheetPrefix = NormalizeSpriteName(GetHeadSheetPrefixForHair(hairSprite));
        Sprite[] matches = System.Array.FindAll(cachedShopSprites, sprite =>
            sprite != null && NormalizeSpriteName(sprite.name).StartsWith(sheetPrefix));

        System.Array.Sort(matches, (a, b) => GetSpriteNumericSuffix(a.name).CompareTo(GetSpriteNumericSuffix(b.name)));
        return matches;
    }

    private static int GetSpriteNumericSuffix(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return 0;
        }

        int value = 0;
        int multiplier = 1;
        bool foundDigit = false;

        for (int i = spriteName.Length - 1; i >= 0; i--)
        {
            char c = spriteName[i];
            if (c < '0' || c > '9')
            {
                break;
            }

            foundDigit = true;
            value += (c - '0') * multiplier;
            multiplier *= 10;
        }

        return foundDigit ? value : 0;
    }

    private static int GetAnimatedFishermanHeadRow(string clipName)
    {
        string state = NormalizeSpriteName(clipName);

        switch (state)
        {
            case "castingleft": return 0;
            case "castingright": return 1;
            case "cryleft": return 2;
            case "cryright": return 3;
            case "fightingleft": return 4;
            case "fightingright": return 5;
            case "fishgotofffacingleft": return 6;
            case "fishgotofffacingright": return 7;
            case "fishingleft": return 8;
            case "fishingright": return 9;
            case "idelleft":
            case "idleleft": return 10;
            case "idelright":
            case "idleright": return 11;
            case "leftpoletooar": return 12;
            case "movebackwards": return 13;
            case "moveforward": return 14;
            case "movereversebackwards": return 15;
            case "movereverseforward": return 16;
            case "oartoleftpole": return 17;
            case "oartorightpole":
            case "ourtorightpole": return 18;
            case "reelingleft": return 19;
            case "reelingright": return 20;
            case "righttoleftpole": return 21;
            case "winningleft": return 22;
            case "winningright": return 23;
            default: return 10;
        }
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

    private static bool AreSpritesMatching(string nameA, string nameB, bool exactOnly = false)
    {
        if (string.IsNullOrEmpty(nameA) || string.IsNullOrEmpty(nameB)) return false;
        
        string a = nameA.ToLowerInvariant();
        string b = nameB.ToLowerInvariant();
        
        if (a == b) return true;
        
        // Remove spaces, underscores, dashes, numbers and suffixes
        string cleanA = a.Replace(" ", "").Replace("_", "").Replace("-", "").Replace("hat", "").Replace("cosmetic", "");
        string cleanB = b.Replace(" ", "").Replace("_", "").Replace("-", "").Replace("hat", "").Replace("cosmetic", "");
        
        if (cleanA.EndsWith("0")) cleanA = cleanA.Substring(0, cleanA.Length - 1);
        if (cleanB.EndsWith("0")) cleanB = cleanB.Substring(0, cleanB.Length - 1);
        
        if (cleanA == cleanB) return true;
        
        if (exactOnly) return false;

        if (!string.IsNullOrEmpty(cleanA) && !string.IsNullOrEmpty(cleanB))
        {
            if (cleanA.Contains(cleanB) || cleanB.Contains(cleanA)) return true;
        }
        
        // Special case for turtle and other keywords
        if (a.Contains("turtle") && b.Contains("turtle")) return true;
        if (a.Contains("frog") && b.Contains("frog")) return true;
        if (a.Contains("blue") && a.Contains("cap") && b.Contains("blue") && b.Contains("cap")) return true;
        if (a.Contains("backwards") && b.Contains("backwards")) return true;
        if (a.Contains("headphones") && b.Contains("headphones")) return true;
        if (a.Contains("straw") && b.Contains("straw")) return true;
        if (a.Contains("silver") && b.Contains("silver")) return true;
        if (a.Contains("pointed") && b.Contains("pointed")) return true;
        if (a.Contains("green") && a.Contains("bucket") && b.Contains("green") && b.Contains("bucket")) return true;
        if (a.Contains("black") && a.Contains("hair") && b.Contains("black") && b.Contains("hair")) return true;
        if (a.Contains("red") && a.Contains("hair") && b.Contains("red") && b.Contains("hair")) return true;
        
        return false;
    }

    public static bool IsHatPreBaked(string hatName)
    {
        if (string.IsNullOrEmpty(hatName)) return false;
        string name = hatName.ToLowerInvariant();
        return name.Contains("yellow") || name.Contains("fishing_hat") || name.Contains("default") ||
               name.Contains("backwards") || name.Contains("blue") || name.Contains("frog") || name.Contains("griin") ||
               (name.Contains("green") && !name.Contains("turtle")) || name.Contains("headphones") || 
               name.Contains("silver") || name.Contains("straw") || name.Contains("white");
    }

    private static bool IsPreviewSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return false;
        string name = spriteName.ToLowerInvariant();
        
        // Fisherman preview sprites have a space after fisherman/fishermna/fishaerman
        if (name.StartsWith("fisherman ") || 
            name.StartsWith("fishermna ") || 
            name.StartsWith("fishaerman ") || 
            name.Contains("preview"))
        {
            return true;
        }
        return false;
    }
}
