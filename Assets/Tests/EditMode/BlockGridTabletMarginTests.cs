using NUnit.Framework;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class BlockGridTabletMarginTests
    {
        private const float Threshold = 0.6f;

        [Test]
        public void IsTabletAspect_IPhone_ReturnsFalse()
        {
            Assert.IsFalse(BlockGrid.IsTabletAspect(1170f, 2532f, Threshold));
        }

        [Test]
        public void IsTabletAspect_Phone16x9_ReturnsFalse()
        {
            Assert.IsFalse(BlockGrid.IsTabletAspect(1080f, 1920f, Threshold));
        }

        [Test]
        public void IsTabletAspect_IPadPortrait_ReturnsTrue()
        {
            Assert.IsTrue(BlockGrid.IsTabletAspect(1536f, 2048f, Threshold));
        }

        [Test]
        public void IsTabletAspect_IPadLandscape_ReturnsTrue()
        {
            Assert.IsTrue(BlockGrid.IsTabletAspect(2048f, 1536f, Threshold));
        }

        [Test]
        public void IsTabletAspect_AndroidTablet16x10_ReturnsTrue()
        {
            Assert.IsTrue(BlockGrid.IsTabletAspect(1600f, 2560f, Threshold));
        }

        [Test]
        public void IsTabletAspect_ZeroSize_ReturnsFalse()
        {
            Assert.IsFalse(BlockGrid.IsTabletAspect(0f, 0f, Threshold));
        }
    }
}
