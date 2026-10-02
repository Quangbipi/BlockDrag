using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class GameOverCheckTests
    {
        private GameObject gridObject;
        private BlockGrid grid;
        private GameObject shapeObject;
        private BlockShape shape;
        private BlockShapeData shapeData;

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("TestGrid");
            grid = gridObject.AddComponent<BlockGrid>();
            grid.GenerateGrid();

            shapeObject = new GameObject("TestShape");
            shape = shapeObject.AddComponent<BlockShape>();

            shapeData = ScriptableObject.CreateInstance<BlockShapeData>();
            shapeData.shapeColor = Color.red;
            shapeData.rows = new string[] { "1" }; // 1x1 block
            shape.Initialize(shapeData, null, BlockRotation.Rot_0, cellSize: 1f);
        }

        [TearDown]
        public void TearDown()
        {
            if (shapeObject != null) Object.DestroyImmediate(shapeObject);
            if (gridObject != null) Object.DestroyImmediate(gridObject);
            if (shapeData != null) Object.DestroyImmediate(shapeData);
        }

        [Test]
        public void SetDimmed_WhenTrue_DisablesBoxColliderAndSetsIsDimmed()
        {
            shape.SetDimmed(true);

            Assert.IsTrue(shape.IsDimmed);
            Assert.IsFalse(shape.BoxCollider.enabled);
        }

        [Test]
        public void SetDimmed_WhenFalse_EnablesBoxColliderAndResetsIsDimmed()
        {
            shape.SetDimmed(true);
            shape.SetDimmed(false);

            Assert.IsFalse(shape.IsDimmed);
            Assert.IsTrue(shape.BoxCollider.enabled);
        }

        [Test]
        public void CanShapeBePlacedAnywhere_WhenGridIsEmpty_ReturnsTrue()
        {
            bool canPlace = grid.CanShapeBePlacedAnywhere(shape);

            Assert.IsTrue(canPlace);
        }

        [Test]
        public void CanShapeBePlacedAnywhere_WhenShapeIsLargerThanGrid_ReturnsFalse()
        {
            var largeShapeData = ScriptableObject.CreateInstance<BlockShapeData>();
            largeShapeData.rows = new string[]
            {
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1",
                "1,1,1,1,1,1,1,1,1,1" // 9x10 (larger than default 8x8)
            };

            var largeShapeObj = new GameObject("LargeShape");
            var largeShape = largeShapeObj.AddComponent<BlockShape>();
            largeShape.Initialize(largeShapeData, null, BlockRotation.Rot_0, cellSize: 1f);

            bool canPlace = grid.CanShapeBePlacedAnywhere(largeShape);

            Assert.IsFalse(canPlace);

            Object.DestroyImmediate(largeShapeObj);
            Object.DestroyImmediate(largeShapeData);
        }

        [Test]
        public void ResetBoard_WhenBlocksPlaced_ClearsAllOccupiedCells()
        {
            // Place 1 block manually
            Vector3 worldPos = grid.GetCellWorldPosition(0, 0);
            bool placed = grid.TryPlaceShape(shape, worldPos);

            Assert.IsTrue(placed);
            Assert.IsTrue(grid.IsCellOccupied(0, 0));

            // Reset board
            grid.ResetBoard();

            Assert.IsFalse(grid.IsCellOccupied(0, 0));
        }
    }
}
