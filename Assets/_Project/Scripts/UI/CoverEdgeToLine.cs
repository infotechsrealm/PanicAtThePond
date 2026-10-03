using UnityEngine;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// Moves the bottom edge of a visibility cover onto a line drawn by another canvas.
    /// </summary>
    /// <remarks>
    /// <para>The fish-side sky cover hides the surface world (boat, fisherman, line) in Murky and
    /// Reflective water. Its serialized edge stops above the surface strip, and the v2 fisherman's
    /// hull sits inside that strip, so the bottom of the boat showed under the cover and gave the
    /// fisherman's position away.</para>
    ///
    /// <para>The pond art's waterline is the pivot of its layer container on the background canvas
    /// (a camera-space canvas), while the cover lives on an overlay canvas under a scaled parent.
    /// Neither anchors nor offsets can express "this point of the other canvas", so the edge is
    /// placed through screen space every frame, which also follows resolution changes.</para>
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CoverEdgeToLine : MonoBehaviour
    {
        [Tooltip("Its pivot marks the line, e.g. the pond layer container whose pivot is the waterline.")]
        public RectTransform line;

        private RectTransform rect;
        private Canvas lineCanvas;
        private Vector2 serializedOffsetMin;
        private bool captured;

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            if (!captured)
            {
                serializedOffsetMin = rect.offsetMin;
                captured = true;
            }

            lineCanvas = line != null ? line.GetComponentInParent<Canvas>() : null;
            Match();
        }

        private void OnDisable()
        {
            // Maps without the pond keep the cover exactly as it was authored.
            if (rect != null && captured)
            {
                rect.offsetMin = serializedOffsetMin;
            }
        }

        private void LateUpdate()
        {
            Match();
        }

        private void Match()
        {
            if (rect == null || line == null || lineCanvas == null || rect.parent == null)
            {
                return;
            }

            Camera lineCamera = lineCanvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : lineCanvas.rootCanvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(lineCamera, line.position);

            var parent = (RectTransform)rect.parent;
            Canvas ownCanvas = rect.GetComponentInParent<Canvas>();
            Camera ownCamera = ownCanvas == null || ownCanvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : ownCanvas.rootCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, ownCamera, out Vector2 local))
            {
                return;
            }

            // Bottom edge in parent space = parent bottom + anchorMin.y * parent height + offsetMin.y.
            Rect parentRect = parent.rect;
            float anchorBottom = parentRect.yMin + rect.anchorMin.y * parentRect.height;
            float offset = local.y - anchorBottom;
            if (Mathf.Abs(rect.offsetMin.y - offset) > 0.01f)
            {
                rect.offsetMin = new Vector2(rect.offsetMin.x, offset);
            }
        }
    }
}
