using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PanicAtThePond.UI;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Builds the layered pond background prefab and wires it into the Play scene.
    /// </summary>
    /// <remarks>
    /// <para>Safe to re-run: the prefab is rebuilt from the sprite sheets, and the scene instances
    /// are replaced rather than duplicated.</para>
    ///
    /// <para><b>Fit.</b> Every layer shares one container with an <see cref="AspectRatioFitter"/> in
    /// <c>EnvelopeParent</c> mode at 16:9 — cover, never stretch. The container's pivot sits on the
    /// waterline rather than the centre, so when a wide screen makes the art grow, it grows away from
    /// the waterline instead of moving it. That matters because the camera's vertical extent is fixed
    /// and the boat, the fish and the water collider are placed in world units against that line;
    /// pivoting on the centre would slide the surface relative to them on every ultrawide screen.</para>
    /// </remarks>
    public static class PondBackgroundBuilder
    {
        private const string ArtFolder = "Assets/_Project/Art/Sprites/Backgrounds/Pond/";
        private const string PrefabPath = "Assets/_Project/Prefabs/Environment/PondBackgroundLayers.prefab";
        private const string PlayScene = "Assets/_Project/Scenes/Play.unity";
        private const string CoverViewName = "PondCoverView";
        private const string LegacyCoverViewName = "LegacyCoverView";

        /// <summary>First row of deep water in the 540px-tall art, measured: 260.</summary>
        private const float WaterlinePivotY = 1f - 260f / 540f;

        private const float FramesPerSecond = 10f;

        [MenuItem("Panic At The Pond/Rebuild Pond Background")]
        public static void BuildAll()
        {
            GameObject prefab = BuildPrefab();
            WireScene(prefab);
        }

        public static GameObject BuildPrefab()
        {
            var root = new GameObject("PondBackgroundLayers", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, WaterlinePivotY);
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                var fitter = root.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = 960f / 540f;

                var layered = root.AddComponent<LayeredBackground>();
                var layers = new List<LayeredBackground.Layer>
                {
                    AddLayer(root, "Base", Single("Pond_Base.png")),
                    AddLayer(root, "Clouds", Single("Pond_Clouds.png")),
                    AddLayer(root, "LightRays", Sheet("Pond_LightRays_Sheet.png", "Pond_LightRays_")),
                    AddLayer(root, "Plants", Sheet("Pond_Plants_Sheet.png", "Pond_Plants_")),
                    AddLayer(root, "WaterTopWaves", Sheet("Pond_WaterTopWaves_Sheet.png", "Pond_WaterTopWaves_")),
                };
                layered.layers = layers.ToArray();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[PondBackgroundBuilder] prefab saved: {PrefabPath} ({layers.Count} layers)");
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static LayeredBackground.Layer AddLayer(GameObject root, string id, Sprite[] frames)
        {
            var go = new GameObject(id, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.sprite = frames.Length > 0 ? frames[0] : null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false; // the container already holds the art's exact aspect
            image.raycastTarget = false;  // scenery must never take clicks meant for gameplay or UI

            AnimatedBackground animator = null;
            if (frames.Length > 1)
            {
                animator = go.AddComponent<AnimatedBackground>();
                animator.targetImage = image;
                animator.frames = frames;
                animator.framesPerSecond = FramesPerSecond;
                animator.loop = true;
                animator.useUnscaledTime = true;
            }

            if (frames.Length == 0)
            {
                Debug.LogError($"[PondBackgroundBuilder] layer '{id}' has no sprites");
            }

            return new LayeredBackground.Layer { id = id, image = image, animator = animator };
        }

        private static Sprite[] Single(string file)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file);
            return s == null ? new Sprite[0] : new[] { s };
        }

        private static Sprite[] Sheet(string file, string prefix)
        {
            var found = new SortedDictionary<int, Sprite>();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ArtFolder + file))
            {
                if (o is Sprite s && s.name.StartsWith(prefix)
                    && int.TryParse(s.name.Substring(prefix.Length), out int index))
                {
                    found[index] = s;
                }
            }

            var frames = new Sprite[found.Count];
            found.Values.CopyTo(frames, 0);
            return frames;
        }

        public static void WireScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
            // GameManager is looked up by name: this Editor assembly does not reference Photon, which
            // GameManager's base class lives in, so the type cannot be named here.
            MonoBehaviour gm = null;
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb != null && mb.GetType().Name == "GameManager") { gm = mb; break; }
            }

            var so = gm == null ? null : new SerializedObject(gm);
            var mainBG = so?.FindProperty("mainBGImage").objectReferenceValue as Image;
            var sky = so?.FindProperty("sky").objectReferenceValue as GameObject;
            var water = so?.FindProperty("water").objectReferenceValue as GameObject;
            if (mainBG == null || sky == null || water == null)
            {
                Debug.LogError("[PondBackgroundBuilder] GameManager or its background/cover references are missing");
                return;
            }

            // 1) Behind the world, on the background canvas.
            Transform bgParent = mainBG.transform;
            RemoveChild(bgParent, prefab.name);
            var bgInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bgParent);
            bgInstance.name = prefab.name;

            // 2) and 3) Inside each visibility cover, masked to the cover, laid out against the screen.
            GameObject skyView = AddCoverView(sky, prefab);
            GameObject waterView = AddCoverView(water, prefab);

            // The fish-side cover's bottom edge follows the pond waterline (enabled by GameManager on
            // the pond map only, so other maps keep the authored edge).
            var skyEdge = sky.GetComponent<CoverEdgeToLine>();
            if (skyEdge == null)
            {
                skyEdge = sky.AddComponent<CoverEdgeToLine>();
            }

            skyEdge.line = (RectTransform)bgInstance.transform;
            skyEdge.enabled = false;

            // Maps without the pond: the sky cover's own image is transparent, so it carries a
            // full-screen copy of whichever background is selected (GameManager sets the sprite).
            RemoveChild(sky.transform, LegacyCoverViewName);
            var legacyView = new GameObject(LegacyCoverViewName, typeof(RectTransform), typeof(ScreenMatchRect));
            legacyView.transform.SetParent(sky.transform, false);
            var legacyImage = legacyView.AddComponent<Image>();
            legacyImage.sprite = mainBG.sprite;
            legacyImage.type = Image.Type.Simple;
            legacyImage.preserveAspect = false; // the main background is stretched across the screen too
            legacyImage.raycastTarget = false;
            legacyView.SetActive(false);

            // The fisherman-side cover clips fish sprites (hats stand above the body) to the area it
            // hides. It runs only while the cover is showing, on every map.
            if (water.GetComponent<CoverWorldClip>() == null)
            {
                water.AddComponent<CoverWorldClip>();
            }

            // Old world-space decoration that the pond layers now draw themselves. Only purely visual
            // groups: anything carrying a Collider2D (the Environment/Rocks) is gameplay and is kept.
            var decor = new List<GameObject>();
            Transform environment = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = root.name == "Environment" ? root.transform : root.transform.Find("Environment");
                if (found != null) { environment = found; break; }
            }

            if (environment != null)
            {
                foreach (Transform child in environment)
                {
                    if (child.GetComponentInChildren<Collider2D>(true) == null)
                    {
                        decor.Add(child.gameObject);
                    }
                }
            }

            so.FindProperty("pondBackgroundLayers").objectReferenceValue = bgInstance;
            so.FindProperty("pondSkyCoverLayers").objectReferenceValue = skyView;
            so.FindProperty("pondWaterCoverLayers").objectReferenceValue = waterView;
            so.FindProperty("legacySkyCoverImage").objectReferenceValue = legacyImage;
            SerializedProperty decorProp = so.FindProperty("legacyEnvironmentDecor");
            decorProp.arraySize = decor.Count;
            for (int i = 0; i < decor.Count; i++)
            {
                decorProp.GetArrayElementAtIndex(i).objectReferenceValue = decor[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PondBackgroundBuilder] Play scene wired: background + sky cover + water cover; "
                + $"legacy decor hidden on this map: {string.Join(", ", decor.ConvertAll(d => d.name))}");
        }

        private static GameObject AddCoverView(GameObject cover, GameObject prefab)
        {
            // The cover strip clips the full-screen layer copy to its own area.
            if (cover.GetComponent<RectMask2D>() == null)
            {
                cover.AddComponent<RectMask2D>();
            }

            RemoveChild(cover.transform, CoverViewName);
            var view = new GameObject(CoverViewName, typeof(RectTransform), typeof(ScreenMatchRect));
            view.transform.SetParent(cover.transform, false);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, view.transform);
            instance.name = prefab.name;
            return view;
        }

        private static void RemoveChild(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                if (parent.GetChild(i).name == name)
                {
                    Object.DestroyImmediate(parent.GetChild(i).gameObject);
                }
            }
        }
    }
}
