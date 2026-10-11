using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Luật bàn cờ Block Blast thuần C# (không phụ thuộc GameObject): trạng thái ô, kiểm tra đặt khối,
    /// tìm và phá hàng/cột đầy. Toạ độ là (row, col), hàng 0 ở trên cùng, cột 0 bên trái.
    /// </summary>
    public sealed class BlockBoard
    {
        private readonly bool[,] occupied;

        public int Rows { get; }
        public int Columns { get; }

        public BlockBoard(int rows, int columns)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

            Rows = rows;
            Columns = columns;
            occupied = new bool[rows, columns];
        }

        public bool IsInside(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Columns;
        }

        /// <summary>
        /// Ô ngoài phạm vi được coi là không bị chiếm
        /// </summary>
        public bool IsOccupied(int row, int col)
        {
            return IsInside(row, col) && occupied[row, col];
        }

        public void SetOccupied(int row, int col, bool value)
        {
            if (!IsInside(row, col)) return;
            occupied[row, col] = value;
        }

        public void Clear()
        {
            Array.Clear(occupied, 0, occupied.Length);
        }

        /// <summary>
        /// Mọi ô của shape đặt tại baseCoord (row, col) đều nằm trong bàn và chưa bị chiếm
        /// </summary>
        public bool CanPlace(IShapeFootprint shape, Vector2Int baseCoord)
        {
            if (shape == null) return false;

            int rows = shape.RowsCount;
            int cols = shape.ColumnsCount;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!shape.HasBlockAt(r, c)) continue;

                    int targetR = baseCoord.x + r;
                    int targetC = baseCoord.y + c;

                    if (!IsInside(targetR, targetC) || occupied[targetR, targetC])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Shape có thể đặt được ở ít nhất một vị trí trên bàn (dùng kiểm tra Game Over)
        /// </summary>
        public bool CanPlaceAnywhere(IShapeFootprint shape)
        {
            if (shape == null) return false;

            int maxR = Rows - shape.RowsCount;
            int maxC = Columns - shape.ColumnsCount;

            for (int r = 0; r <= maxR; r++)
            {
                for (int c = 0; c <= maxC; c++)
                {
                    if (CanPlace(shape, new Vector2Int(r, c)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tìm các hàng/cột đã lấp đầy mà không thay đổi bàn cờ
        /// </summary>
        public LineClearResult FindFullLines()
        {
            List<int> fullRows = new List<int>();
            for (int r = 0; r < Rows; r++)
            {
                if (IsRowFull(r)) fullRows.Add(r);
            }

            List<int> fullCols = new List<int>();
            for (int c = 0; c < Columns; c++)
            {
                if (IsColumnFull(c)) fullCols.Add(c);
            }

            if (fullRows.Count == 0 && fullCols.Count == 0)
            {
                return LineClearResult.None;
            }

            // HashSet để ô giao giữa hàng và cột chỉ tính 1 lần
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            foreach (int r in fullRows)
            {
                for (int c = 0; c < Columns; c++)
                {
                    cells.Add(new Vector2Int(r, c));
                }
            }

            foreach (int c in fullCols)
            {
                for (int r = 0; r < Rows; r++)
                {
                    cells.Add(new Vector2Int(r, c));
                }
            }

            return new LineClearResult(fullRows, fullCols, cells);
        }

        /// <summary>
        /// Tìm và trả các ô thuộc hàng/cột đầy về trạng thái rỗng
        /// </summary>
        public LineClearResult ClearFullLines()
        {
            LineClearResult result = FindFullLines();
            foreach (Vector2Int cell in result.Cells)
            {
                occupied[cell.x, cell.y] = false;
            }
            return result;
        }

        private bool IsRowFull(int row)
        {
            for (int c = 0; c < Columns; c++)
            {
                if (!occupied[row, c]) return false;
            }
            return true;
        }

        private bool IsColumnFull(int col)
        {
            for (int r = 0; r < Rows; r++)
            {
                if (!occupied[r, col]) return false;
            }
            return true;
        }
    }
}
