using UnityEngine;

[CreateAssetMenu(fileName = "NewShapeData", menuName = "Game/Block Shape Data")]
public class BlockShapeData : ScriptableObject
{
    public Color shapeColor = Color.cyan;
    
    // Mảng các dòng. Ví dụ với chữ L:
    // row 0: "1,0"
    // row 1: "1,0"
    // row 2: "1,1"
    [Tooltip("Mỗi phần tử là 1 dòng, các cột cách nhau bằng dấu phẩy. 1 là có block, 0 là rỗng.")]
    public string[] rows;

    public int RowsCount => rows != null ? rows.Length : 0;
    public int ColumnsCount
    {
        get
        {
            if (rows == null || rows.Length == 0) return 0;
            return rows[0].Split(',').Length;
        }
    }

    // Lấy giá trị ô (1 hoặc 0)
    public bool HasBlockAt(int row, int col)
    {
        if (rows == null || row >= rows.Length) return false;
        string[] cols = rows[row].Split(',');
        if (col >= cols.Length) return false;
        return cols[col].Trim() == "1";
    }

    // Lấy số hàng sau khi xoay
    public int GetRotatedRowsCount(BlockRotation rotation)
    {
        return (rotation == BlockRotation.Rot_90 || rotation == BlockRotation.Rot_270) 
            ? ColumnsCount 
            : RowsCount;
    }

    // Lấy số cột sau khi xoay
    public int GetRotatedColumnsCount(BlockRotation rotation)
    {
        return (rotation == BlockRotation.Rot_90 || rotation == BlockRotation.Rot_270) 
            ? RowsCount 
            : ColumnsCount;
    }

    // Kiểm tra ô (rNew, cNew) sau khi xoay có gạch hay không
    public bool HasBlockAtRotated(int rNew, int cNew, BlockRotation rotation)
    {
        int H = RowsCount;
        int W = ColumnsCount;

        int origRow = rNew;
        int origCol = cNew;

        switch (rotation)
        {
            case BlockRotation.Rot_0:
                origRow = rNew;
                origCol = cNew;
                break;

            case BlockRotation.Rot_90:
                // Hàng mới lấy từ cột cũ, cột mới lấy từ hàng cũ bị đảo chiều
                origRow = H - 1 - cNew;
                origCol = rNew;
                break;

            case BlockRotation.Rot_180:
                origRow = H - 1 - rNew;
                origCol = W - 1 - cNew;
                break;

            case BlockRotation.Rot_270:
                origRow = cNew;
                origCol = W - 1 - rNew;
                break;
        }

        return HasBlockAt(origRow, origCol);
    }
}

public enum BlockRotation
{
    Rot_0 = 0,
    Rot_90 = 90,
    Rot_180 = 180,
    Rot_270 = 270
}