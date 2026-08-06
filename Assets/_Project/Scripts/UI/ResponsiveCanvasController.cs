using System;
using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.UI
{
    /// <summary>How the canvas reconciles the screen's aspect ratio with the reference resolution.</summary>
    public enum CanvasFitMode
    {
        /// <summary>Scale so the whole reference area always fits. UI is never cropped on any aspect ratio.</summary>
        ExpandToFit,

        /// <summary>Scale so the reference area always covers the screen. Edges of the UI may be cropped.</summary>
        FillScreen,

        /// <summary>Unity's default blend. Keeps whatever <c>matchWidthOrHeight</c> was authored.</summary>
        Authored
    }

    /// <summary>Logical width buckets used to drive layout changes without hardcoding device names.</summary>
    public enum LayoutBreakpoint
    {
        /// <summary>Under 600 logical px — phones in portrait.</summary>
        Compact,

        /// <summary>600–1023 logical px — large phones, small tablets.</summary>
        Medium,

        /// <summary>1024 logical px and up — tablets, desktop, console.</summary>
        Expanded
    }

    /// <summary>
    /// Makes a uGUI canvas behave consistently across resolutions, aspect ratios and DPIs.
    ///
    /// <para>Deliberately does <b>not</b> overwrite the authored reference resolution: content is
    /// laid out in that coordinate space, so changing it would rescale every element. Instead it
    /// drives <c>matchWidthOrHeight</c> per-frame from the current aspect ratio, which is what
    /// actually decides whether UI gets cropped on an unusual screen.</para>
    ///
    /// <para>Also publishes a <see cref="LayoutBreakpoint"/> so controllers can rearrange panels
    /// without polling <c>Screen.width</c> themselves.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasController : MonoBehaviour
    {
        private const float CompactMaxWidth = 600f;
        private const float MediumMaxWidth = 1024f;

        [Tooltip("Authored (default) keeps the scene's own matchWidthOrHeight, so the game looks " +
                 "exactly as authored. ExpandToFit guarantees no UI is ever cut off on unusual " +
                 "aspect ratios, but CHANGES the on-screen size wherever the reference resolution " +
                 "does not match the display: on an 800x600 reference at 1920x1080 it scales 1.80 " +
                 "instead of 2.08, i.e. 87% of authored size. Opt in per canvas, and re-check the " +
                 "layout after doing so.")]
        [SerializeField] private CanvasFitMode _fitMode = CanvasFitMode.Authored;

        [Tooltip("Forces Scale With Screen Size. Constant Pixel Size does not scale with DPI and " +
                 "should not be used for game UI.")]
        [SerializeField] private bool _forceScaleWithScreenSize = true;

        private CanvasScaler _scaler;
        private Vector2Int _lastResolution;
        private LayoutBreakpoint _breakpoint = LayoutBreakpoint.Expanded;

        /// <summary>The current logical-width bucket.</summary>
        public LayoutBreakpoint Breakpoint => _breakpoint;

        /// <summary>Raised when the breakpoint changes. Subscribe in OnEnable, unsubscribe in OnDisable.</summary>
        public event Action<LayoutBreakpoint> BreakpointChanged;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
        }

        private void OnEnable()
        {
            _lastResolution = Vector2Int.zero;
            Refresh();
        }

        private void OnDisable()
        {
            BreakpointChanged = null;
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>Recomputes scaling and breakpoint if the resolution changed.</summary>
        public void Refresh()
        {
            if (_scaler == null)
            {
                return;
            }

            Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);
            if (resolution == _lastResolution || resolution.x <= 0 || resolution.y <= 0)
            {
                return;
            }

            _lastResolution = resolution;

            if (_forceScaleWithScreenSize)
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                // Only take over the match mode when we are actually going to drive the match value.
                // Overwriting it in Authored mode would discard a scene's authored Expand/Shrink setting.
                if (_fitMode != CanvasFitMode.Authored)
                {
                    _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                }
            }

            ApplyFitMode(resolution);
            ApplyBreakpoint(resolution);
        }

        private void ApplyFitMode(Vector2Int resolution)
        {
            if (_fitMode == CanvasFitMode.Authored)
            {
                return;
            }

            Vector2 reference = _scaler.referenceResolution;
            if (reference.x <= 0f || reference.y <= 0f)
            {
                return;
            }

            float widthRatio = resolution.x / reference.x;
            float heightRatio = resolution.y / reference.y;

            // matchWidthOrHeight 0 scales by width, 1 scales by height. Picking the axis with the
            // smaller ratio fits the whole reference area on screen; the larger ratio fills it.
            bool useWidth = _fitMode == CanvasFitMode.ExpandToFit
                ? widthRatio < heightRatio
                : widthRatio > heightRatio;

            _scaler.matchWidthOrHeight = useWidth ? 0f : 1f;
        }

        private void ApplyBreakpoint(Vector2Int resolution)
        {
            float logicalWidth = _scaler.scaleFactor > 0f
                ? resolution.x / _scaler.scaleFactor
                : resolution.x;

            LayoutBreakpoint next;
            if (logicalWidth < CompactMaxWidth)
            {
                next = LayoutBreakpoint.Compact;
            }
            else if (logicalWidth < MediumMaxWidth)
            {
                next = LayoutBreakpoint.Medium;
            }
            else
            {
                next = LayoutBreakpoint.Expanded;
            }

            if (next == _breakpoint)
            {
                return;
            }

            _breakpoint = next;
            BreakpointChanged?.Invoke(next);
        }
    }
}
