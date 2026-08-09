using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using PanicAtThePond.Shop;

namespace PanicAtThePond.Tests.EditMode
{
    /// <summary>
    /// Regression tests for the hat/body animation desync.
    ///
    /// The shipping fisherman and fish animation clips name their frames <b>1-based</b>
    /// (<c>IdleLeft1</c>â€¦<c>IdleLeft4</c>, <c>Idle1</c>â€¦<c>Idle4</c>), but every cosmetic bob table in
    /// <see cref="CosmeticRuntimeApplier"/> is indexed 0-based. The old
    /// <c>GetCurrentSpriteFrameIndex</c> returned <c>trailingNumber % 4</c>, which read each table one
    /// frame late and wrapped the final frame back onto the first â€” so the hat travelled against the
    /// head instead of with it. These tests fail on the old implementation and pass on the fix.
    /// </summary>
    [TestFixture]
    public sealed class CosmeticFrameIndexTests
    {
        private const int SpriteWidth = 8;
        private const int SpriteHeight = 8;

        private GameObject _host;
        private CosmeticRuntimeApplier _applier;
        private SpriteRenderer _rootRenderer;
        private MethodInfo _getFrameIndex;
        private Texture2D _texture;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("CosmeticFrameIndexTestHost");
            _rootRenderer = _host.AddComponent<SpriteRenderer>();

            GameObject cosmetic = new GameObject("cosmetic");
            cosmetic.transform.SetParent(_host.transform, false);
            _applier = cosmetic.AddComponent<CosmeticRuntimeApplier>();

            _texture = new Texture2D(SpriteWidth, SpriteHeight);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            typeof(CosmeticRuntimeApplier)
                .GetField("rootRenderer", flags)
                .SetValue(_applier, _rootRenderer);

