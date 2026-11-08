using UnityEngine;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// Grows a full-screen picture just enough to cover the whole canvas on any screen shape.
    /// </summary>
    /// <remarks>
    /// <para>The Sal-T shop is a 2:1 picture with the shop's shelves, signs and prices laid out on it
    /// as children. Its authored size covers a 16:9 screen with a few pixels to spare, so on a taller
    /// screen (16:10, 4:3) the canvas grew past it and the menu behind showed through at the top,
    /// bottom and left edges.</para>
    ///
    /// <para>Scaling the picture, rather than resizing it, keeps every child registered to the art. It
    /// only ever multiplies the authored scale by one factor of at least 1, so the authored proportions
    /// are kept (the shop's scale is deliberately taller than wide, to offset its parent's squash) and
    /// the screen shape it was authored for looks exactly as before.</para>
    ///
    /// <para>The left edge stays pinned to the screen's left edge while it grows: the signs and the Back
    /// and Close buttons live on the left of the shop, and the right side is wall that already runs off
    /// screen. Growing around the centre pushed the Back button off a 4:3 screen.</para>
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CoverCanvas : MonoBehaviour
    {
        private RectTransform rect;
        private RectTransform canvasRect;
        private Vector3 authoredScale;
        private Vector2 authoredAnchoredPosition;
        private readonly Vector3[] corners = new Vector3[4];

        private void Awake()
        {
            rect = (RectTransform)transform;
            authoredScale = rect.localScale;
            authoredAnchoredPosition = rect.anchoredPosition;
        }

        private void OnEnable()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        }

        private void LateUpdate()
        {
            Fit();
        }

        /// <summary>Re-fits now; tools that resize the canvas outside the player loop call this.</summary>
        public void Fit()
        {
            if (rect == null || canvasRect == null || rect.parent == null)
            {
                return;
            }

            // Start from the authored placement for the current layout: the anchor moves with the parent
            // on every screen shape, so a stored local position would go stale.
            rect.anchoredPosition = authoredAnchoredPosition;
            Vector3 authoredPosition = rect.localPosition;

            // The canvas edges, in the parent's space.
            var parent = (RectTransform)rect.parent;
            canvasRect.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);

            // Extents of the picture on each side of its pivot at the authored scale.
            Rect r = rect.rect;
            float left = -r.xMin * authoredScale.x, right = r.xMax * authoredScale.x;
            float down = -r.yMin * authoredScale.y, up = r.yMax * authoredScale.y;
            if (left <= 0f || right <= 0f || down <= 0f || up <= 0f)
            {
                return;
            }

            // Tall enough to cover top and bottom around its authored height, and wide enough to span
            // the screen once its left edge is on the screen's left edge.
            float y = authoredPosition.y;
            float factor = Mathf.Max(1f, Mathf.Max(
                Mathf.Max((y - min.y) / down, (max.y - y) / up),
                (max.x - min.x) / (left + right)));

            // The left edge sits on the screen's left edge. On narrow screens the picture's anchor moves
            // left with its parent, so keeping the authored edge would crop the Back sign.
            float x = min.x + left * factor;

            Vector3 scale = new Vector3(authoredScale.x * factor, authoredScale.y * factor, authoredScale.z);
            if ((rect.localScale - scale).sqrMagnitude > 1e-8f)
            {
                rect.localScale = scale;
            }

            Vector3 position = new Vector3(x, authoredPosition.y, authoredPosition.z);
            if ((rect.localPosition - position).sqrMagnitude > 1e-6f)
            {
                rect.localPosition = position;
            }
        }
    }
}
