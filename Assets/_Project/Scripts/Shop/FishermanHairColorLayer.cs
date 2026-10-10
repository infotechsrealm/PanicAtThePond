using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Shop
{
    /// <summary>
    /// Shows the v2 fisherman's hair in the selected colour by swapping the hair layer's frame for
    /// the same frame of the other colour's sheet.
    /// </summary>
    /// <remarks>
    /// <para><b>Why not a second animator.</b> The legacy hair colours were whole animator
    /// controllers (<c>ANIM_FisherManBlackHair</c>), built for the old single-sprite fisherman.
    /// Putting one on the v2 rig drives only the root renderer, with old 64x64 art, and freezes every
    /// v2 layer on its first frame: the fisherman turned into the old body under a still v2 head,
    /// and the hair stayed red. The v2 rig has one controller for every look; hair colour is a
    /// property of one layer, exactly like the hat in <see cref="FishermanHatLayer"/>.</para>
    ///
    /// <para><b>Matching.</b> Frames are matched by the shared suffix after the first underscore:
    /// <c>HairRed_Idle_L_2</c> and <c>HairBlack_Idle_L_2</c> both reduce to <c>Idle_L_2</c>. Both
    /// sheets are serialized here so they ship in a build: nothing else references the black one.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FishermanHairColorLayer : MonoBehaviour
    {
        public SpriteRenderer hairRenderer;
        public Sprite[] redFrames = new Sprite[0];
        public Sprite[] blackFrames = new Sprite[0];

        [Tooltip("The rig's own controller. A legacy hair or hat controller is replaced by it.")]
        public RuntimeAnimatorController rigController;

        private bool black;
        private Dictionary<string, Sprite> redBySuffix;
        private Dictionary<string, Sprite> blackBySuffix;

        public bool IsBlack => black;

        public void SetBlack(bool value)
        {
            black = value;
            Sync();
        }

        /// <summary>
        /// Puts the rig's own controller back if something swapped a legacy one in.
        /// </summary>
        public void RestoreRigController(Animator animator)
        {
            if (animator != null && rigController != null && animator.runtimeAnimatorController != rigController)
            {
                animator.runtimeAnimatorController = rigController;
            }
        }

        // LateUpdate, so the animator has already written this frame's (red) hair sprite.
        private void LateUpdate()
        {
            Sync();
        }

        private void Sync()
        {
            if (hairRenderer == null || hairRenderer.sprite == null)
            {
                return;
            }

            if (redBySuffix == null)
            {
                redBySuffix = BySuffix(redFrames);
                blackBySuffix = BySuffix(blackFrames);
            }

            string suffix = FrameSuffix(hairRenderer.sprite.name);
            Dictionary<string, Sprite> wanted = black ? blackBySuffix : redBySuffix;
            if (suffix != null && wanted.TryGetValue(suffix, out Sprite frame) && hairRenderer.sprite != frame)
            {
                hairRenderer.sprite = frame;
            }
        }

        private static Dictionary<string, Sprite> BySuffix(Sprite[] frames)
        {
            var map = new Dictionary<string, Sprite>();
            if (frames == null)
            {
                return map;
            }

            foreach (Sprite sprite in frames)
            {
                string suffix = sprite != null ? FrameSuffix(sprite.name) : null;
                if (suffix != null)
                {
                    map[suffix] = sprite;
                }
            }

            return map;
        }

        private static string FrameSuffix(string spriteName)
        {
            int underscore = spriteName.IndexOf('_');
            return underscore < 0 || underscore == spriteName.Length - 1 ? null : spriteName.Substring(underscore + 1);
        }
    }
}
