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

        // Buffer dùng lại cho FindLinesCompletedBy (gọi mỗi frame khi kéo) để không sinh rác GC
        private readonly bool[,] footprintScratch;
        private readonly bool[] rowScratch;
        private readonly bool[] colScratch;
        private readonly List<Vector2Int> footprintCells = new List<Vector2Int>();

        public int Rows { get; }
        public int Columns { get; }

        public BlockBoard(int rows, int columns)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

            Rows = rows;
            Columns = columns;
            occupied = new bool[rows, columns];
            footprintScratch = new bool[rows, columns];
            rowScratch = new bool[rows];
            colScratch = new bool[columns];
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
        /// Dự đoán các hàng/cột sẽ đầy nếu đặt shape tại baseCoord, không thay đổi bàn cờ.
        /// Trả None nếu không đặt được.
        /// </summary>
        public LineClearResult FindLinesCompletedBy(IShapeFootprint shape, Vector2Int baseCoord)
        {
            if (!CanPlace(shape, baseCoord)) return LineClearResult.None;

            List<int> fullRows = null;
            List<int> fullCols = null;

            try
            {
                for (int r = 0; r < shape.RowsCount; r++)
                {
                    for (int c = 0; c < shape.ColumnsCount; c++)
                    {
                        if (!shape.HasBlockAt(r, c)) continue;

                        int targetR = baseCoord.x + r;
                        int targetC = baseCoord.y + c;
                        footprintScratch[targetR, targetC] = true;
                        rowScratch[targetR] = true;
                        colScratch[targetC] = true;
                        footprintCells.Add(new Vector2Int(targetR, targetC));
                    }
                }

                // Chỉ hàng/cột shape chạm vào mới có thể vừa đầy; duyệt theo chỉ số để kết quả tăng dần
                for (int r = 0; r < Rows; r++)
                {
                    if (!rowScratch[r]) continue;

                    bool full = true;
                    for (int c = 0; c < Columns && full; c++)
                    {
                        full = occupied[r, c] || footprintScratch[r, c];
                    }
                    if (full) (fullRows ??= new List<int>()).Add(r);
                }

                for (int c = 0; c < Columns; c++)
                {
                    if (!colScratch[c]) continue;

                    bool full = true;
                    for (int r = 0; r < Rows && full; r++)
                    {
                        full = occupied[r, c] || footprintScratch[r, c];
                    }
                    if (full) (fullCols ??= new List<int>()).Add(c);
                }
            }
            finally
            {
                for (int i = 0; i < footprintCells.Count; i++)
                {
                    Vector2Int cell = footprintCells[i];
                    footprintScratch[cell.x, cell.y] = false;
                    rowScratch[cell.x] = false;
                    colScratch[cell.y] = false;
                }
                footprintCells.Clear();
            }

            if (fullRows == null && fullCols == null)
            {
                return LineClearResult.None;
            }

            IReadOnlyList<int> rows = fullRows ?? (IReadOnlyList<int>)Array.Empty<int>();
            IReadOnlyList<int> cols = fullCols ?? (IReadOnlyList<int>)Array.Empty<int>();

            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int i = 0; i < rows.Count; i++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    cells.Add(new Vector2Int(rows[i], c));
                }
            }

            for (int i = 0; i < cols.Count; i++)
            {
                for (int r = 0; r < Rows; r++)
                {
                    cells.Add(new Vector2Int(r, cols[i]));
                }
            }

            return new LineClearResult(rows, cols, cells);
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
