using UnityEngine;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// Keeps a RectTransform exactly covering its root canvas, whatever its parent's rect is.
    /// </summary>
    /// <remarks>
    /// Used inside the visibility-mode covers. A cover is a strip (the top or bottom part of the
    /// screen) with a <see cref="UnityEngine.UI.RectMask2D"/>; the background layers inside it have
    /// to be laid out against the <i>whole screen</i>, not the strip, so the art inside the cover
    /// lines up pixel-for-pixel with the background behind it. Anchors cannot express "the full
    /// screen" from inside a strip whose edges carry per-map offsets, so this matches the canvas
    /// corners directly.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenMatchRect : MonoBehaviour
    {
        private RectTransform rect;
        private RectTransform canvasRect;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
            Match();
        }

        private void LateUpdate()
        {
            Match();
        }

        private void Match()
        {
            if (rect == null || canvasRect == null || rect.parent == null)
            {
                return;
            }

            canvasRect.GetWorldCorners(corners);
            var parent = (RectTransform)rect.parent;
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);

            Vector2 size = max - min;
            Vector2 centre = (min + max) * 0.5f;

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // The parent's own pivot sits at local (0,0); anchors at its centre are offset from it.
            Vector2 parentCentre = parent.rect.center;
            Vector2 anchored = centre - parentCentre;

            if ((rect.sizeDelta - size).sqrMagnitude > 0.01f)
            {
                rect.sizeDelta = size;
            }

            if ((rect.anchoredPosition - anchored).sqrMagnitude > 0.01f)
            {
                rect.anchoredPosition = anchored;
            }
        }
    }
}
