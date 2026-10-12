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

        [Test]
        public void FindLinesCompletedBy_WhenShapeFillsLastGapInRow_ReturnsThatRow()
        {
            for (int c = 0; c < 3; c++) board.SetOccupied(2, c, true);

            LineClearResult result = board.FindLinesCompletedBy(Single, new Vector2Int(2, 3));

            CollectionAssert.AreEqual(new[] { 2 }, result.Rows.ToArray());
            Assert.AreEqual(0, result.Columns.Count);
            Assert.AreEqual(4, result.Cells.Count);
        }

        [Test]
        public void FindLinesCompletedBy_WhenShapeCompletesRowAndColumn_ReturnsBothWithoutDuplicateCells()
        {
            // Hàng 3 thiếu (3,0), (3,1); cột 0 thiếu (2,0), (3,0) => L 2 ô ngang + 1 ô trên
            for (int c = 2; c < 4; c++) board.SetOccupied(3, c, true);
            for (int r = 0; r < 2; r++) board.SetOccupied(r, 0, true);
            var corner = new MaskFootprint(new[,] { { true, false }, { true, true } });

            LineClearResult result = board.FindLinesCompletedBy(corner, new Vector2Int(2, 0));

            CollectionAssert.AreEqual(new[] { 3 }, result.Rows.ToArray());
            CollectionAssert.AreEqual(new[] { 0 }, result.Columns.ToArray());
            Assert.AreEqual(7, result.Cells.Count);
        }

        [Test]
        public void FindLinesCompletedBy_WhenNoLineCompleted_ReturnsNone()
        {
            LineClearResult result = board.FindLinesCompletedBy(Square2, new Vector2Int(0, 0));

            Assert.IsFalse(result.HasLines);
        }

        [Test]
        public void FindLinesCompletedBy_WhenCannotPlace_ReturnsNone()
        {
            for (int c = 0; c < 4; c++) board.SetOccupied(0, c, true);

            Assert.IsFalse(board.FindLinesCompletedBy(Single, new Vector2Int(0, 0)).HasLines);
            Assert.IsFalse(board.FindLinesCompletedBy(Single, new Vector2Int(-1, 0)).HasLines);
        }

        [Test]
        public void FindLinesCompletedBy_DoesNotMutateBoard()
        {
            for (int c = 0; c < 3; c++) board.SetOccupied(0, c, true);

            board.FindLinesCompletedBy(Single, new Vector2Int(0, 3));

            Assert.IsFalse(board.IsOccupied(0, 3));
            Assert.IsTrue(board.IsOccupied(0, 0));
            Assert.IsFalse(board.FindFullLines().HasLines);
        }

        [Test]
        public void FindLinesCompletedBy_MatchesFindFullLinesAfterActualPlacement()
        {
            // Hàng 0 thiếu (0,1); cột 1 thiếu (0,1), (1,1)
            board.SetOccupied(2, 1, true);
            board.SetOccupied(3, 1, true);
            board.SetOccupied(0, 0, true);
            board.SetOccupied(0, 2, true);
            board.SetOccupied(0, 3, true);
            var baseCoord = new Vector2Int(0, 1);
            var vertical2 = new MaskFootprint(new[,] { { true }, { true } });

            LineClearResult predicted = board.FindLinesCompletedBy(vertical2, baseCoord);
            board.SetOccupied(0, 1, true);
            board.SetOccupied(1, 1, true);
            LineClearResult actual = board.FindFullLines();

            Assert.IsTrue(predicted.HasLines);
            CollectionAssert.AreEqual(actual.Rows.ToArray(), predicted.Rows.ToArray());
            CollectionAssert.AreEqual(actual.Columns.ToArray(), predicted.Columns.ToArray());
            CollectionAssert.AreEquivalent(actual.Cells.ToArray(), predicted.Cells.ToArray());
        }

        [Test]
        public void FindLinesCompletedBy_CalledRepeatedlyAtDifferentPositions_DoesNotLeakPreviousFootprint()
        {
            for (int c = 0; c < 3; c++) board.SetOccupied(0, c, true);

            Assert.IsTrue(board.FindLinesCompletedBy(Single, new Vector2Int(0, 3)).HasLines);
            Assert.IsFalse(board.FindLinesCompletedBy(Single, new Vector2Int(1, 0)).HasLines);

            LineClearResult again = board.FindLinesCompletedBy(Single, new Vector2Int(0, 3));
            CollectionAssert.AreEqual(new[] { 0 }, again.Rows.ToArray());
            Assert.AreEqual(0, again.Columns.Count);
        }

        [Test]
        public void FindLinesCompletedBy_WhenCannotPlace_DoesNotLeakIntoNextCall()
        {
            for (int c = 0; c < 3; c++) board.SetOccupied(0, c, true);

            board.FindLinesCompletedBy(Square2, new Vector2Int(3, 3));
            board.FindLinesCompletedBy(Single, new Vector2Int(0, 0));

            Assert.IsFalse(board.FindLinesCompletedBy(Single, new Vector2Int(2, 2)).HasLines);
        }

        [Test]
        public void FilledRatio_AfterPlacingCells_ReturnsOccupiedFraction()
        {
            board.Place(Square2, new Vector2Int(0, 0));

            Assert.AreEqual(4, board.OccupiedCount);
            Assert.AreEqual(0.25f, board.FilledRatio, 1e-5f);
        }

        [Test]
        public void CopyFrom_CopiesOccupancyIndependently()
        {
            board.SetOccupied(1, 2, true);
            BlockBoard copy = new BlockBoard(4, 4);

            copy.CopyFrom(board);
            copy.SetOccupied(3, 3, true);

            Assert.IsTrue(copy.IsOccupied(1, 2));
            Assert.IsFalse(board.IsOccupied(3, 3));
        }

        [Test]
        public void Place_MarksAllShapeCellsOccupied()
        {
            board.Place(LShape, new Vector2Int(1, 1));

            Assert.IsTrue(board.IsOccupied(1, 1));
            Assert.IsTrue(board.IsOccupied(2, 1));
            Assert.IsTrue(board.IsOccupied(3, 1));
            Assert.IsTrue(board.IsOccupied(3, 2));
            Assert.IsFalse(board.IsOccupied(1, 2));
            Assert.AreEqual(4, board.OccupiedCount);
        }

        [Test]
        public void ClearFullLinesInPlace_ClearsRowAndColumnAndReturnsCount()
        {
            for (int c = 0; c < 4; c++) board.SetOccupied(0, c, true);
            for (int r = 0; r < 4; r++) board.SetOccupied(r, 3, true);
            board.SetOccupied(2, 1, true);

            Assert.AreEqual(2, board.ClearFullLinesInPlace());
            Assert.AreEqual(1, board.OccupiedCount);
            Assert.IsTrue(board.IsOccupied(2, 1));
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
