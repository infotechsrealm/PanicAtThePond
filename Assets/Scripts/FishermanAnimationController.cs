using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// WORKING Animation System - Simple and Reliable
/// Directly loads sprite sheets and plays animations on fisherman state changes
/// </summary>
public class FishermanAnimationController : MonoBehaviour
{
    [SerializeField] private FishermanController fishermanController;
    private List<SpriteRenderer> bodyParts = new List<SpriteRenderer>();

    [Header("Sprite Sheet References")]
    [SerializeField] private Texture2D bodySheet;
    [SerializeField] private Texture2D armsSheet;
    [SerializeField] private Texture2D boatSheet;
    [SerializeField] private Texture2D rodsSheet;

    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 0.1f;
    [SerializeField] private int frameWidth = 256;
    [SerializeField] private int frameHeight = 192;

    // Animation frame storage
    private Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();

    // Current animation state
    private string currentAnimation = "";
    private float frameTimer = 0f;
    private int currentFrame = 0;
    private bool isAnimating = false;

    // State tracking
    private bool lastIsMoving = false;
    private bool lastIsCasting = false;
    private string lastDirection = "left";

    private void Start()
    {
        Debug.Log("🔄 [FISHERMAN ANIMATION] Initializing...");

        // Get controller reference
        if (fishermanController == null)
            fishermanController = GetComponent<FishermanController>();

        // Find all body part sprite renderers
        FindBodyParts();

        // Load animations
        if (!LoadAnimations())
        {
            Debug.LogError("❌ Failed to load animations!");
            return;
        }

        Debug.Log("✅ [FISHERMAN ANIMATION] Ready!");
    }

    private void FindBodyParts()
    {
        bodyParts.Clear();

        // Get all sprite renderers in children
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in renderers)
        {
            // Skip if it's the root or named "head" (will handle separately)
            if (sr.gameObject.name.ToLower() != "head" && sr != GetComponent<SpriteRenderer>())
            {
                bodyParts.Add(sr);
                Debug.Log($"  ✓ Found: {sr.gameObject.name}");
            }
        }

        Debug.Log($"✓ Found {bodyParts.Count} animated body parts");
    }

    private bool LoadAnimations()
    {
        // Load sprite sheets from current location
        if (bodySheet == null)
            bodySheet = FishermanSpriteLoader.LoadSpriteSheet("GreenBody");
        if (armsSheet == null)
            armsSheet = FishermanSpriteLoader.LoadSpriteSheet("Arms");
        if (boatSheet == null)
            boatSheet = FishermanSpriteLoader.LoadSpriteSheet("Boat");
        if (rodsSheet == null)
            rodsSheet = FishermanSpriteLoader.LoadSpriteSheet("Rods");

        if (bodySheet == null)
        {
            Debug.LogError("❌ Could not load sprite sheets from Assets/Animations/Fisher Man Animations/Sprite Sheets/");
            return false;
        }

        // Create animations from sprite sheets
        // Format: 1536 height / 192 frame height = 8 animations per sheet
        // Each animation has 4 frames horizontally

        animations["idle"] = CreateAnimationFrames(bodySheet, 0);
        animations["moveForward"] = CreateAnimationFrames(bodySheet, 1);
        animations["moveBackward"] = CreateAnimationFrames(bodySheet, 2);
        animations["casting"] = CreateAnimationFrames(rodsSheet, 3);
        animations["fishing"] = CreateAnimationFrames(rodsSheet, 4);
        animations["fighting"] = CreateAnimationFrames(rodsSheet, 5);
        animations["crying"] = CreateAnimationFrames(boatSheet, 6);
        animations["win"] = CreateAnimationFrames(armsSheet, 7);

        Debug.Log($"✅ Loaded {animations.Count} animations");
        return true;
    }

    private List<Sprite> CreateAnimationFrames(Texture2D sheet, int animationRow)
    {
        List<Sprite> frames = new List<Sprite>();

        if (sheet == null)
            return frames;

        int yStart = animationRow * frameHeight;

        // Create 4 frames per animation
        for (int i = 0; i < 4; i++)
        {
            Rect rect = new Rect(i * frameWidth, yStart, frameWidth, frameHeight);

            Sprite sprite = Sprite.Create(
                sheet,
                rect,
                new Vector2(0.5f, 0.5f),
                100f
            );

            sprite.name = $"Frame_{animationRow}_{i}";
            frames.Add(sprite);
        }

        return frames;
    }

    private void Update()
    {
        if (fishermanController == null)
            return;

        // Check for state changes
        UpdateAnimationState();

        // Update animation playback
        if (isAnimating)
        {
            UpdateFramePlayback();
        }
    }

    private void UpdateAnimationState()
    {
        // MOVEMENT STATE
        if (fishermanController.isMoving != lastIsMoving)
        {
            lastIsMoving = fishermanController.isMoving;

            if (fishermanController.isMoving)
            {
                string direction = fishermanController.isLeft ? "moveForward" : "moveBackward";
                PlayAnimation(direction);
            }
            else
            {
                PlayAnimation("idle");
            }
        }

        // CASTING STATE
        if (fishermanController.isCasting != lastIsCasting)
        {
            lastIsCasting = fishermanController.isCasting;

            if (fishermanController.isCasting)
            {
                PlayAnimation("casting");
            }
        }
    }

    private void UpdateFramePlayback()
    {
        if (!animations.ContainsKey(currentAnimation))
            return;

        frameTimer += Time.deltaTime;

        if (frameTimer >= animationSpeed)
        {
            frameTimer = 0f;

            List<Sprite> frames = animations[currentAnimation];
            currentFrame = (currentFrame + 1) % frames.Count;

            // Update all body parts
            Sprite frameSprite = frames[currentFrame];
            foreach (SpriteRenderer renderer in bodyParts)
            {
                if (renderer != null)
                {
                    renderer.sprite = frameSprite;
                }
            }
        }
    }

    public void PlayAnimation(string animName)
    {
        if (currentAnimation == animName && isAnimating)
            return;

        if (!animations.ContainsKey(animName))
        {
            Debug.LogWarning($"⚠ Animation not found: {animName}");
            return;
        }

        currentAnimation = animName;
        currentFrame = 0;
        frameTimer = 0f;
        isAnimating = true;

        Debug.Log($"▶ Playing: {animName}");
    }

    // Public methods for special animations
    public void PlayFighting()
    {
        PlayAnimation("fighting");
    }

    public void PlayCrying()
    {
        PlayAnimation("crying");
    }

    public void PlayWin()
    {
        PlayAnimation("win");
    }

    public void PlayFishing()
    {
        PlayAnimation("fishing");
    }

    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
}
