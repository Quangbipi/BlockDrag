using System.Collections.Generic;
using System.Text;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Một BlockShapeData ở một góc xoay, footprint đã copy ra mảng để kiểm tra nhanh.
    /// Key định danh hình sau khi xoay; FamilyKey giống nhau cho mọi góc xoay của cùng một hình.
    /// </summary>
    public sealed class ShapeVariant : IShapeFootprint
    {
        private static readonly BlockRotation[] AllRotations =
        {
            BlockRotation.Rot_0,
            BlockRotation.Rot_90,
            BlockRotation.Rot_180,
            BlockRotation.Rot_270
        };

        private readonly bool[,] cells;

        public BlockShapeData Data { get; }
        public BlockRotation Rotation { get; }
        public int RowsCount { get; }
        public int ColumnsCount { get; }
        public int CellCount { get; }
        public string Key { get; }
        public string FamilyKey { get; }

        public ShapeVariant(BlockShapeData data, BlockRotation rotation)
        {
            Data = data;
            Rotation = rotation;
            RowsCount = data.GetRotatedRowsCount(rotation);
            ColumnsCount = data.GetRotatedColumnsCount(rotation);
            cells = new bool[RowsCount, ColumnsCount];

            for (int r = 0; r < RowsCount; r++)
            {
                for (int c = 0; c < ColumnsCount; c++)
                {
                    cells[r, c] = data.HasBlockAtRotated(r, c, rotation);
                    if (cells[r, c]) CellCount++;
                }
            }

            Key = BuildKey(data, rotation);
            FamilyKey = BuildFamilyKey(data);
        }

        public bool HasBlockAt(int row, int col)
        {
            return row >= 0 && row < RowsCount && col >= 0 && col < ColumnsCount && cells[row, col];
        }

        /// <summary>
        /// Các góc xoay cho ra hình khác nhau của shape (Square: 1, I: 2, L: 4). Shape rỗng → list rỗng.
        /// </summary>
        public static List<ShapeVariant> BuildDistinct(BlockShapeData data)
        {
            List<ShapeVariant> result = new List<ShapeVariant>();
            if (data == null || data.RowsCount == 0 || data.ColumnsCount == 0) return result;

            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < AllRotations.Length; i++)
            {
                ShapeVariant variant = new ShapeVariant(data, AllRotations[i]);
                if (variant.CellCount > 0 && seen.Add(variant.Key))
                {
                    result.Add(variant);
                }
            }
            return result;
        }

        private static string BuildFamilyKey(BlockShapeData data)
        {
            string min = null;
            for (int i = 0; i < AllRotations.Length; i++)
            {
                string key = BuildKey(data, AllRotations[i]);
                if (min == null || string.CompareOrdinal(key, min) < 0)
                {
                    min = key;
                }
            }
            return min;
        }

        private static string BuildKey(BlockShapeData data, BlockRotation rotation)
        {
            int rows = data.GetRotatedRowsCount(rotation);
            int cols = data.GetRotatedColumnsCount(rotation);

            StringBuilder sb = new StringBuilder(rows * cols + 6);
            sb.Append(rows).Append('x').Append(cols).Append(':');
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    sb.Append(data.HasBlockAtRotated(r, c, rotation) ? '1' : '0');
                }
            }
            return sb.ToString();
        }
    }
}
