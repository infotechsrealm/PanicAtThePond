using UnityEngine;
using System.Collections.Generic;

public class FishermanAnimationManager : MonoBehaviour
{
    [System.Serializable]
    public class BodyPartAnimator
    {
        public string partName;
        public SpriteRenderer spriteRenderer;
        public Sprite[] allSprites; // All sprites from the sheet, sorted by numeric suffix

        [System.NonSerialized]
        public List<Sprite>[,] gridMap; // 24 rows x 4 columns of 64x64 pixel cells

        /// <summary>
        /// Set the frame for a given row and frame column index using the mathematically precise 64x64 grid system.
        /// Handles sub-sprites (e.g. left vs right hand/rod) inside the same cell.
        /// </summary>
        public void SetFrame(int row, int frameIndex)
        {
            if (spriteRenderer == null || allSprites == null || allSprites.Length == 0)
                return;

            if (gridMap != null && row >= 0 && row < 24 && frameIndex >= 0 && frameIndex < 4)
            {
                List<Sprite> cellSprites = gridMap[row, frameIndex];
                if (cellSprites.Count == 0)
                {
                    spriteRenderer.sprite = null;
                    return;
                }

                string name = partName.ToLowerInvariant();
                bool isRightSide = name.Contains("right");
                int spriteIdx = 0;

                // If this is a hand or a rod, it has 2 sub-sprites inside the same grid cell:
                // Left Hand/Rod is index 0 (physically on the left), Right Hand/Rod is index 1 (physically on the right)
                if (name.Contains("hand") || name.Contains("arm") || name.Contains("road") || name.Contains("rod"))
                {
                    if (isRightSide)
                    {
                        spriteIdx = cellSprites.Count > 1 ? 1 : 0;
                    }
                    else
                    {
                        spriteIdx = 0;
                    }
                }

                if (spriteIdx >= 0 && spriteIdx < cellSprites.Count)
                {
                    spriteRenderer.sprite = cellSprites[spriteIdx];
                }
            }
            else
            {
                // Fallback: simple 4-col formula
                int index = row * 4 + frameIndex;
                if (index >= 0 && index < allSprites.Length)
                {
                    spriteRenderer.sprite = allSprites[index];
                }
            }
        }
    }

    [SerializeField] private float frameRate = 0.25f; // 4 frames per second (slow, natural feel for pixel art)
    [SerializeField] private List<BodyPartAnimator> bodyParts = new List<BodyPartAnimator>();

    private FishermanController fishermanController;
    private string currentAnimation = "idle";
    private float animationTimer = 0f;
    private int currentFrameIndex = 0;
    private bool isAnimationPlaying = true;

    public string CurrentAnimationName => currentAnimation;
    public int CurrentFrameIndex => currentFrameIndex;

    private struct AnimationInfo
    {
        public int rowLeft;
        public int rowRight;
        public int totalFrames;

        public AnimationInfo(int rowLeft, int rowRight, int totalFrames)
        {
            this.rowLeft = rowLeft;
            this.rowRight = rowRight;
            this.totalFrames = totalFrames;
        }
    }

    private Dictionary<string, AnimationInfo> animationInfoMap = new Dictionary<string, AnimationInfo>
    {
        { "idle", new AnimationInfo(10, 11, 4) },
        { "moveforward", new AnimationInfo(14, 14, 4) },
        { "movebackward", new AnimationInfo(13, 13, 4) },
        { "casting", new AnimationInfo(0, 1, 2) },
        { "fishing", new AnimationInfo(8, 9, 2) },
        { "fighting", new AnimationInfo(4, 5, 4) },
        { "crying", new AnimationInfo(2, 3, 3) },
        { "win", new AnimationInfo(22, 23, 1) },
        { "reeling", new AnimationInfo(19, 20, 3) },
        { "fishgotoff", new AnimationInfo(6, 7, 4) }
    };

