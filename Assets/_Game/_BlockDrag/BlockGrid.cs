using System.Collections.Generic;
using UnityEngine;

public class BlockGrid : MonoBehaviour
{
    [Header("Grid Dimensions")]
    [Tooltip("Số lượng cột")]
    [SerializeField] protected int maxColumn = 8;
    [Tooltip("Số lượng hàng")]
    [SerializeField] protected int maxRow = 8;

    [Header("Spacing & Margins")]
    [Tooltip("Khoảng cách giữa các phần tử trong grid (World Units)")]
    [SerializeField] protected float spacing = 0.05f;
    [Tooltip("Tỷ lệ cách đều lề trái/phải màn hình (mặc định 0.1 tương ứng 10%)")]
    [Range(0f, 0.4f)]
    [SerializeField] protected float marginPercent = 0.1f;
    [Tooltip("Nếu bật, tự động đưa vị trí Grid về chính giữa màn hình Camera. Nếu tắt, giữ nguyên transform.position")]
    [SerializeField] protected bool centerOnCamera = true;

    [Header("Prefabs & References")]
    [Tooltip("Prefab ô nền vuông 1x1")]
    [SerializeField] protected GameObject cellPrefab;
    [Tooltip("Camera chính dùng để đo kích thước màn hình (nếu để trống sẽ tự lấy Camera.main)")]
    [SerializeField] protected Camera targetCamera;

    // Runtime variables
    protected GameObject[,] gridCells;
    protected float cellSize;
    protected float gridWidth;
    protected float gridHeight;

    public float CellSize => cellSize;
    public float GridWidth => gridWidth;
    public float GridHeight => gridHeight;
    public int MaxColumn => maxColumn;
    public int MaxRow => maxRow;
    public float Spacing => spacing;

    protected virtual void Start()
    {
        GenerateGrid();
    }

    /// <summary>
    /// Tính toán các thông số kích thước ô và kích thước toàn grid dựa trên Camera
    /// </summary>
    public void CalculateGridParameters()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[BlockGrid] Không tìm thấy Camera để tính kích thước màn hình! Sử dụng kích thước mặc định.");
            cellSize = 1f;
            gridWidth = maxColumn * cellSize + (maxColumn - 1) * spacing;
            gridHeight = maxRow * cellSize + (maxRow - 1) * spacing;
            return;
        }

        // Đo kích thước màn hình trong World Space
        float screenWorldHeight;
        float screenWorldWidth;

        if (cam.orthographic)
        {
            screenWorldHeight = cam.orthographicSize * 2f;
            screenWorldWidth = screenWorldHeight * cam.aspect;
        }
        else
        {
            float distance = Mathf.Abs(cam.transform.position.z - transform.position.z);
            screenWorldHeight = 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            screenWorldWidth = screenWorldHeight * cam.aspect;
        }

        // Vùng chiều ngang khả dụng sau khi trừ đi lề 2 bên (ví dụ 10% mỗi bên => khả dụng = 80%)
        float usableWidth = screenWorldWidth * Mathf.Clamp01(1f - 2f * marginPercent);

        int cols = Mathf.Max(1, maxColumn);
        int rows = Mathf.Max(1, maxRow);

        // Trừ tổng khoảng cách spacing giữa các cột để grid luôn nằm gọn trong 80% chiều ngang
        float totalSpacingX = (cols - 1) * spacing;
        cellSize = Mathf.Max(0.01f, (usableWidth - totalSpacingX) / cols);

        // Tổng kích thước của toàn bộ Grid
        gridWidth = cols * cellSize + totalSpacingX;
        gridHeight = rows * cellSize + (rows - 1) * spacing;

        // Căn tâm grid vào giữa màn hình nếu được bật
        if (centerOnCamera)
        {
            Vector3 centerPos = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cam.nearClipPlane));
            centerPos.z = transform.position.z;
            transform.position = centerPos;
        }
    }

    /// <summary>
    /// Xoá và tái tạo toàn bộ lưới các ô
    /// </summary>
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();
        CalculateGridParameters();

        int cols = Mathf.Max(1, maxColumn);
        int rows = Mathf.Max(1, maxRow);
        gridCells = new GameObject[rows, cols];

        float step = cellSize + spacing;
        float startX = -gridWidth * 0.5f + cellSize * 0.5f;
        float startY = gridHeight * 0.5f - cellSize * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Tọa độ cục bộ: Hàng 0 ở trên cùng, cột 0 ở ngoài cùng bên trái
                Vector3 localPos = new Vector3(startX + c * step, startY - r * step, 0f);

                GameObject cellObj;
                if (cellPrefab != null)
                {
                    cellObj = Instantiate(cellPrefab, transform);
                }
                else
                {
                    // Fallback tạo GameObject rỗng nếu chưa gán prefab
                    cellObj = new GameObject($"Cell_{r}_{c}");
                    cellObj.transform.SetParent(transform);
                }

                cellObj.name = $"Cell_{r}_{c}";
                cellObj.transform.localPosition = localPos;

                // Tự động scale ô theo kích thước cellSize đã tính
                ApplyCellScale(cellObj, cellSize);

                gridCells[r, c] = cellObj;
            }
        }
    }

    /// <summary>
    /// Căn chỉnh scale của ô dựa vào sprite.bounds.size của SpriteRenderer
    /// </summary>
    private void ApplyCellScale(GameObject cellObj, float targetSize)
    {
        var spriteRenderer = cellObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float scaleX = spriteSize.x > 0f ? (targetSize / spriteSize.x) : 1f;
            float scaleY = spriteSize.y > 0f ? (targetSize / spriteSize.y) : 1f;
            cellObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            cellObj.transform.localScale = new Vector3(targetSize, targetSize, 1f);
        }
    }

    /// <summary>
    /// Xóa toàn bộ các ô trong grid
    /// </summary>
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        if (gridCells != null)
        {
            for (int r = 0; r < gridCells.GetLength(0); r++)
            {
                for (int c = 0; c < gridCells.GetLength(1); c++)
                {
                    if (gridCells[r, c] != null)
                    {
                        DestroySafely(gridCells[r, c]);
                    }
                }
            }
            gridCells = null;
        }

        // Xóa bất kỳ child nào còn sót lại
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            toDestroy.Add(child.gameObject);
        }

        for (int i = toDestroy.Count - 1; i >= 0; i--)
        {
            DestroySafely(toDestroy[i]);
        }
    }

    private void DestroySafely(GameObject obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(obj);
            return;
        }
