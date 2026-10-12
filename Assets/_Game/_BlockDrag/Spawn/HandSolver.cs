using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Mô phỏng trên bản sao bàn cờ: có thứ tự nào đặt được hết các khối không (có phá hàng sau mỗi lần đặt)
    /// </summary>
    public static class HandSolver
    {
        /// <summary>
        /// True nếu tồn tại thứ tự và vị trí đặt hết shapes. Vượt maxNodes → coi như không giải được.
        /// Không thay đổi board.
        /// </summary>
        public static bool IsSolvable(BlockBoard board, IReadOnlyList<IShapeFootprint> shapes, int maxNodes)
        {
            if (board == null || shapes == null) return false;
            if (shapes.Count == 0) return true;

            BlockBoard[] levels = new BlockBoard[shapes.Count + 1];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = new BlockBoard(board.Rows, board.Columns);
            }
            levels[0].CopyFrom(board);

            bool[] used = new bool[shapes.Count];
            int nodes = 0;
            return Solve(levels, 0, shapes, used, ref nodes, maxNodes);
        }

        /// <summary>
        /// Shape có vị trí đặt được mà hoàn thành ít nhất 1 hàng/cột
        /// </summary>
        public static bool CanClearAnyLine(BlockBoard board, IShapeFootprint shape)
        {
            if (board == null || shape == null) return false;

            int maxR = board.Rows - shape.RowsCount;
            int maxC = board.Columns - shape.ColumnsCount;
            for (int r = 0; r <= maxR; r++)
            {
                for (int c = 0; c <= maxC; c++)
                {
                    Vector2Int coord = new Vector2Int(r, c);
                    if (board.CanPlace(shape, coord) && board.FindLinesCompletedBy(shape, coord).HasLines)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool Solve(BlockBoard[] levels, int depth, IReadOnlyList<IShapeFootprint> shapes, bool[] used, ref int nodes, int maxNodes)
        {
            if (depth == shapes.Count) return true;

            BlockBoard current = levels[depth];
            BlockBoard next = levels[depth + 1];

            for (int i = 0; i < shapes.Count; i++)
            {
                if (used[i]) continue;

                IShapeFootprint shape = shapes[i];
                int maxR = current.Rows - shape.RowsCount;
                int maxC = current.Columns - shape.ColumnsCount;

                used[i] = true;
                for (int r = 0; r <= maxR; r++)
                {
                    for (int c = 0; c <= maxC; c++)
                    {
                        Vector2Int coord = new Vector2Int(r, c);
                        if (!current.CanPlace(shape, coord)) continue;
                        if (++nodes > maxNodes)
                        {
                            used[i] = false;
                            return false;
                        }

                        next.CopyFrom(current);
                        next.Place(shape, coord);
                        next.ClearFullLinesInPlace();

                        if (Solve(levels, depth + 1, shapes, used, ref nodes, maxNodes))
                        {
                            used[i] = false;
                            return true;
                        }
                        if (nodes > maxNodes)
                        {
                            used[i] = false;
                            return false;
                        }
                    }
                }
                used[i] = false;
            }

            return false;
        }
    }
}
