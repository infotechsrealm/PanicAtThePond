using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a frame-by-frame sprite animation on a target UI Image, used to drive the
/// animated "BG. 2.gif" background. Unity imports a .gif as a single static texture, so the
/// GIF frames are extracted to individual sprites (BG2_Frames) and cycled here at runtime.
///
/// Enable this component to play the animation; disable it to leave the Image on its current
/// static sprite. GameManager turns it on only when the BG_2 map (possibleBGSprites[0]) is selected.
/// </summary>
using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.UI
{
[DisallowMultipleComponent]
public class AnimatedBackground : MonoBehaviour
{
    [Tooltip("Image whose sprite is swapped each frame. Defaults to the Image on this GameObject.")]
    public Image targetImage;

    [Tooltip("Animation frames in play order (extracted from BG. 2.gif).")]
    public Sprite[] frames;

    [Tooltip("Playback speed in frames per second. BG. 2.gif is 10 fps (100ms per frame).")]
    public float framesPerSecond = 10f;

    [Tooltip("Loop the animation when it reaches the last frame.")]
    public bool loop = true;

    [Tooltip("Use unscaled time so the background keeps animating even when the game is paused.")]
    public bool useUnscaledTime = true;

    private int currentFrame;
    private float timer;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        currentFrame = 0;
        timer = 0f;
        ApplyFrame(0);
    }

    private void Update()
    {
        if (targetImage == null || frames == null || frames.Length == 0 || framesPerSecond <= 0f)
            return;

        if (!loop && currentFrame >= frames.Length - 1)
            return;

        timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        // Advance as many frames as elapsed time allows (handles low frame rates / hitches).
        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            int next = currentFrame + 1;

            if (next >= frames.Length)
            {
                if (loop)
                {
                    next = 0;
                }
                else
                {
                    currentFrame = frames.Length - 1;
                    ApplyFrame(currentFrame);
                    return;
                }
            }

            currentFrame = next;
            ApplyFrame(currentFrame);
        }
    }

    private void ApplyFrame(int index)
    {
        if (frames != null && index >= 0 && index < frames.Length && frames[index] != null && targetImage != null)
        {
            targetImage.sprite = frames[index];
        }
    }
}

}