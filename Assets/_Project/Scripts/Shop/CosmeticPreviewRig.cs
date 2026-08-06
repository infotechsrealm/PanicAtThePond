using UnityEngine;

namespace PanicAtThePond.Shop
{
    /// <summary>
    /// Renders the real fisherman / fish prefabs - base, hair and hat as separate GameObjects - into
    /// a <see cref="RenderTexture"/> the shop UI can display.
    ///
    /// <para><b>Why.</b> The shop previews were one hand-painted image per combination: 18 for the
    /// fisherman (2 hair x 8 hats) and 14 for the fish (2 species x 6 hats). Every new hat meant a
    /// new painting per hair colour and species, and because each was drawn by hand the character
    /// came out a different size in each - a measured 17% spread. Rendering the actual prefab
    /// removes both problems at once: cosmetics are applied by
    /// <see cref="CosmeticRuntimeApplier"/>, the same verified code the in-game character uses, so
    /// a new hat needs <b>no preview art at all</b>.</para>
    ///
    /// <para><b>Consistent size.</b> Framing is captured from the pristine prefab before any
    /// cosmetic is applied, so a tall hat extends past the framing instead of shrinking the
    /// character to fit. The body is therefore identical in every combination.</para>
    ///
    /// <para>The rig lives far from the play area so no gameplay camera can see it, and it renders
    /// on demand rather than every frame.</para>
    /// </summary>
    public sealed class CosmeticPreviewRig : MonoBehaviour
    {
        /// <summary>Where the rig is parked so no gameplay camera can see it.</summary>
        private static readonly Vector3 RigOrigin = new Vector3(10000f, 10000f, 0f);

        private const int TextureSize = 512;

        /// <summary>Fraction of the framed height the base character fills. Lower leaves more headroom for tall hats.</summary>
        private const float BaseFillFraction = 0.62f;

        private Camera _camera;
        private Transform _subjectRoot;
        private RenderTexture _texture;
        private GameObject _current;
        private string _currentKey;

        private Vector3 _frameCentre;
        private float _frameHalfHeight;
        private bool _hasFrame;

        /// <summary>The texture the UI should display. Valid after the first render.</summary>
        public RenderTexture Texture => _texture;

        private void Awake()
        {
            transform.position = RigOrigin;

            _texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "RT_CosmeticPreview",
                antiAliasing = 1
            };

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.targetTexture = _texture;
            _camera.enabled = false; // rendered on demand via Camera.Render()