            _getFrameIndex = typeof(CosmeticRuntimeApplier).GetMethod("GetCurrentSpriteFrameIndex", flags);
            Assert.That(_getFrameIndex, Is.Not.Null, "GetCurrentSpriteFrameIndex was renamed or removed.");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_texture);
        }

        private int FrameIndexFor(string spriteName)
        {
            Sprite sprite = Sprite.Create(
                _texture,
                new Rect(0f, 0f, SpriteWidth, SpriteHeight),
                new Vector2(0.5f, 0.5f));
            sprite.name = spriteName;
            _rootRenderer.sprite = sprite;
            return (int)_getFrameIndex.Invoke(_applier, null);
        }

        // --- 1-based animation frame names -> 0-based table columns -------------------------------

        [TestCase("IdleLeft1", 0)]
        [TestCase("IdleLeft2", 1)]
        [TestCase("IdleLeft3", 2)]
        [TestCase("IdleLeft4", 3)]
        [TestCase("CastingRight1", 0)]
        [TestCase("CastingRight4", 3)]
        [TestCase("MoveReverseBackwards2", 1)]
        [TestCase("RightPoleToOarFacingRight3", 2)]
        public void FishermanClipFrames_MapToZeroBasedColumn(string spriteName, int expected)
        {
            Assert.That(FrameIndexFor(spriteName), Is.EqualTo(expected));
        }

        [TestCase("Idle1", 0)]
        [TestCase("Idle2", 1)]
        [TestCase("Idle3", 2)]
        [TestCase("Idle4", 3)]
        [TestCase("Move1", 0)]
        [TestCase("Move4", 3)]
        [TestCase("Eat1", 0)]
        [TestCase("Fight4", 3)]
        public void FishClipFrames_MapToZeroBasedColumn(string spriteName, int expected)
        {
            Assert.That(FrameIndexFor(spriteName), Is.EqualTo(expected));
        }

        /// <summary>
        /// The last frame of a cycle must never wrap onto column 0 â€” that wrap is what made the hat
        /// snap back to its rest position while the body was still at the bottom of its bob.
        /// </summary>
        [Test]
        public void LastFrameOfCycle_DoesNotWrapToFirstColumn()
        {
            Assert.That(FrameIndexFor("IdleLeft4"), Is.Not.EqualTo(FrameIndexFor("IdleLeft1")));
            Assert.That(FrameIndexFor("Idle4"), Is.Not.EqualTo(FrameIndexFor("Idle1")));
        }

        // --- 0-based sheet slice names keep their existing meaning --------------------------------

        [TestCase("FishermansAnimations-GreenBody_Sheet_0", 0)]
        [TestCase("FishermansAnimations-GreenBody_Sheet_1", 1)]
        [TestCase("FishermansAnimations-GreenBody_Sheet_4", 0)]
        [TestCase("FishermansAnimations-GreenBody_Sheet_6", 2)]
        [TestCase("FishermansAnimations-GreenBody_Sheet_7", 3)]
        [TestCase("FishermansAnimations-Head_Sheet_43", 3)]
        public void SheetSliceNames_StayZeroBased(string spriteName, int expected)
        {
            Assert.That(FrameIndexFor(spriteName), Is.EqualTo(expected));
        }

        [Test]
        public void SpriteWithoutTrailingNumber_ReturnsFirstColumn()
        {
            Assert.That(FrameIndexFor("SomeSpriteWithNoNumber"), Is.EqualTo(0));
        }

        [Test]
        public void NullSprite_ReturnsFirstColumn()
        {
            _rootRenderer.sprite = null;
            Assert.That((int)_getFrameIndex.Invoke(_applier, null), Is.EqualTo(0));
        }

        // --- the bob must travel with the head, never against it ---------------------------------

        /// <summary>
        /// Measured head-centre Y for the idle-left row of FishermansAnimations-Head_Sheet.png
        /// (top-down pixels, so a smaller number is higher on screen): 20, 19, 21, 21. The hat's
        /// vertical offset must therefore rise on column 1 and fall on columns 2 and 3.
        /// </summary>
        // --- fish and fisherman hat slots must never cross-contaminate --------------------------

        /// <summary>
        /// A real save was found containing
        /// <c>SelectedFishHatCosmetic = FisherMan_Hat_-Default_-_Fishing_Hat</c> â€” a fisherman hat in
        /// the fish slot, which can never resolve to a valid fish cosmetic, so the fish rendered bare.
        /// </summary>
        [TestCase("FisherMan_Hat_-Default_-_Fishing_Hat", true)]
        [TestCase("FisherMan_Hat_-Blue_Cap", true)]
        [TestCase("FisherMan_Hat_-Chef_Hat", true)]
        [TestCase("FisherMan_Hat_-Soda_Hat", true)]
        [TestCase("TurtleHat", true)]
        [TestCase("cap", false)]
        [TestCase("hat", false)]
        [TestCase("hat2", false)]
        [TestCase("beret", false)]
        [TestCase("paper_boat", false)]
        public void CosmeticCategory_IsDetectedFromShopConfig(string spriteName, bool expectedFisherman)
        {
            MethodInfo method = typeof(CosmeticRuntimeApplier)
                .GetMethod("IsFishermanCategorySprite", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "IsFishermanCategorySprite was renamed or removed.");

            bool actual = (bool)method.Invoke(null, new object[] { spriteName });

            Assert.That(actual, Is.EqualTo(expectedFisherman),
                $"'{spriteName}' was categorised wrongly. Note the fish hat 'cap' is a substring of the "
                + "fisherman hat 'FisherMan_Hat_-Blue_Cap' once separators are stripped, so this lookup "
                + "must match ids EXACTLY â€” loose matching misfiles 'cap' as a fisherman hat.");
        }

        [Test]
        public void FishermanHeadBob_FollowsMeasuredHeadMotion()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo offsetMethod = typeof(CosmeticRuntimeApplier)
                .GetMethod("GetFishermanHeadOffset", flags);
            Assert.That(offsetMethod, Is.Not.Null, "GetFishermanHeadOffset was renamed or removed.");

            const float UnitsPerPixel = 0.04f;   // the fisherman ships at 25 PPU

            float Bob(int column) =>
                ((Vector3)offsetMethod.Invoke(null, new object[] { "idleleft", column, UnitsPerPixel })).y;

            float baseline = Bob(0);
            Assert.That(Bob(1), Is.GreaterThan(baseline), "column 1: head rises 1px, hat must rise too");
            Assert.That(Bob(2), Is.LessThan(baseline), "column 2: head drops 1px, hat must drop too");
            Assert.That(Bob(3), Is.LessThan(baseline), "column 3: head drops 1px, hat must drop too");
        }

        /// <summary>
        /// Direction alone is not enough â€” the hat has to move the SAME DISTANCE as the head, in world
        /// units. The bob tables are in source pixels, so the conversion must use the body sprite's
        /// pixels-per-unit. A hard-coded 0.01 (i.e. 100 PPU) moved the 25 PPU fisherman's hat only a
        /// quarter as far as its head, which still looked out of sync even with the direction correct.
        /// </summary>
        [TestCase(0.04f, TestName = "FishermanHeadBob_ScalesWithPixelsPerUnit_25ppu")]
        [TestCase(0.02f, TestName = "FishermanHeadBob_ScalesWithPixelsPerUnit_50ppu")]
        [TestCase(0.01f, TestName = "FishermanHeadBob_ScalesWithPixelsPerUnit_100ppu")]
        public void FishermanHeadBob_ScalesWithPixelsPerUnit(float unitsPerPixel)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo offsetMethod = typeof(CosmeticRuntimeApplier)
                .GetMethod("GetFishermanHeadOffset", flags);

            // idleleft head centres are 20, 19, 21, 21 (top-down px), so column 1 is exactly 1 px UP.
            float bob = ((Vector3)offsetMethod.Invoke(null, new object[] { "idleleft", 1, unitsPerPixel })).y;

            Assert.That(bob, Is.EqualTo(unitsPerPixel).Within(1e-6f),
                "a 1 px head movement must produce exactly 1 px of hat movement in world units");
        }

        // The fish hat's per-frame bob was deleted along with GetFishHatBobOffset: the fish's
        // HeadAnchor is keyed inside its swim clips, so there is no offset table left to unit-test.
        // HatTracksHeadTests replaces this coverage and audits every fish hat on every frame.
    }
}
