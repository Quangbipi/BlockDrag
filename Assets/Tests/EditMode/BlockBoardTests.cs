using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class BlockBoardTests
    {
        private sealed class MaskFootprint : IShapeFootprint
        {
            private readonly bool[,] mask;

            public MaskFootprint(bool[,] mask)
            {
                this.mask = mask;
            }

            public int RowsCount => mask.GetLength(0);
            public int ColumnsCount => mask.GetLength(1);
            public bool HasBlockAt(int row, int col) => mask[row, col];
        }

        private static readonly IShapeFootprint Single = new MaskFootprint(new[,] { { true } });
        private static readonly IShapeFootprint Square2 = new MaskFootprint(new[,] { { true, true }, { true, true } });

        private static readonly IShapeFootprint LShape = new MaskFootprint(new[,]
        {
            { true, false },
            { true, false },
            { true, true },
        });

        private BlockBoard board;

        [SetUp]
        public void SetUp()
        {
            board = new BlockBoard(4, 4);
        }

        [Test]
        public void CanPlace_WhenInsideAndEmpty_ReturnsTrue()
        {
            Assert.IsTrue(board.CanPlace(Square2, new Vector2Int(2, 2)));
        }

        [Test]
        public void CanPlace_WhenPartlyOutOfBounds_ReturnsFalse()
        {
            Assert.IsFalse(board.CanPlace(Square2, new Vector2Int(3, 3)));
            Assert.IsFalse(board.CanPlace(Single, new Vector2Int(-1, 0)));
        }

        [Test]
        public void CanPlace_WhenOverlappingOccupiedCell_ReturnsFalse()
        {
            board.SetOccupied(1, 1, true);

            Assert.IsFalse(board.CanPlace(Square2, new Vector2Int(0, 0)));
        }

        [Test]
        public void CanPlace_IgnoresEmptyMaskCells()
        {
            // Ô (0,1) của L trống nên ô bị chiếm tại đó không chặn việc đặt
            board.SetOccupied(0, 1, true);

            Assert.IsTrue(board.CanPlace(LShape, new Vector2Int(0, 0)));
        }

        [Test]
        public void CanPlaceAnywhere_WhenOnlyOneSlotFree_ReturnsTrue()
        {
            FillAllExcept(new Vector2Int(3, 0));

            Assert.IsTrue(board.CanPlaceAnywhere(Single));
            Assert.IsFalse(board.CanPlaceAnywhere(Square2));
        }

        [Test]
        public void CanPlaceAnywhere_WhenShapeLargerThanBoard_ReturnsFalse()
        {
            var tiny = new BlockBoard(1, 1);

            Assert.IsFalse(tiny.CanPlaceAnywhere(Square2));
        }

        [Test]
        public void ClearFullLines_WhenNothingFull_ReturnsNoneAndKeepsCells()
        {
            board.SetOccupied(0, 0, true);

            LineClearResult result = board.ClearFullLines();

            Assert.IsFalse(result.HasLines);
            Assert.IsTrue(board.IsOccupied(0, 0));
        }

        [Test]
        public void ClearFullLines_WhenRowAndColumnFull_CountsIntersectionOnce()
        {
            for (int i = 0; i < 4; i++)
            {
                board.SetOccupied(1, i, true);
                board.SetOccupied(i, 2, true);
            }
            board.SetOccupied(3, 3, true);

            LineClearResult result = board.ClearFullLines();

            CollectionAssert.AreEqual(new[] { 1 }, result.Rows.ToArray());
            CollectionAssert.AreEqual(new[] { 2 }, result.Columns.ToArray());
            Assert.AreEqual(7, result.Cells.Count);
            Assert.IsFalse(board.IsOccupied(1, 0));
            Assert.IsFalse(board.IsOccupied(0, 2));
            Assert.IsTrue(board.IsOccupied(3, 3));
        }

        [Test]
        public void Clear_ResetsEveryCell()
        {
            FillAllExcept(new Vector2Int(-1, -1));

            board.Clear();

            Assert.IsTrue(board.CanPlace(Square2, Vector2Int.zero));
            Assert.IsFalse(board.IsOccupied(3, 3));
        }

        [Test]
        public void IsOccupied_WhenOutOfBounds_ReturnsFalse()
        {
            Assert.IsFalse(board.IsOccupied(-1, 0));
            Assert.IsFalse(board.IsOccupied(0, 4));
        }

        private void FillAllExcept(Vector2Int free)
        {
            for (int r = 0; r < board.Rows; r++)
            {
                for (int c = 0; c < board.Columns; c++)
                {
                    board.SetOccupied(r, c, new Vector2Int(r, c) != free);
                }
            }
        }
    }
}
