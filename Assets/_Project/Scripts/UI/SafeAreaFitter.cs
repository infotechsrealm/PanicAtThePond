using UnityEngine;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// Keeps a full-screen uGUI panel inside <see cref="Screen.safeArea"/> so content never sits
    /// under a notch, punch-hole, rounded corner or system bar.
    ///
    /// <para>Attach to a child of the root Canvas that wraps the screen's content, then parent the
    /// actual UI under it. The fitter drives its own anchors, so the object it sits on must not be
    /// positioned by hand.</para>
    ///
    /// <para>Re-applies whenever the safe area, resolution or orientation changes. On desktop the
    /// safe area equals the full screen, so this is a no-op there and costs one comparison a frame.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Apply the safe area horizontally (left/right notches, rounded corners).")]
        [SerializeField] private bool _applyHorizontal = true;

        [Tooltip("Apply the safe area vertically (status bar, home indicator).")]
        [SerializeField] private bool _applyVertical = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0f, 0f, 0f, 0f);
        private ScreenOrientation _lastOrientation;
        private Vector2Int _lastResolution;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            // Force a refresh on enable; the cached values may be stale after a scene change.
            _lastSafeArea = new Rect(0f, 0f, 0f, 0f);
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>
        /// Re-applies the current safe area if anything relevant changed. Safe to call manually
        /// after changing resolution programmatically.
        /// </summary>
        public void Refresh()
        {
            Rect safeArea = Screen.safeArea;
            ScreenOrientation orientation = Screen.orientation;
            Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);

            if (safeArea == _lastSafeArea
                && orientation == _lastOrientation
                && resolution == _lastResolution)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastOrientation = orientation;
            _lastResolution = resolution;

            Apply(safeArea, resolution);
        }

        private void Apply(Rect safeArea, Vector2Int resolution)
        {
            if (_rectTransform == null || resolution.x <= 0 || resolution.y <= 0)
            {
                return;
            }

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;

            min.x /= resolution.x;
            min.y /= resolution.y;
            max.x /= resolution.x;
            max.y /= resolution.y;

            // Guard against a driver reporting a degenerate safe area, which would collapse the UI.
            if (max.x - min.x <= 0f || max.y - min.y <= 0f)
            {
                return;
            }

            if (!_applyHorizontal)
            {
                min.x = 0f;
                max.x = 1f;
            }

            if (!_applyVertical)
            {
                min.y = 0f;
                max.y = 1f;
            }

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
