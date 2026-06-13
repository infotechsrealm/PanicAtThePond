using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SceneObjectImageSaver : MonoBehaviour
{
    [Header("Capture Target")]
    [SerializeField] private Transform targetRoot;
    [SerializeField] private Camera captureCamera;

    [Header("Output")]
    [SerializeField] private string fileName = "saved_character.png";
    [SerializeField] private int imageSize = 100;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private float padding = 1.15f;

    [Header("Testing")]
    [SerializeField] private bool saveOnStart;
    [SerializeField] private KeyCode saveKey = KeyCode.P;

    private const int CaptureLayer = 31;

    private void Start()
    {
        if (saveOnStart)
        {
            SaveImage();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            SaveImage();
        }
    }

    [ContextMenu("Save Image")]
    public void SaveImage()
    {
        if (targetRoot == null)
        {
            Debug.LogWarning("SceneObjectImageSaver needs a targetRoot assigned.");
            return;
        }

        int size = Mathf.Max(1, imageSize);
        RenderTexture renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Camera cameraToUse = GetOrCreateCamera();
        CameraState cameraState = new CameraState(cameraToUse);
        List<LayerState> layerStates = MoveTargetToCaptureLayer(targetRoot);

        try
        {
            Bounds bounds = CalculateTargetBounds(targetRoot);
            ConfigureCamera(cameraToUse, bounds, renderTexture);

            RenderTexture previousActive = RenderTexture.active;
            cameraToUse.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;

            string path = SaveTexture(texture);
            Debug.Log("Saved 100x100 image to: " + path);
        }
        finally
        {
            RestoreLayers(layerStates);
            cameraState.Restore(cameraToUse);

            renderTexture.Release();
            Destroy(renderTexture);
            Destroy(texture);
        }
    }

    private Camera GetOrCreateCamera()
    {
        if (captureCamera != null)
        {
            return captureCamera;
        }

        GameObject cameraObject = new GameObject("Scene Object Capture Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        captureCamera = cameraObject.AddComponent<Camera>();
        captureCamera.enabled = false;
        return captureCamera;
    }

    private void ConfigureCamera(Camera cameraToUse, Bounds bounds, RenderTexture renderTexture)
    {
        Vector3 center = bounds.center;
        float largestSize = Mathf.Max(bounds.size.x, bounds.size.y, 0.01f);

        cameraToUse.orthographic = true;
        cameraToUse.orthographicSize = largestSize * padding * 0.5f;
        cameraToUse.clearFlags = CameraClearFlags.SolidColor;
        cameraToUse.backgroundColor = backgroundColor;
        cameraToUse.cullingMask = 1 << CaptureLayer;
        cameraToUse.targetTexture = renderTexture;
        cameraToUse.transform.position = new Vector3(center.x, center.y, center.z - 10f);
        cameraToUse.transform.rotation = Quaternion.identity;
        cameraToUse.nearClipPlane = 0.01f;
        cameraToUse.farClipPlane = 100f;
    }

    private Bounds CalculateTargetBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private string SaveTexture(Texture2D texture)
    {
        string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "saved_character.png" : fileName;
        if (!safeFileName.EndsWith(".png"))
        {
            safeFileName += ".png";
        }

        string path = Path.Combine(Application.persistentDataPath, safeFileName);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        return path;
    }

    private static List<LayerState> MoveTargetToCaptureLayer(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        List<LayerState> states = new List<LayerState>(children.Length);

        foreach (Transform child in children)
        {
            states.Add(new LayerState(child.gameObject, child.gameObject.layer));
            child.gameObject.layer = CaptureLayer;
        }

        return states;
    }

    private static void RestoreLayers(List<LayerState> states)
    {
        foreach (LayerState state in states)
        {
            if (state.GameObject != null)
            {
                state.GameObject.layer = state.Layer;
            }
        }
    }

    private struct LayerState
    {
        public LayerState(GameObject gameObject, int layer)
        {
            GameObject = gameObject;
            Layer = layer;
        }

        public GameObject GameObject { get; }
        public int Layer { get; }
    }

    private struct CameraState
    {
        private readonly bool orthographic;
        private readonly float orthographicSize;
        private readonly CameraClearFlags clearFlags;
        private readonly Color backgroundColor;
        private readonly int cullingMask;
        private readonly RenderTexture targetTexture;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly float nearClipPlane;
        private readonly float farClipPlane;

        public CameraState(Camera camera)
        {
            orthographic = camera.orthographic;
            orthographicSize = camera.orthographicSize;
            clearFlags = camera.clearFlags;
            backgroundColor = camera.backgroundColor;
            cullingMask = camera.cullingMask;
            targetTexture = camera.targetTexture;
            position = camera.transform.position;
            rotation = camera.transform.rotation;
            nearClipPlane = camera.nearClipPlane;
            farClipPlane = camera.farClipPlane;
        }

        public void Restore(Camera camera)
        {
            camera.orthographic = orthographic;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = clearFlags;
            camera.backgroundColor = backgroundColor;
            camera.cullingMask = cullingMask;
            camera.targetTexture = targetTexture;
            camera.transform.position = position;
            camera.transform.rotation = rotation;
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
        }
    }
}