    private void Start()
    {
        fishermanController = GetComponent<FishermanController>();
        if (fishermanController == null)
            fishermanController = GetComponentInParent<FishermanController>();

        // Disable root SpriteRenderer to avoid duplicate rendering in modular mode
        SpriteRenderer rootSR = GetComponent<SpriteRenderer>();
        if (rootSR != null)
        {
            rootSR.enabled = false;
        }

        // If we are using a pre-baked hat, do not run modular animations!
        string hatName = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishermanHatPrefKey, "").ToLowerInvariant();
        if (CosmeticRuntimeApplier.IsHatPreBaked(hatName))
        {
            enabled = false;
            return;
        }

        // Initialize and map all body parts
        InitializeBodyParts();
    }

    private void InitializeBodyParts()
    {
        bodyParts.Clear();

        // Load sprite arrays for each sheet
        Sprite[] bodySprites = LoadSheetSprites("GreenBody");
        Sprite[] armsSprites = LoadSheetSprites("Arms");
        Sprite[] boatSprites = LoadSheetSprites("Boat");
        Sprite[] oarsSprites = LoadSheetSprites("Oars");
        Sprite[] rodsSprites = LoadSheetSprites("Rods");
        Sprite[] headSprites = LoadHeadSprites();

        // Build 64x64 grid maps for each sheet
        var bodyGrid = BuildGridMap(bodySprites);
        var armsGrid = BuildGridMap(armsSprites);
        var boatGrid = BuildGridMap(boatSprites);
        var oarsGrid = BuildGridMap(oarsSprites);
        var rodsGrid = BuildGridMap(rodsSprites);
        var headGrid = BuildGridMap(headSprites);

        // Find child SpriteRenderers
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr == GetComponent<SpriteRenderer>()) continue; // Skip root
            if (sr.gameObject.name == "hat Cosmetic") continue;  // Skip hat cosmetic (manually overlaid)

            string partName = sr.gameObject.name.ToLowerInvariant();
            Sprite[] partSprites = null;
            List<Sprite>[,] partGrid = null;

            if (partName.Contains("body") || partName.Contains("chest"))
            {
                partSprites = bodySprites;
                partGrid = bodyGrid;
            }
            else if (partName.Contains("boat"))
            {
                partSprites = boatSprites;
                partGrid = boatGrid;
            }
            else if (partName.Contains("hand") || partName.Contains("arm"))
            {
                partSprites = armsSprites;
                partGrid = armsGrid;
            }
            else if (partName.Contains("oar"))
            {
                partSprites = oarsSprites;
                partGrid = oarsGrid;
            }
            else if (partName.Contains("road") || partName.Contains("rod"))
            {
                partSprites = rodsSprites;
                partGrid = rodsGrid;
            }
            else if (partName.Contains("head"))
            {
                partSprites = headSprites;
                partGrid = headGrid;
            }

            if (partSprites != null && partSprites.Length > 0)
            {
                BodyPartAnimator bpa = new BodyPartAnimator
                {
                    partName = sr.gameObject.name,
                    spriteRenderer = sr,
                    allSprites = partSprites,
                    gridMap = partGrid
                };
                bodyParts.Add(bpa);
                sr.enabled = true; // Ensure it's enabled to render modular parts!
            }
        }

        Debug.Log($"✓ [FishermanAnimationManager] Initialized {bodyParts.Count} body parts using precise 64x64 grid system.");
    }

    /// <summary>
    /// Build a precise 2D grid map of 24 rows x 4 columns of 64x64 pixel cells.
    /// Maps each sprite based on its visual center, and sorts each cell left-to-right (X ascending).
    /// </summary>
    private List<Sprite>[,] BuildGridMap(Sprite[] sprites)
    {
        List<Sprite>[,] grid = new List<Sprite>[24, 4];
        for (int r = 0; r < 24; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                grid[r, c] = new List<Sprite>();
            }
        }

        if (sprites == null || sprites.Length == 0)
            return grid;

        float cellWidth = 64f;
        float cellHeight = 64f;
        float textureHeight = 1536f;

        foreach (Sprite s in sprites)
        {
            if (s == null) continue;

            float cx = s.rect.x + s.rect.width / 2f;
            float cy = s.rect.y + s.rect.height / 2f;

            int col = Mathf.FloorToInt(cx / cellWidth);
            int row = Mathf.FloorToInt((textureHeight - cy) / cellHeight);

            col = Mathf.Clamp(col, 0, 3);
            row = Mathf.Clamp(row, 0, 23);

            grid[row, col].Add(s);
        }

        // Sort each cell's list by X coordinate (left to right)
        for (int r = 0; r < 24; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                grid[r, c].Sort((a, b) => (a.rect.x + a.rect.width / 2f).CompareTo(b.rect.x + b.rect.width / 2f));
            }
        }

        return grid;
    }

    private Sprite[] LoadSheetSprites(string sheetName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("FishermanSprites/FishermansAnimations-" + sheetName + "_Sheet");
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"❌ Failed to load sheet sprites: {sheetName}");
            return new Sprite[0];
        }

        // Sort by numeric suffix (_0, _1, _2...)
        System.Array.Sort(sprites, (a, b) => GetSpriteNumericSuffix(a.name).CompareTo(GetSpriteNumericSuffix(b.name)));
        Debug.Log($"  Loaded {sprites.Length} sprites for {sheetName}");
        return sprites;
    }

    private Sprite[] LoadHeadSprites()
    {
        string hairName = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishermanHairPrefKey, "");
        string sheetPrefix = "FishermansAnimations-Head_Sheet";
        if (hairName.ToLowerInvariant().Contains("black"))
        {
            sheetPrefix = "FishermansAnimations-Head-BlackHair-Sheet";
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>("ShopUI/" + sheetPrefix);
        if (sprites == null || sprites.Length == 0)
        {
            sprites = Resources.LoadAll<Sprite>("ShopUI/FishermansAnimations-Head_Sheet");
        }

        if (sprites != null && sprites.Length > 0)
        {
            System.Array.Sort(sprites, (a, b) => GetSpriteNumericSuffix(a.name).CompareTo(GetSpriteNumericSuffix(b.name)));
        }

        return sprites;
    }

    private int GetSpriteNumericSuffix(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return 0;
        int underscoreIdx = spriteName.LastIndexOf('_');
        if (underscoreIdx >= 0 && underscoreIdx < spriteName.Length - 1)
        {
            int val;
            if (int.TryParse(spriteName.Substring(underscoreIdx + 1), out val))
            {
                return val;
            }
        }
        return 0;
    }

    public void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName && isAnimationPlaying)
            return;

        currentAnimation = animationName;
        currentFrameIndex = 0;
        animationTimer = 0f;
        isAnimationPlaying = true;
    }

    // Unused registration from parser (stubbed out to keep integration script compiled)
    public void RegisterAnimationClips(string animationName, List<Sprite> sprites)
    {
    }

    private void Update()
    {
        if (!isAnimationPlaying || string.IsNullOrEmpty(currentAnimation))
            return;

        string key = currentAnimation.ToLowerInvariant();
        if (!animationInfoMap.ContainsKey(key))
            return;

        AnimationInfo info = animationInfoMap[key];

        animationTimer += Time.deltaTime;
        if (animationTimer >= frameRate)
        {
            animationTimer -= frameRate; // Subtract instead of reset to avoid frame drift
            currentFrameIndex = (currentFrameIndex + 1) % info.totalFrames;
        }

        bool isLeft = fishermanController != null ? fishermanController.isLeft : true;
        int row = isLeft ? info.rowLeft : info.rowRight;

        UpdateAllBodyParts(row, currentFrameIndex);
    }

    private void UpdateAllBodyParts(int row, int frameIndex)
    {
        foreach (BodyPartAnimator bp in bodyParts)
        {
            bp.SetFrame(row, frameIndex);
        }
    }
}
