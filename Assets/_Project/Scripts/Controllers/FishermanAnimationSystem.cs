using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Complete animation system for fisherman with all body parts
/// Automatically loads sprites and plays animations based on controller state
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.UI;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.Controllers
{
public class FishermanAnimationSystem : MonoBehaviour
{
    [SerializeField] private FishermanController fishermanController;
    [SerializeField] private List<SpriteRenderer> bodyPartRenderers = new List<SpriteRenderer>();

    [Header("Animation Frame Settings")]
    [SerializeField] private float frameRate = 0.1f; // 10 FPS
    [SerializeField] private int frameWidth = 256;
    [SerializeField] private int frameHeight = 192;

    [Header("Sprite Sheets")]
    [SerializeField] private Texture2D armsSheet;
    [SerializeField] private Texture2D boatSheet;
    [SerializeField] private Texture2D bodySheet;
    [SerializeField] private Texture2D oarsSheet;
    [SerializeField] private Texture2D rodsSheet;

    private Dictionary<string, List<Sprite>> animationFrames = new Dictionary<string, List<Sprite>>();
    private string currentAnimation = "";
    private float animationTimer = 0f;
    private int currentFrameIndex = 0;
    private bool isAnimationPlaying = false;

    private bool lastIsMoving = false;
    private bool lastIsCasting = false;

    private void OnEnable()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        Debug.Log("🔄 Fisherman Animation System: Initializing...");

        // Find fisherman controller
        if (fishermanController == null)
            fishermanController = GetComponent<FishermanController>();

        // Find all child sprite renderers
        bodyPartRenderers.Clear();
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in allRenderers)
        {
            if (sr.gameObject != gameObject) // Skip root if it has one
            {
                bodyPartRenderers.Add(sr);
                Debug.Log($"  ✓ Found body part: {sr.gameObject.name}");
            }
        }

        Debug.Log($"✓ Found {bodyPartRenderers.Count} body parts");

        // Load and parse sprite sheets
        LoadSpriteSheets();

        Debug.Log("✓ Fisherman Animation System: Ready!");
    }

    private void LoadSpriteSheets()
    {
        Debug.Log("🔄 Loading sprite sheets...");

        // Load from Resources or Assets
        if (armsSheet == null)
            armsSheet = Resources.Load<Texture2D>("Sprites/FishermansAnimations-Arms_Sheet");
        if (boatSheet == null)
            boatSheet = Resources.Load<Texture2D>("Sprites/FishermansAnimations-Boat_Sheet");
        if (bodySheet == null)
            bodySheet = Resources.Load<Texture2D>("Sprites/FishermansAnimations-GreenBody_Sheet");
        if (oarsSheet == null)
            oarsSheet = Resources.Load<Texture2D>("Sprites/FishermansAnimations-Oars_Sheet");
        if (rodsSheet == null)
            rodsSheet = Resources.Load<Texture2D>("Sprites/FishermansAnimations-Rods_Sheet");

        // Create animations from sheets
        CreateAnimationsFromSheets();
    }

    private void CreateAnimationsFromSheets()
    {
        if (bodySheet == null || armsSheet == null)
        {
            Debug.LogError("❌ Sprite sheets not found! Make sure they're in Assets/Animations/Fisher Man Animations/Sprite Sheets/");
            return;
        }

        // Create 8 frame animations (assuming 1536 / 192 = 8 animations)
        string[] animationNames = { "idle", "moveForward", "moveBackward", "casting", "fishing", "fighting", "crying", "win" };

        for (int animIndex = 0; animIndex < animationNames.Length; animIndex++)
        {
            List<Sprite> frames = new List<Sprite>();

            int yStart = animIndex * frameHeight;

            // Create sprites from this animation row
            for (int frameIndex = 0; frameIndex < 4; frameIndex++) // 4 frames per animation
            {
                Rect rect = new Rect(frameIndex * frameWidth, yStart, frameWidth, frameHeight);

                Sprite sprite = Sprite.Create(
                    bodySheet,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                sprite.name = $"{animationNames[animIndex]}_{frameIndex}";
                frames.Add(sprite);
            }

            animationFrames[animationNames[animIndex]] = frames;
            Debug.Log($"  ✓ Created animation: {animationNames[animIndex]} ({frames.Count} frames)");
        }
    }

    private void Update()
    {
        if (fishermanController == null)
            return;

        // Handle animation state changes
        HandleMovementAnimation();
        HandleCastingAnimation();

        // Update current animation
        UpdateAnimation();
    }

    private void HandleMovementAnimation()
    {
        if (fishermanController.isMoving != lastIsMoving)
        {
            lastIsMoving = fishermanController.isMoving;

            if (fishermanController.isMoving)
            {
                PlayAnimation("moveForward");
            }
            else
            {
                PlayAnimation("idle");
            }
        }
    }

    private void HandleCastingAnimation()
    {
        if (fishermanController.isCasting != lastIsCasting)
        {
            lastIsCasting = fishermanController.isCasting;

            if (fishermanController.isCasting)
            {
                PlayAnimation("casting");
            }
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName && isAnimationPlaying)
            return;

        if (!animationFrames.ContainsKey(animationName))
        {
            Debug.LogWarning($"⚠ Animation not found: {animationName}");
            return;
        }

        currentAnimation = animationName;
        currentFrameIndex = 0;
        animationTimer = 0f;
        isAnimationPlaying = true;
    }

    private void UpdateAnimation()
    {
        if (!isAnimationPlaying || !animationFrames.ContainsKey(currentAnimation))
            return;

        animationTimer += Time.deltaTime;

        if (animationTimer >= frameRate)
        {
            animationTimer -= frameRate;

            List<Sprite> frames = animationFrames[currentAnimation];
            currentFrameIndex = (currentFrameIndex + 1) % frames.Count;

            // Update all body parts with current frame
            foreach (SpriteRenderer renderer in bodyPartRenderers)
            {
                if (renderer != null)
                {
                    renderer.sprite = frames[currentFrameIndex];
                }
            }
        }
    }

    // Animation triggers for special states
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
}

}