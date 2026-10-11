using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Kết quả quét hàng/cột đầy. Cells chứa mỗi ô (row, col) đúng một lần, kể cả ô giao giữa hàng và cột.
    /// </summary>
    public readonly struct LineClearResult
    {
        public static readonly LineClearResult None =
            new LineClearResult(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<Vector2Int>());

        public IReadOnlyList<int> Rows { get; }
        public IReadOnlyList<int> Columns { get; }
        public IReadOnlyCollection<Vector2Int> Cells { get; }

        public bool HasLines => Rows.Count > 0 || Columns.Count > 0;

        public LineClearResult(IReadOnlyList<int> rows, IReadOnlyList<int> columns, IReadOnlyCollection<Vector2Int> cells)
        {
            Rows = rows;
            Columns = columns;
            Cells = cells;
        }
    }
}
