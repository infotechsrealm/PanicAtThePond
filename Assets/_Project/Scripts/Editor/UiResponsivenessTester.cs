using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Renders a Canvas at a spread of real device aspect ratios and writes a PNG for each, so UI
    /// can be checked on shapes of screen this machine does not have.
    /// </summary>
    /// <remarks>
    /// <para><b>Why aspect ratio and not resolution.</b> A <c>CanvasScaler</c> set to
    /// ScaleWithScreenSize already handles a change of <i>size</i> — 1280x720 and 1920x1080 are the
    /// same layout at different zoom. What it cannot hide is a change of <i>shape</i>: a stretched
    /// anchor, a background whose sprite aspect does not match the screen, or an element parked at
    /// an absolute offset that falls off a narrower screen. The sizes below span 4:3 to 21:9.</para>
    ///
    /// <para><b>Why it renders through its own camera.</b> The Game view's render texture is not
    /// reachable in this Unity version, and a plain camera render does not contain a
    /// ScreenSpaceOverlay canvas at all. Temporarily driving the canvas as ScreenSpaceCamera into a
    /// RenderTexture of the wanted size reproduces exactly what the layout does at that size, since
    /// <c>CanvasScaler</c> and every RectTransform derive from the canvas rect. The canvas is put
    /// back afterwards.</para>
    ///
    /// <para>Only the UI is drawn, with a flat backdrop. That is deliberate: a pond behind the menu
    /// makes it harder, not easier, to see that a panel is the wrong shape.</para>
    /// </remarks>
    public static class UiResponsivenessTester
    {
        /// <summary>Name, width, height — ordered narrow to wide.</summary>
        public static readonly (string Name, int W, int H)[] Devices =
        {
            ("1_4-3_1024x768",     1024,  768),   // iPad / older tablets  (1.33)
            ("2_16-10_1280x800",   1280,  800),   // common laptop         (1.60)
            ("3_16-9_1920x1080",   1920, 1080),   // the authoring target  (1.78)
            ("4_19.5-9_2340x1080", 2340, 1080),   // modern phone          (2.17)
            ("5_21-9_2560x1080",   2560, 1080),   // ultrawide             (2.37)
        };

        private const string OutputRoot = "TestBuilds/Logs/ui";

        /// <summary>
        /// Captures every device size for <paramref name="canvas"/>, writing
        /// <c>Build/TestLogs/ui/&lt;label&gt;__&lt;device&gt;.png</c>. Returns the output directory.
        /// </summary>
        public static string Capture(Canvas canvas, string label)
        {
            string dir = Path.GetFullPath(OutputRoot);
            Directory.CreateDirectory(dir);

            if (canvas == null)
            {
                Debug.LogError("[UiResponsivenessTester] no canvas given");
                return dir;
            }

            RenderMode originalMode = canvas.renderMode;
            Camera originalCam = canvas.worldCamera;
            float originalPlane = canvas.planeDistance;

            var camGo = new GameObject("~UiCaptureCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.13f, 0.18f, 1f);
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            cam.cullingMask = ~0;
            camGo.transform.position = new Vector3(0f, 0f, -500f);

            var written = new List<string>();
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 100f;

                foreach ((string name, int w, int h) in Devices)
                {
                    var rt = new RenderTexture(w, h, 24) { useMipMap = false };
                    cam.targetTexture = rt;

                    Canvas.ForceUpdateCanvases();
                    cam.Render();
                    Canvas.ForceUpdateCanvases();
                    cam.Render(); // second pass: the first sizes the canvas, the second draws it

                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    RenderTexture.active = previous;

                    string file = Path.Combine(dir, $"{label}__{name}.png");
                    File.WriteAllBytes(file, tex.EncodeToPNG());
                    written.Add(Path.GetFileName(file));

                    Object.DestroyImmediate(tex);
                    cam.targetTexture = null;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
            finally
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCam;
                canvas.planeDistance = originalPlane;
                Object.DestroyImmediate(camGo);
                Canvas.ForceUpdateCanvases();
            }

            Debug.Log($"[UiResponsivenessTester] {written.Count} capture(s) -> {dir}\n  "
                + string.Join("\n  ", written));
            return dir;
        }

        /// <summary>
        /// Reports UI that is likely to misbehave when the screen changes shape. Static analysis, so
        /// it flags candidates rather than proving a fault — the captures decide.
        /// </summary>
        public static string Audit(Canvas canvas)
        {
            var sb = new System.Text.StringBuilder();
            if (canvas == null)
            {
                return "no canvas";
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            sb.AppendLine($"canvas '{canvas.name}' mode={canvas.renderMode}"
                + (scaler == null
                    ? " (no CanvasScaler)"
                    : $" scaler={scaler.uiScaleMode} ref={scaler.referenceResolution.x:0}x{scaler.referenceResolution.y:0}"
                      + $" match={scaler.matchWidthOrHeight:0.##} screenMatch={scaler.screenMatchMode}"));

            foreach (Image img in canvas.GetComponentsInChildren<Image>(true))
            {
                var rt = img.rectTransform;
                bool stretchesX = !Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x);
                bool stretchesY = !Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y);
                if (!stretchesX && !stretchesY)
                {
                    continue; // point-anchored: keeps its authored size, cannot be stretched by shape
                }

                // Sliced and tiled sprites are designed to be stretched; simple ones are not.
                if (img.type != Image.Type.Simple || img.sprite == null)
                {
                    continue;
                }

                if (img.preserveAspect || img.GetComponent<AspectRatioFitter>() != null)
                {
                    continue; // already protected
                }

                float spriteAspect = img.sprite.rect.width / img.sprite.rect.height;
                sb.AppendLine($"  STRETCH RISK  {HierarchyPath(img.transform)}"
                    + $"\n      sprite '{img.sprite.name}' {img.sprite.rect.width:0}x{img.sprite.rect.height:0}"
                    + $" (aspect {spriteAspect:0.###}) stretched on {(stretchesX && stretchesY ? "both axes" : stretchesX ? "X" : "Y")}"
                    + " with preserveAspect off and no AspectRatioFitter");
            }

            return sb.ToString();
        }

        public static string HierarchyPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }

            return sb.ToString();
        }
    }
}
