using System;
using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.UI
{
    /// <summary>
    /// A background built from separate, independently swappable layers instead of one baked image.
    /// </summary>
    /// <remarks>
    /// <para><b>Why layers.</b> The pond is drawn as base, clouds, light rays, plants and the surface
    /// waves, each on the same 960x540 canvas so they register at 0,0. Keeping them apart is what
    /// makes themes cheap: recolouring the water or swapping the plants for a seasonal set is a
    /// change to one layer, not a redraw of every frame of a composited animation.</para>
    ///
    /// <para><b>The theme hook.</b> <see cref="SetFrames"/> and <see cref="SetTint"/> address a layer
    /// by its <see cref="Layer.id"/>, so cosmetics code never needs to know the hierarchy. The same
    /// layer set is also instanced inside the visibility-mode covers; a theme has to be applied to
    /// every instance (<see cref="ApplyToAll"/>) or a cover would show the old look.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LayeredBackground : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            [Tooltip("Stable name used by themes: Base, Clouds, LightRays, Plants, WaterTopWaves.")]
            public string id;
            public Image image;
            [Tooltip("Present on animated layers only.")]
            public AnimatedBackground animator;
        }

        public Layer[] layers = Array.Empty<Layer>();

        public Layer Find(string id)
        {
            foreach (Layer layer in layers)
            {
                if (layer != null && string.Equals(layer.id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return layer;
                }
            }

            return null;
        }

        /// <summary>
        /// Replaces a layer's art. One sprite makes it static; several play as an animation.
        /// Frames must be authored on the shared 960x540 canvas so the layer stays registered.
        /// </summary>
        public bool SetFrames(string id, Sprite[] frames)
        {
            Layer layer = Find(id);
            if (layer == null || layer.image == null || frames == null || frames.Length == 0)
            {
                return false;
            }

            if (layer.animator != null)
            {
                layer.animator.frames = frames;
                layer.animator.enabled = frames.Length > 1;
            }

            layer.image.sprite = frames[0];
            return true;
        }

        /// <summary>Multiplies a layer's colour, e.g. to recolour the water for a theme.</summary>
        public bool SetTint(string id, Color tint)
        {
            Layer layer = Find(id);
            if (layer == null || layer.image == null)
            {
                return false;
            }

            layer.image.color = tint;
            return true;
        }

        /// <summary>Applies the same change to every instance in the scene, covers included.</summary>
        public static void ApplyToAll(Action<LayeredBackground> change)
        {
            if (change == null)
            {
                return;
            }

            foreach (LayeredBackground background in FindObjectsByType<LayeredBackground>(FindObjectsInactive.Include))
            {
                change(background);
            }
        }
    }
}
