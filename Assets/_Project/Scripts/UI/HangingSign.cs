using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// Hangs a pixel-art sign from the top of the screen on two chains.
    /// </summary>
    /// <remarks>
    /// <para>The sign art ends in two chain links at its top edge, which read as hooks with nothing to
    /// hang from. This draws a chain from each link up to the top of the canvas, tiled from the
    /// sign's own link pixels, so it matches the art exactly at any sign size.</para>
    ///
    /// <para>Recomputed every frame: the sign is resized at runtime (the shop fits it to a share of
    /// the screen width), and the gap to the top of the screen changes with the screen shape.</para>
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class HangingSign : MonoBehaviour
    {
        [Tooltip("One chain link, as drawn on the sign (5x4 px for the Sal-T sign).")]
        public Sprite chainLink;

        [Tooltip("Horizontal centre of each chain in sign pixels, measured from the sign's left edge.")]
        public float[] chainCentresPx = { 6.5f, 64.5f };

        [Tooltip("The sign's own art width in pixels; converts pixel positions to the sign's rect.")]
        public float signWidthPx = 69f;

        private RectTransform rect;
        private RectTransform canvasRect;
        private Canvas canvas;
        private RectTransform[] chains;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        }

        private void LateUpdate()
        {
            if (chainLink == null || canvasRect == null || signWidthPx <= 0f)
            {
                return;
            }

            if (chains == null)
            {
                Build();
            }

            // One sign pixel in this rect's local units, and the screen top in the same space.
            float unitsPerPx = rect.rect.width / signWidthPx;
            canvasRect.GetWorldCorners(corners);
            float top = rect.InverseTransformPoint(corners[1]).y;
            float bottom = rect.rect.yMax; // the links on the sign's top edge are where the chain meets it
            float length = Mathf.Max(0f, top - bottom);

            // Tiled images take their tile size from the sprite and the canvas's reference PPU;
            // scale that so one tile is exactly one link at the sign's own pixel size.
            float tileUnits = chainLink.rect.width * (canvas.rootCanvas.referencePixelsPerUnit / chainLink.pixelsPerUnit);
            float multiplier = tileUnits / (chainLink.rect.width * unitsPerPx);

            for (int i = 0; i < chains.Length; i++)
            {
                RectTransform chain = chains[i];
                float x = rect.rect.xMin + chainCentresPx[i] * unitsPerPx;
                chain.anchorMin = chain.anchorMax = new Vector2(0.5f, 0.5f);
                chain.pivot = new Vector2(0.5f, 0f);
                chain.sizeDelta = new Vector2(chainLink.rect.width * unitsPerPx, length);
                chain.anchoredPosition = new Vector2(x - rect.rect.center.x, bottom - rect.rect.center.y);

                Image image = chain.GetComponent<Image>();
                if (!Mathf.Approximately(image.pixelsPerUnitMultiplier, multiplier))
                {
                    image.pixelsPerUnitMultiplier = multiplier;
                }
            }
        }

        private void Build()
        {
            chains = new RectTransform[chainCentresPx.Length];
            for (int i = 0; i < chains.Length; i++)
            {
                var go = new GameObject("Chain " + (i + 1), typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.sprite = chainLink;
                image.type = Image.Type.Tiled;
                image.raycastTarget = false; // the sign is the button; the chains are decoration
                chains[i] = (RectTransform)go.transform;
            }
        }
    }
}
