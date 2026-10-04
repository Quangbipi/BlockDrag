using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class BlockGridPlacementCenterTests
    {
        private GameObject gridGo;
        private BlockGrid grid;

        [SetUp]
        public void SetUp()
        {
            gridGo = new GameObject("BlockGrid_Test");
            grid = gridGo.AddComponent<BlockGrid>();
            //grid.InitializeGrid(8, 8, 1f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (gridGo != null)
            {
                Object.DestroyImmediate(gridGo);
            }
        }

        [Test]
        public void CalculateCellsCenterPosition_SingleCell_ReturnsCellWorldPosition()
        {
            Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0) };
            Vector3 expectedPos = grid.GetCellWorldPosition(0, 0);

            Vector3 center = grid.CalculateCellsCenterPosition(cells);

            Assert.AreEqual(expectedPos.x, center.x, 0.001f);
            Assert.AreEqual(expectedPos.y, center.y, 0.001f);
        }

        [Test]
        public void CalculateCellsCenterPosition_TwoCells_ReturnsAveragePosition()
        {
            Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1) };
            Vector3 pos0 = grid.GetCellWorldPosition(0, 0);
            Vector3 pos1 = grid.GetCellWorldPosition(0, 1);
            Vector3 expectedCenter = (pos0 + pos1) * 0.5f;

            Vector3 center = grid.CalculateCellsCenterPosition(cells);

            Assert.AreEqual(expectedCenter.x, center.x, 0.001f);
            Assert.AreEqual(expectedCenter.y, center.y, 0.001f);
        }

        [Test]
        public void CalculateCellsCenterPosition_EmptyOrNull_ReturnsGridPosition()
        {
            Vector3 centerNull = grid.CalculateCellsCenterPosition(null);
            Assert.AreEqual(grid.transform.position, centerNull);

            Vector3 centerEmpty = grid.CalculateCellsCenterPosition(new Vector2Int[0]);
            Assert.AreEqual(grid.transform.position, centerEmpty);
        }
    }
}
