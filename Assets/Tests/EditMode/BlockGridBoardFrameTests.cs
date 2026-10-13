using NUnit.Framework;
using Gameplay.BlockDrag;
using UnityEngine;

namespace Tests.EditMode
{
    public class BlockGridBoardFrameTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ComputeFrameSize_AddsPaddingOnEachSide()
        {
            Vector2 size = BlockGrid.ComputeFrameSize(8f, 8f, 1f, 0.12f);

            Assert.AreEqual(8.24f, size.x, Tolerance);
            Assert.AreEqual(8.24f, size.y, Tolerance);
        }

        [Test]
        public void ComputeFrameSize_ScalesPaddingWithCellSize()
        {
            Vector2 size = BlockGrid.ComputeFrameSize(4f, 6f, 0.5f, 0.2f);

            Assert.AreEqual(4.2f, size.x, Tolerance);
            Assert.AreEqual(6.2f, size.y, Tolerance);
        }

        [Test]
        public void ComputeFrameSize_NegativeRatio_ReturnsGridSize()
        {
            Vector2 size = BlockGrid.ComputeFrameSize(8f, 8f, 1f, -0.5f);

            Assert.AreEqual(8f, size.x, Tolerance);
            Assert.AreEqual(8f, size.y, Tolerance);
        }
    }
}
