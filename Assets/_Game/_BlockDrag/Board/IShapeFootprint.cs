namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Hình dạng ô của một khối (đã tính góc xoay), dùng cho luật đặt khối của <see cref="BlockBoard"/>
    /// </summary>
    public interface IShapeFootprint
    {
        int RowsCount { get; }
        int ColumnsCount { get; }
        bool HasBlockAt(int row, int col);
    }
}
