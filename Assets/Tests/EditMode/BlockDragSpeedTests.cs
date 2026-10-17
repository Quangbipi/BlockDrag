using Gameplay.BlockDrag;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class BlockDragSpeedTests
    {
        [Test]
        public void ComputeDragPosition_MultiplierOne_ReturnsFinger()
        {
            Vector2 result = BlockDragHandler.ComputeDragPosition(new Vector2(1f, 1f), new Vector2(3f, -2f), 1f);

            Assert.AreEqual(3f, result.x, 1e-5f);
            Assert.AreEqual(-2f, result.y, 1e-5f);
        }

        [Test]
        public void ComputeDragPosition_FingerAtAnchor_ReturnsAnchor()
        {
            Vector2 result = BlockDragHandler.ComputeDragPosition(new Vector2(1f, 1f), new Vector2(1f, 1f), 2f);

            Assert.AreEqual(1f, result.x, 1e-5f);
            Assert.AreEqual(1f, result.y, 1e-5f);
        }

        [Test]
        public void ComputeDragPosition_Multiplier1_5_ScalesDeltaFromAnchor()
        {
            Vector2 result = BlockDragHandler.ComputeDragPosition(new Vector2(1f, 1f), new Vector2(3f, 2f), 1.5f);

            Assert.AreEqual(4f, result.x, 1e-5f);
            Assert.AreEqual(2.5f, result.y, 1e-5f);
        }
    }
}