            var subject = new GameObject("Subject");
            subject.transform.SetParent(transform, false);
            _subjectRoot = subject.transform;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
                _texture = null;
            }
        }

        /// <summary>
        /// Renders the fisherman wearing <paramref name="hatName"/> and <paramref name="hairName"/>.
        /// Either may be empty for the bare character.
        /// </summary>
        public void RenderFisherman(string hairName, string hatName)
        {
            string key = "FM|" + hairName + "|" + hatName;
            if (key == _currentKey)
            {
                return;
            }

            // The modular rig, not the flat "FisherMan" sprite: the cosmetic appliers place hair and
            // hats relative to a "head" child, which only this prefab has.
            if (!Rebuild("FisherMan (2) 1", key))
            {
                return;
            }

            // Frame on the bare prefab, then dress it.
            CaptureFraming();
            CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(_current, hatName, hairName);
            ForceModularRendering(hatName);
            FrameAndRender();
        }

        /// <summary>
        /// Renders the fish species at <paramref name="speciesIndex"/> wearing <paramref name="hatName"/>.
        /// </summary>
        public void RenderFish(int speciesIndex, string hatName)
        {
            string key = "FISH|" + speciesIndex + "|" + hatName;
            if (key == _currentKey)
            {
                return;
            }

            string prefab = speciesIndex == 1 ? "Fish 2" : "Fish";
            if (!Rebuild(prefab, key))
            {
                return;
            }

            // Species is part of the BASE, so it must be applied before framing is captured -
            // it swaps the body sprite and therefore the bounds. The hat comes after, so a tall
            // hat extends past the frame instead of shrinking the fish.
            CosmeticRuntimeApplier.ApplyFishSpeciesByIndex(_current, speciesIndex);
            CaptureFraming();
            CosmeticRuntimeApplier.ApplyFishHatByName(_current, hatName);
            FrameAndRender();
        }

        /// <summary>Forces the next Render call to rebuild even if the arguments are unchanged.</summary>
        public void Invalidate()
        {
            _currentKey = null;
        }

        private bool Rebuild(string prefabName, string key)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                return false;
            }

            if (_current != null)
            {
                // Immediate, not deferred: Destroy() runs at end of frame, so the previous subject
                // would still be present when we render below and both would appear at once.
                DestroyImmediate(_current);
            }

            _current = Instantiate(prefab, _subjectRoot);
            _current.transform.localPosition = Vector3.zero;
            _current.transform.localRotation = Quaternion.identity;
            StripNonVisualComponents(_current);

            _currentKey = key;
            return true;
        }

        /// <summary>
        /// Removes anything that would try to behave like a live game object - networking, physics,
        /// colliders and gameplay controllers - so a preview instance cannot join the simulation.
        /// </summary>
        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component is Transform || component is SpriteRenderer ||
                    component is UnityEngine.Rendering.SortingGroup)
                {
                    continue;
                }

                // Animators are stripped too. Selecting the default fishing hat makes the applier
                // swap the fisherman's whole animator controller; with no frame ticking before we
                // render, that left every SpriteRenderer blank and the preview came out empty.
                // A shop preview wants the prefab's authored pose, held still.
                DestroyImmediate(component);
            }
        }

        /// <summary>
        /// Guarantees the preview shows the modular character (head + body + hat as separate
        /// renderers) for every hat.
        ///
        /// <para>Most hats already render that way, but the default fishing hat takes a
        /// <em>pre-baked</em> path: the applier disables every modular part and enables the root
        /// renderer, whose sprite is supplied by an animator controller. With no animator running
        /// in a still preview that left the whole character blank. Forcing the modular form keeps
        /// all hats consistent and means a new hat never needs a pre-baked animation to show up
        /// in the shop.</para>
        /// </summary>
        private void ForceModularRendering(string hatName)
        {
            if (_current == null)
            {
                return;
            }

            SpriteRenderer root = _current.GetComponent<SpriteRenderer>();
            if (root != null)
            {
                root.enabled = false;
            }

            foreach (SpriteRenderer sr in _current.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr == root)
                {
                    continue;
                }

                sr.enabled = true;
            }

            // The pre-baked path leaves the hat child holding whatever the prefab shipped with, so
            // set it explicitly from the requested hat.
            Transform hatChild = FindDeep(_current.transform, "hat Cosmetic");
            if (hatChild != null && hatChild.TryGetComponent(out SpriteRenderer hatRenderer))
            {
                Sprite hatSprite = string.IsNullOrEmpty(hatName)
                    ? null
                    : CosmeticRuntimeApplier.GetSpriteByName(hatName);

                hatRenderer.sprite = hatSprite;
                hatRenderer.enabled = hatSprite != null;
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Records camera framing from the freshly instantiated, cosmetic-free prefab. The same
        /// prefab yields the same bounds every time, so every combination renders at the same size.
        /// </summary>
        private void CaptureFraming()
        {
            _hasFrame = false;
            if (_current == null)
            {
                return;
            }

            bool any = false;
            Bounds bounds = default;
            foreach (SpriteRenderer sr in _current.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null || sr.sprite == null)
                {
                    continue;
                }

                if (!any)
                {
                    bounds = sr.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(sr.bounds);
                }
            }

            if (!any)
            {
                return;
            }

            _frameCentre = bounds.center;

            // Fit BOTH axes inside the fill fraction. Framing on height alone made the fish - which
            // is wide and short - overflow horizontally and almost touch the texture edges.
            float fill = Mathf.Max(0.01f, BaseFillFraction);
            float halfForHeight = Mathf.Max(0.01f, bounds.size.y * 0.5f) / fill;
            float aspect = _camera != null && _camera.aspect > 0.01f ? _camera.aspect : 1f;
            float halfForWidth = Mathf.Max(0.01f, bounds.size.x * 0.5f) / fill / aspect;

            _frameHalfHeight = Mathf.Max(halfForHeight, halfForWidth);
            _hasFrame = true;
        }

        private void FrameAndRender()
        {
            if (!_hasFrame)
            {
                return;
            }

            _camera.orthographicSize = _frameHalfHeight;
            _camera.transform.position = new Vector3(_frameCentre.x, _frameCentre.y, _frameCentre.z - 10f);
            _camera.Render();
        }
    }
}