#endif
        Destroy(obj);
    }

    /// <summary>
    /// Lấy GameObject ô tại vị trí (hàng, cột)
    /// </summary>
    public GameObject GetCell(int row, int col)
    {
        if (gridCells == null) return null;
        if (row < 0 || row >= gridCells.GetLength(0) || col < 0 || col >= gridCells.GetLength(1))
            return null;

        return gridCells[row, col];
    }

    /// <summary>
    /// Lấy toạ độ World Space của tâm ô tại vị trí (hàng, cột)
    /// </summary>
    public Vector3 GetCellWorldPosition(int row, int col)
    {
        GameObject cell = GetCell(row, col);
        if (cell != null)
        {
            return cell.transform.position;
        }

        // Nếu chưa sinh GameObject, tính toán toạ độ lý thuyết
        float step = cellSize + spacing;
        float startX = -gridWidth * 0.5f + cellSize * 0.5f;
        float startY = gridHeight * 0.5f - cellSize * 0.5f;
        Vector3 localPos = new Vector3(startX + col * step, startY - row * step, 0f);
        return transform.TransformPoint(localPos);
    }

    /// <summary>
    /// Chuyển đổi từ toạ độ World Space sang chỉ số ô (hàng, cột) gần nhất trong Grid
    /// </summary>
    public Vector2Int? GetGridCoordinateFromWorldPos(Vector3 worldPos)
    {
        if (gridWidth <= 0f || gridHeight <= 0f) return null;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        float halfW = gridWidth * 0.5f;
        float halfH = gridHeight * 0.5f;

        // Nằm ngoài phạm vi bao của Grid
        if (localPos.x < -halfW || localPos.x > halfW || localPos.y < -halfH || localPos.y > halfH)
        {
            return null;
        }

        float step = cellSize + spacing;
        float startX = -gridWidth * 0.5f;
        float startY = gridHeight * 0.5f;

        int col = Mathf.FloorToInt((localPos.x - startX) / step);
        int row = Mathf.FloorToInt((startY - localPos.y) / step);

        col = Mathf.Clamp(col, 0, maxColumn - 1);
        row = Mathf.Clamp(row, 0, maxRow - 1);

        return new Vector2Int(row, col);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWidth, gridHeight, 0.1f));
    }
}