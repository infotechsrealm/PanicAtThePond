using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Shop
{
    /// <summary>
    /// Draws a v2 hat as one more layer of the fisherman rig, showing whichever hat frame matches
    /// the head frame currently on screen.
    /// </summary>
    /// <remarks>
    /// <para><b>This is what replaces the per-hat placement table.</b> A v2 hat sheet is authored on
    /// the same 320x320 canvas, in the same row and frame order, as the head it sits on — so the
    /// correct frame is already in the correct place. There is no offset, no rotation and no scale
    /// to tune: the hat is composited at the rig's origin exactly like the boat, arms and hair.</para>
    ///
    /// <para><b>Why mirror the head rather than key the clips.</b> Baking the hat into the 26
    /// animation clips would work, but the hat is a cosmetic the player swaps at will — the clip
    /// would then have to be rewritten per hat, which is the multiplication the layer system exists
    /// to avoid. Reading the head layer's current sprite instead keeps one set of clips for every
    /// hat, and a new hat is a single PNG with no code and no clip edits.</para>
    ///
    /// <para><b>Frames are matched by name, not by index.</b> The head sprite is
    /// <c>Head_Idle_L_2</c>; the hat sprite is <c>SodaHat_Idle_L_2</c>. Matching on the shared
    /// <c>Idle_L_2</c> suffix means a hat sheet lines up as long as it follows the naming, and a
    /// mismatch shows up as a missing hat on that frame rather than as a silently wrong frame.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FishermanHatLayer : MonoBehaviour
    {
        /// <summary>The head layer whose frame this hat follows.</summary>
        public SpriteRenderer headRenderer;

        /// <summary>This layer's own renderer.</summary>
        public SpriteRenderer hatRenderer;

        private readonly Dictionary<string, Sprite> framesBySuffix = new Dictionary<string, Sprite>();
        private string lastHeadSpriteName;

        /// <summary>
        /// Points this layer at a hat sheet. Passing null or an empty set clears the hat.
        /// </summary>
        public void SetSheet(IEnumerable<Sprite> sheet)
        {
            framesBySuffix.Clear();
            lastHeadSpriteName = null;

            if (sheet != null)
            {
                foreach (Sprite sprite in sheet)
                {
                    if (sprite == null)
                    {
                        continue;
                    }

                    string suffix = FrameSuffix(sprite.name);
                    if (!string.IsNullOrEmpty(suffix))
                    {
                        framesBySuffix[suffix] = sprite;
                    }
                }
            }

            if (hatRenderer != null)
            {
                hatRenderer.sprite = null;
                hatRenderer.enabled = framesBySuffix.Count > 0;
            }

            Sync();
        }

        /// <summary>True when a sheet is loaded and this layer is drawing a hat.</summary>
        public bool HasSheet => framesBySuffix.Count > 0;

        /// <summary>
        /// Everything after the first underscore: <c>Head_Idle_L_2</c> and <c>SodaHat_Idle_L_2</c>
        /// both reduce to <c>Idle_L_2</c>.
        /// </summary>
        private static string FrameSuffix(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            int underscore = spriteName.IndexOf('_');
            return underscore < 0 || underscore == spriteName.Length - 1
                ? null
                : spriteName.Substring(underscore + 1);
        }

        // LateUpdate, so the animator has already written this frame's head sprite.
        private void LateUpdate()
        {
            Sync();
        }

        private void Sync()
        {
            if (hatRenderer == null || headRenderer == null || framesBySuffix.Count == 0)
            {
                return;
            }

            Sprite headSprite = headRenderer.sprite;
            if (headSprite == null)
            {
                hatRenderer.sprite = null;
                return;
            }

            // The head sprite only changes on a keyframe; skip the dictionary lookup in between.
            if (headSprite.name == lastHeadSpriteName)
            {
                MatchHeadVisibility();
                return;
            }

            lastHeadSpriteName = headSprite.name;

            string suffix = FrameSuffix(headSprite.name);
            hatRenderer.sprite = suffix != null && framesBySuffix.TryGetValue(suffix, out Sprite frame)
                ? frame
                : null;

            MatchHeadVisibility();
        }

        /// <summary>
        /// Keeps the hat with the head when the rig hides itself — the fisherman is hidden outright
        /// during parts of the role transition, and a hat left behind would float over the pond.
        /// </summary>
        private void MatchHeadVisibility()
        {
            if (hatRenderer.flipX != headRenderer.flipX)
            {
                hatRenderer.flipX = headRenderer.flipX;
            }

            if (hatRenderer.flipY != headRenderer.flipY)
            {
                hatRenderer.flipY = headRenderer.flipY;
            }

            bool shouldDraw = headRenderer.enabled && hatRenderer.sprite != null;
            if (hatRenderer.enabled != shouldDraw)
            {
                hatRenderer.enabled = shouldDraw;
            }
        }
    }
}
