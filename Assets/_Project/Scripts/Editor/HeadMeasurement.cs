using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Locates the character's head inside a sprite by reading the PNG on disk.
    /// </summary>
    /// <remarks>
    /// <para><b>Why "topmost wide-enough run" is not good enough.</b> The first version of this rule
    /// took the first row from the top whose widest continuous opaque run was ≥ 6 px, on the reasoning
    /// that the fishing rod and line are only a pixel or two wide. Measuring the actual art showed the
    /// rod is <b>exactly 6 px</b> in the casting frames, so the rule locked onto it and reported the
    /// rod tip as the head — on all four <c>AC_CastingLeft</c> frames the "head" was found at rows
    /// 3–9 with centres swinging between x=18 and x=61, while the real head sat still at row 11–12,
    /// x≈30.</para>
    ///
    /// <para>Raising the threshold does not fix it. <c>CastingLeft3</c> has an 18 px arm-and-rod run
    /// above the head, and <c>CastingLeft2</c> has an 11 px rod run — the same width as the head — at
    /// x=58. Width alone and position alone are both ambiguous; together they are decisive.</para>
    ///
    /// <para>So the head is measured once on the character's rest sprite, where nothing overlaps it,
    /// and every other frame must match that reference in <b>both</b> width and horizontal position.
    /// Calibrating per character rather than hard-coding numbers keeps this working for the fish, and
    /// for whatever the art is replaced with.</para>
    /// </remarks>
    public static class HeadMeasurement
    {
        /// <summary>Shortest run considered structural rather than a stray pixel or an outline.</summary>
        private const int MinStructuralRun = 3;

        /// <summary>
        /// How far a candidate's width may differ from the reference head, as a fraction of it.
        /// The top row of the head is very consistent frame to frame; this only absorbs a pixel of
        /// anti-aliasing either way.
        /// </summary>
        private const float WidthTolerance = 0.35f;

        /// <summary>
        /// How far a candidate may sit from the reference head centre, as a fraction of sprite width.
        /// The head genuinely translates — up to 4 px in <c>AC_FightingLeft</c> — so this cannot be
        /// tight, but the rod runs are 10–30 px away and stay comfortably outside it.
        /// </summary>
        private const float CentreTolerance = 0.125f;

        /// <summary>
        /// How far the head may sit from the reference row, as a fraction of sprite height. Bounds
        /// the damage when nothing near the top matches: without it the scan keeps walking down and
        /// can report a point on the body as the head.
        /// </summary>
        private const float RowTolerance = 0.25f;

        /// <summary>Where the head sits in one sprite. <see cref="Row"/> is -1 when nothing matched.</summary>
        public struct Band
        {
            /// <summary>Rows from the TOP of the sprite down to the first row of head.</summary>
            public int Row;

            /// <summary>Centre of the head run, in pixels from the left edge of the sprite.</summary>
            public float CentreX;

            /// <summary>Width of the head run in pixels.</summary>
            public int Width;

            public bool Found => Row >= 0;

            public static Band None => new Band { Row = -1 };

            public override string ToString() =>
                Found ? $"row={Row} cx={CentreX:0.0} w={Width}" : "not found";
        }

        /// <summary>
        /// Measures the head on a sprite known to be unobstructed — the character's rest frame — to
        /// establish what that character's head looks like.
        /// </summary>
        public static Band Calibrate(Sprite sprite, Dictionary<string, Texture2D> cache)
        {
            Texture2D texture = Load(sprite, cache);
            if (texture == null)
            {
                return Band.None;
            }

            int width = (int)sprite.rect.width;
            int height = (int)sprite.rect.height;

            // On the rest frame the topmost structural run IS the head, so take it directly. Anything
            // narrower than a third of the sprite is skipped as a stray outline.
            int minimumWidth = Mathf.Max(MinStructuralRun + 1, width / 12);

            for (int row = 0; row < height; row++)
            {
                Band widest = Band.None;
                foreach (Band run in RunsInRow(texture, sprite, row))
                {
                    if (run.Width >= minimumWidth && (!widest.Found || run.Width > widest.Width))
                    {
                        widest = run;
                    }
                }

                if (widest.Found)
                {
                    return widest;
                }
            }

            return Band.None;
        }

        /// <summary>
        /// Finds the head in <paramref name="sprite"/>, requiring it to look like
        /// <paramref name="reference"/> in both width and horizontal position.
        /// </summary>
        public static Band Measure(Sprite sprite, Band reference, Dictionary<string, Texture2D> cache)
        {
            if (!reference.Found)
            {
                return Band.None;
            }

            Texture2D texture = Load(sprite, cache);
            if (texture == null)
            {
                return Band.None;
            }

            int height = (int)sprite.rect.height;
            float widthSlack = Mathf.Max(2f, reference.Width * WidthTolerance);
            float centreSlack = sprite.rect.width * CentreTolerance;

            // A head does not travel a quarter of the character between two frames of the same
            // animation. Without this bound the fish was catastrophically mis-measured: on
            // AC_Fish1Idel frame 3 nothing near the top matched — a fish is a smooth taper, so when
            // it tilts, the first row is already wider than the reference — and the scan ran on down
            // to row 32 of a 35 px sprite, latching onto the belly. The anchor then dropped 0.62
            // units and snapped back every cycle, which is what a hat "animating a lot in Y" is.
            float rowSlack = Mathf.Max(4f, height * RowTolerance);

            for (int row = 0; row < height; row++)
            {
                if (Mathf.Abs(row - reference.Row) > rowSlack)
                {
                    continue;
                }

                Band best = Band.None;
                foreach (Band run in RunsInRow(texture, sprite, row))
                {
                    if (Mathf.Abs(run.Width - reference.Width) > widthSlack)
                    {
                        continue;
                    }

                    if (Mathf.Abs(run.CentreX - reference.CentreX) > centreSlack)
                    {
                        continue;
                    }

                    // Several runs in one row can qualify; the one nearest the reference centre is
                    // the head, the others are limbs or tackle that happen to match its width.
                    if (!best.Found ||
                        Mathf.Abs(run.CentreX - reference.CentreX) < Mathf.Abs(best.CentreX - reference.CentreX))
                    {
                        best = run;
                    }
                }

                if (best.Found)
                {
                    return best;
                }
            }

            return Band.None;
        }

        /// <summary>Every continuous opaque run in one row, top-down.</summary>
        private static IEnumerable<Band> RunsInRow(Texture2D texture, Sprite sprite, int row)
        {
            var pixels = texture.GetPixels32();
            int rectX = (int)sprite.rect.x;
            int rectY = (int)sprite.rect.y;
            int rectW = (int)sprite.rect.width;
            int rectH = (int)sprite.rect.height;

            int textureY = rectY + rectH - 1 - row;   // sprite rows are bottom-up
            int run = 0;

            for (int x = 0; x <= rectW; x++)
            {
                bool solid = x < rectW && pixels[textureY * texture.width + (rectX + x)].a > 10;
                if (solid)
                {
                    run++;
                    continue;
                }

                if (run >= MinStructuralRun)
                {
                    yield return new Band { Row = row, Width = run, CentreX = (x - run) + run / 2f };
                }
                run = 0;
            }
        }

        /// <summary>
        /// Reads the sprite's texture from disk. Going via the file rather than
        /// <c>sprite.texture</c> avoids needing Read/Write enabled on 100+ textures.
        /// </summary>
        private static Texture2D Load(Sprite sprite, Dictionary<string, Texture2D> cache)
        {
            if (sprite == null || sprite.texture == null)
            {
                return null;
            }

            string path = AssetDatabase.GetAssetPath(sprite.texture);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            if (!cache.TryGetValue(path, out Texture2D texture))
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(path));
                cache[path] = texture;
            }

            return texture;
        }
    }
}
