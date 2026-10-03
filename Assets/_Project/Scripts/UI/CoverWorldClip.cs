using System.Collections.Generic;
using PanicAtThePond.Controllers;
using UnityEngine;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// While a bottom visibility cover is up, clips chosen world sprites to the area under it.
    /// </summary>
    /// <remarks>
    /// <para>The fisherman-side water cover (Murky and Deep water) hides the underwater world, but
    /// its top edge has to stay under the boat's hull, which rides in the surface strip. A fish near
    /// its ceiling is still under the cover; its hat is not: hats stand about half a world unit
    /// above the body, into the strip, and gave away where the fish was.</para>
    ///
    /// <para>Moving the cover up would hide the bottom of the fisherman's own boat, and lowering the
    /// fish ceiling would change gameplay, so this clips only the fish sprites, only on the screen
    /// where the cover is showing: a <see cref="SpriteMask"/> fills the world below the cover's top
    /// edge and the fish renderers are set to draw inside it. Everything is restored when the cover
    /// goes away (the catch reveal disables it), so nothing changes for anyone else.</para>
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CoverWorldClip : MonoBehaviour
    {
        /// <summary>Far larger than any camera view, so the mask never ends on screen.</summary>
        private const float MaskExtent = 1000f;

        private readonly Dictionary<SpriteRenderer, SpriteMaskInteraction> clipped =
            new Dictionary<SpriteRenderer, SpriteMaskInteraction>();
        private readonly Vector3[] corners = new Vector3[4];

        private RectTransform rect;
        private SpriteMask mask;
        private static Sprite maskSprite;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            if (mask == null)
            {
                var go = new GameObject("CoverWorldClipMask");
                go.hideFlags = HideFlags.DontSave;
                mask = go.AddComponent<SpriteMask>();
                mask.sprite = MaskSprite();
            }

            mask.gameObject.SetActive(true);
            Apply();
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<SpriteRenderer, SpriteMaskInteraction> entry in clipped)
            {
                if (entry.Key != null)
                {
                    entry.Key.maskInteraction = entry.Value;
                }
            }

            clipped.Clear();
            if (mask != null)
            {
                mask.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (mask != null)
            {
                Destroy(mask.gameObject);
            }
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            Camera cam = Camera.main;
            if (cam == null || mask == null)
            {
                return;
            }

            // Top edge of the cover in screen pixels (the cover lives on an overlay canvas).
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.rootCanvas.worldCamera;
            rect.GetWorldCorners(corners);
            Vector2 screenTop = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[1]);

            float depth = Mathf.Abs(cam.transform.position.z);
            Vector3 worldTop = cam.ScreenToWorldPoint(new Vector3(screenTop.x, screenTop.y, depth));
            Transform maskTransform = mask.transform;
            maskTransform.position = new Vector3(cam.transform.position.x, worldTop.y, 0f);
            maskTransform.localScale = new Vector3(MaskExtent, MaskExtent, 1f);

            // Fish spawn and change hats during the round, so the set is refreshed every frame.
            // The Mirror (LAN) variants sit on the same objects as these, so they are covered too.
            foreach (FishController fish in FindObjectsByType<FishController>(FindObjectsSortMode.None))
            {
                Clip(fish);
            }

            foreach (GoldenFishAI golden in FindObjectsByType<GoldenFishAI>(FindObjectsSortMode.None))
            {
                Clip(golden);
            }
        }

        private void Clip(Component owner)
        {
            foreach (SpriteRenderer sprite in owner.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!clipped.ContainsKey(sprite))
                {
                    clipped[sprite] = sprite.maskInteraction;
                    sprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
        }

        /// <summary>A 1x1-unit opaque square pivoted on its top edge, scaled to fill the world below.</summary>
        private static Sprite MaskSprite()
        {
            if (maskSprite == null)
            {
                Texture2D white = Texture2D.whiteTexture;
                maskSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height),
                    new Vector2(0.5f, 1f), white.width);
                maskSprite.name = "CoverWorldClipMask";
            }

            return maskSprite;
        }
    }
}
