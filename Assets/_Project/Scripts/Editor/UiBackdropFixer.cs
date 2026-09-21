using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Makes stretched UI images keep their aspect ratio on any shape of screen.
    /// </summary>
    /// <remarks>
    /// <para><b>The problem it fixes.</b> Nearly every full-screen panel in this project draws the
    /// same <c>fishtank</c> sprite, which is 1920x1280 — aspect 1.5. The canvas is 16:9, so the
    /// backdrop is squashed about 19% horizontally before any device variation is considered; on a
    /// 4:3 tablet or a 21:9 monitor it is far worse. A <c>CanvasScaler</c> cannot help, because it
    /// scales uniformly and the fault is non-uniform.</para>
    ///
    /// <para><b>Two different repairs, because there are two different cases.</b></para>
    /// <list type="bullet">
    /// <item><description><b>Backdrops</b> (a panel whose own Image is a large background) become a
    /// dedicated <c>Background</c> child carrying an <see cref="AspectRatioFitter"/> in
    /// <c>EnvelopeParent</c> mode — "cover": fill the screen, crop the overflow, never distort. The
    /// image is moved off the panel deliberately, so the panel's rect stays exactly as authored and
    /// none of its children shift.</description></item>
    /// <item><description><b>Small sprites</b> (a knob, a checkmark, an arrow) simply get
    /// <c>preserveAspect</c>. They are content, not backdrop: letterboxing inside their own rect is
    /// correct, and cropping them would cut the artwork.</description></item>
    /// </list>
    ///
    /// <para>Sliced and tiled images are skipped throughout — they are authored to be stretched, and
    /// that is what nine-slice is for.</para>
    /// </remarks>
    public static class UiBackdropFixer
    {
        private const string BackgroundChildName = "Background";

        /// <summary>
        /// An Image at least this fraction of the canvas on both axes is treated as a backdrop.
        /// </summary>
        private const float BackdropFraction = 0.85f;

        public static string FixCanvas(Canvas canvas, bool apply)
        {
            var sb = new StringBuilder();
            if (canvas == null)
            {
                return "no canvas";
            }

            var canvasRect = (RectTransform)canvas.transform;
            float canvasW = Mathf.Max(1f, canvasRect.rect.width);
            float canvasH = Mathf.Max(1f, canvasRect.rect.height);

            int backdrops = 0, aspects = 0;

            // ToArray: the backdrop repair adds children, which would otherwise invalidate the walk.
            foreach (Image img in new List<Image>(canvas.GetComponentsInChildren<Image>(true)))
            {
                if (img == null || img.sprite == null || img.type != Image.Type.Simple)
                {
                    continue;
                }

                if (img.gameObject.name == BackgroundChildName)
                {
                    continue; // already repaired
                }

                RectTransform rt = img.rectTransform;
                bool stretches = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x)
                    || !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);
                if (!stretches)
                {
                    continue;
                }

                if (img.preserveAspect || img.GetComponent<AspectRatioFitter>() != null)
                {
                    continue;
                }

                Rect r = rt.rect;

                // Full-screen panels are backdrops. So are full-width strips: a sky or water band
                // spans the screen and is meant to stretch vertically to meet its neighbour, and
                // judging it by height alone would letterbox it and open gaps at the sides.
                bool coversWidth = r.width >= canvasW * BackdropFraction;
                bool coversHeight = r.height >= canvasH * BackdropFraction;
                bool isWideStrip = coversWidth && !coversHeight;
                if (isWideStrip)
                {
                    continue; // authored to stretch; leave it exactly as it is
                }

                bool isBackdrop = coversWidth && coversHeight;

                if (isBackdrop)
                {
                    backdrops++;
                    sb.AppendLine($"  BACKDROP  {UiResponsivenessTester.HierarchyPath(img.transform)}"
                        + $"  sprite '{img.sprite.name}' {img.sprite.rect.width:0}x{img.sprite.rect.height:0}");
                    if (apply)
                    {
                        MoveBackdropToChild(img);
                    }
                }
                else
                {
                    aspects++;
                    sb.AppendLine($"  ASPECT    {UiResponsivenessTester.HierarchyPath(img.transform)}"
                        + $"  sprite '{img.sprite.name}' {img.sprite.rect.width:0}x{img.sprite.rect.height:0}");
                    if (apply)
                    {
                        img.preserveAspect = true;
                        EditorUtility.SetDirty(img);
                    }
                }
            }

            sb.Insert(0, $"canvas '{canvas.name}': {backdrops} backdrop(s), {aspects} sprite(s) "
                + $"{(apply ? "FIXED" : "would be fixed")}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Replaces a panel's own backdrop Image with a covering <c>Background</c> child.
        /// </summary>
        private static void MoveBackdropToChild(Image panelImage)
        {
            RectTransform panel = panelImage.rectTransform;
            Sprite sprite = panelImage.sprite;
            Color colour = panelImage.color;
            Material material = panelImage.material;
            bool raycastTarget = panelImage.raycastTarget;

            Object.DestroyImmediate(panelImage);

            Transform found = panel.Find(BackgroundChildName);
            RectTransform bg = found as RectTransform;
            if (bg == null)
            {
                var go = new GameObject(BackgroundChildName, typeof(RectTransform));
                bg = (RectTransform)go.transform;
                bg.SetParent(panel, false);
            }

            bg.SetSiblingIndex(0); // behind every control on the panel
            bg.anchorMin = new Vector2(0.5f, 0.5f);
            bg.anchorMax = new Vector2(0.5f, 0.5f);
            bg.pivot = new Vector2(0.5f, 0.5f);
            bg.anchoredPosition = Vector2.zero;
            bg.localScale = Vector3.one;
            bg.localRotation = Quaternion.identity;

            Image bgImage = bg.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = bg.gameObject.AddComponent<Image>();
            }

            bgImage.sprite = sprite;
            bgImage.color = colour;
            bgImage.material = material;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;  // the fitter owns the aspect

            // Keep blocking clicks. The panel's own Image was what stopped a modal panel being
            // click-through onto whatever sits behind it; removing it without handing that job to
            // the backdrop would let the player press buttons underneath an open menu.
            bgImage.raycastTarget = raycastTarget;

            AspectRatioFitter fitter = bg.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite == null || sprite.rect.height <= 0f
                ? 1f
                : sprite.rect.width / sprite.rect.height;

            EditorUtility.SetDirty(bg);
        }
    }
}
