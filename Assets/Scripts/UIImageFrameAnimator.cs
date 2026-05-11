using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageFrameAnimator : MonoBehaviour
{
    public string resourcesFolder = "Fishingshop2Frames";
    public float framesPerSecond = 10f;
    public bool playOnEnable = true;
    public bool loop = true;

    private Image targetImage;
    private Sprite[] frames;
    private int currentFrame;
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        LoadFrames();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        isPlaying = false;
    }

    private void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0 || framesPerSecond <= 0f)
        {
            return;
        }

        timer += Time.unscaledDeltaTime;
        float frameDuration = 1f / framesPerSecond;
        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            AdvanceFrame();
        }
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0)
        {
            LoadFrames();
        }

        isPlaying = frames != null && frames.Length > 0;
        currentFrame = Mathf.Clamp(currentFrame, 0, frames.Length - 1);
        ApplyFrame();
    }

    public void Stop()
    {
        isPlaying = false;
    }

    private void AdvanceFrame()
    {
        currentFrame++;
        if (currentFrame >= frames.Length)
        {
            if (!loop)
            {
                currentFrame = frames.Length - 1;
                isPlaying = false;
            }
            else
            {
                currentFrame = 0;
            }
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (targetImage != null && frames != null && frames.Length > 0)
        {
            targetImage.sprite = frames[currentFrame];
        }
    }

    private void LoadFrames()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcesFolder);
        if (textures == null || textures.Length == 0)
        {
            frames = Array.Empty<Sprite>();
            return;
        }

        Array.Sort(textures, (left, right) => string.CompareOrdinal(left.name, right.name));
        frames = new Sprite[textures.Length];

        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            frames[i] = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
