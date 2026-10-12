using Gameplay.BlockDrag;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class BlockDragTouchAreaTests
    {
        private GameObject shapeObj;

        [TearDown]
        public void TearDown()
        {
            if (shapeObj != null)
            {
                Object.DestroyImmediate(shapeObj);
            }
        }

        private BlockShape CreateShape()
        {
            shapeObj = new GameObject("Shape", typeof(BoxCollider2D));
            return shapeObj.AddComponent<BlockShape>();
        }

        [Test]
        public void CalculateTouchColliderSize_WhenTouchAreaLarger_UsesTouchAreaDividedByScale()
        {
            Vector2 size = BlockShape.CalculateTouchColliderSize(new Vector2(1f, 1f), new Vector2(3f, 2.4f), 0.6f);

            Assert.AreEqual(5f, size.x, 1e-4f);
            Assert.AreEqual(4f, size.y, 1e-4f);
        }

        [Test]
        public void CalculateTouchColliderSize_WhenShapeLarger_KeepsShapeSize()
        {
            Vector2 size = BlockShape.CalculateTouchColliderSize(new Vector2(8f, 1f), new Vector2(3f, 2.4f), 0.6f);

            Assert.AreEqual(8f, size.x, 1e-4f);
            Assert.AreEqual(4f, size.y, 1e-4f);
        }

        [Test]
        public void CalculateTouchColliderSize_WhenScaleNotPositive_ReturnsShapeSize()
        {
            Vector2 shapeSize = new Vector2(2f, 1f);

            Assert.AreEqual(shapeSize, BlockShape.CalculateTouchColliderSize(shapeSize, new Vector2(3f, 3f), 0f));
        }

        [Test]
        public void SetTouchArea_CentersColliderOnSlot()
        {
            BlockShape shape = CreateShape();
            BoxCollider2D col = shapeObj.GetComponent<BoxCollider2D>();
            col.offset = new Vector2(0.5f, 0.5f);

            shape.SetTouchArea(new Vector2(3f, 2.4f), 0.6f);

            Assert.AreEqual(Vector2.zero, col.offset);
            Assert.AreEqual(5f, col.size.x, 1e-4f);
            Assert.AreEqual(4f, col.size.y, 1e-4f);
        }

        [Test]
        public void SetInputEnabled_WhenDimmed_KeepsColliderDisabled()
        {
            BlockShape shape = CreateShape();
            shape.SetDimmed(true);

            shape.SetInputEnabled(true);

            Assert.IsFalse(shapeObj.GetComponent<BoxCollider2D>().enabled);
        }

        [Test]
        public void SetDimmed_False_WhileInputDisabled_KeepsColliderDisabled()
        {
            BlockShape shape = CreateShape();
            shape.SetInputEnabled(false);

            shape.SetDimmed(false);

            Assert.IsFalse(shapeObj.GetComponent<BoxCollider2D>().enabled);
        }

        [Test]
        public void SetInputEnabled_WhenNotDimmed_EnablesCollider()
        {
            BlockShape shape = CreateShape();
            shape.SetInputEnabled(false);

            shape.SetInputEnabled(true);

            Assert.IsTrue(shapeObj.GetComponent<BoxCollider2D>().enabled);
        }
    }
}
