using System.Collections.Generic;
using DG.Tweening;
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
    [Header("Positioning & Offset")]
    [Tooltip("Nếu bật, tự động căn vị trí Grid theo tỷ lệ màn hình Camera. Nếu tắt, giữ nguyên transform.position")]
    [SerializeField] protected bool centerOnCamera = true;
    [Tooltip("Tỷ lệ vị trí tâm Grid theo chiều ngang màn hình (0: Mép trái, 0.5: Giữa, 1: Mép phải)")]
    [Range(0f, 1f)]
    [SerializeField] protected float screenPositionX = 0.5f;
    [Tooltip("Tỷ lệ vị trí tâm Grid theo chiều dọc màn hình (0: Mép dưới, 0.5: Giữa, 0.6: Cách đáy 60%, 1: Mép trên)")]
    [Range(0f, 1f)]
    [SerializeField] protected float screenPositionY = 0.5f;

    [Header("Prefabs & References")]
    [Tooltip("Prefab ô nền vuông 1x1")]
    [SerializeField] protected GameObject cellPrefab;
    [Tooltip("Camera chính dùng để đo kích thước màn hình (nếu để trống sẽ tự lấy Camera.main)")]
    [SerializeField] protected Camera targetCamera;

    [Header("Indicator Settings")]
    [Tooltip("Độ trong suốt của Indicator khi kéo khối hợp lệ")]
    [Range(0f, 1f)]
    [SerializeField] protected float indicatorAlpha = 0.4f;
    [Tooltip("Sorting Order của Indicator (nên lớn hơn cell nền nhưng nhỏ hơn khối đang kéo)")]
    [SerializeField] protected int indicatorSortingOrder = 5;

    [Header("Line Clearing Settings")]
    [Tooltip("Thời gian animation thu nhỏ khi phá hàng (giây)")]
    [SerializeField] protected float clearDuration = 0.25f;
    [Tooltip("Kiểu ease animation khi thu nhỏ phá hàng")]
    [SerializeField] protected Ease clearEase = Ease.InBack;

    // Events
    public event System.Action OnShapePlaced;
    public event System.Action<int> OnShapePlacedWithCount;
    public event System.Action<int, int, int> OnLinesCleared; // (rowsCleared, colsCleared, totalCellsCleared)

    // Runtime variables
    protected GameObject[,] gridCells;
    protected GameObject[,] occupiedBlocks;
    protected Transform indicatorRoot;
    protected readonly List<GameObject> indicatorPool = new List<GameObject>();
    protected float cellSize;
    protected float gridWidth;
    protected float gridHeight;

    public float CellSize => cellSize;
    public float GridWidth => gridWidth;
    public float GridHeight => gridHeight;
    public int MaxColumn => maxColumn;
    public int MaxRow => maxRow;
    public float Spacing => spacing;
    public Vector3 CellScale { get; protected set; } = Vector3.one;
    public bool CenterOnCamera
    {
        get => centerOnCamera;
        set
        {
            centerOnCamera = value;
            UpdateGridPosition();
        }
    }
    public float ScreenPositionX
    {
        get => screenPositionX;
        set
        {
            screenPositionX = Mathf.Clamp01(value);
            UpdateGridPosition();
        }
    }
    public float ScreenPositionY
    {
        get => screenPositionY;
        set
        {
            screenPositionY = Mathf.Clamp01(value);
            UpdateGridPosition();
        }
    }

    protected virtual void Awake()
    {
        int cols = Mathf.Max(1, maxColumn);
        int rows = Mathf.Max(1, maxRow);
        if (occupiedBlocks == null)
        {
            occupiedBlocks = new GameObject[rows, cols];
        }
    }

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

        // Cập nhật vị trí grid theo tỷ lệ màn hình Camera nếu được bật
        UpdateGridPosition();
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
        occupiedBlocks = new GameObject[rows, cols];
        EnsureIndicatorRoot();

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
    public void ApplyCellScale(GameObject cellObj, float targetSize)
    {
        ApplyScale(cellObj, targetSize);
        if (cellObj != null)
        {
            CellScale = cellObj.transform.localScale;
        }
    }

    /// <summary>
    /// Hàm tiện ích căn chỉnh scale của một GameObject chứa SpriteRenderer theo targetSize
    /// </summary>
    public static void ApplyScale(GameObject targetObj, float targetSize)
    {
        if (targetObj == null || targetSize <= 0f) return;

        var spriteRenderer = targetObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float scaleX = spriteSize.x > 0f ? (targetSize / spriteSize.x) : 1f;
            float scaleY = spriteSize.y > 0f ? (targetSize / spriteSize.y) : 1f;
            targetObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            targetObj.transform.localScale = new Vector3(targetSize, targetSize, 1f);
        }
    }

    /// <summary>
    /// Xóa toàn bộ các ô trong grid
    /// </summary>
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        if (occupiedBlocks != null)
        {
            for (int r = 0; r < occupiedBlocks.GetLength(0); r++)
            {
                for (int c = 0; c < occupiedBlocks.GetLength(1); c++)
                {
                    if (occupiedBlocks[r, c] != null)
                    {
                        DestroySafely(occupiedBlocks[r, c]);
                    }
                }
            }
            occupiedBlocks = null;
        }

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

        if (indicatorPool != null)
        {
            for (int i = 0; i < indicatorPool.Count; i++)
            {
                if (indicatorPool[i] != null)
                {
                    DestroySafely(indicatorPool[i]);
                }
            }
            indicatorPool.Clear();
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

    /// <summary>
    /// Xóa toàn bộ các khối gạch đang chiếm chỗ trên Grid khi chơi lại (Replay), giữ nguyên các ô nền Grid
    /// </summary>
    [ContextMenu("Reset Board")]
    public void ResetBoard()
    {
        HideIndicator();

        if (occupiedBlocks != null)
        {
            int rows = occupiedBlocks.GetLength(0);
            int cols = occupiedBlocks.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (occupiedBlocks[r, c] != null)
                    {
                        DestroySafely(occupiedBlocks[r, c]);
                        occupiedBlocks[r, c] = null;
                    }
                }
            }
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
    /// Kiểm tra ô (row, col) đã có block gạch hay chưa
    /// </summary>
    public bool IsCellOccupied(int row, int col)
    {
        if (occupiedBlocks == null) return false;
        if (row < 0 || row >= maxRow || col < 0 || col >= maxColumn) return false;
        return occupiedBlocks[row, col] != null;
    }

    /// <summary>
    /// Tính toán toạ độ gốc (baseRow, baseCol) của khối shape trên grid dựa trên vị trí con trỏ chuột / khối đang kéo
    /// </summary>
    public bool TryGetPlacementCoordinate(BlockShape shape, Vector3 shapeWorldPos, out Vector2Int baseCoord)
    {
        baseCoord = Vector2Int.zero;
        if (shape == null || gridWidth <= 0f || gridHeight <= 0f) return false;

        int shapeRows = shape.RowsCount;
        int shapeCols = shape.ColumnsCount;
        if (shapeRows <= 0 || shapeCols <= 0) return false;

        float step = cellSize + spacing;
        float startX = -gridWidth * 0.5f + cellSize * 0.5f;
        float startY = gridHeight * 0.5f - cellSize * 0.5f;

        Vector3 shapeLocalPos = transform.InverseTransformPoint(shapeWorldPos);

        float continuousCol = (shapeLocalPos.x - startX) / step - (shapeCols - 1) * 0.5f;
        float continuousRow = (startY - shapeLocalPos.y) / step - (shapeRows - 1) * 0.5f;

        int baseCol = Mathf.RoundToInt(continuousCol);
        int baseRow = Mathf.RoundToInt(continuousRow);

        baseCoord = new Vector2Int(baseRow, baseCol);
        return true;
    }

    /// <summary>
    /// Kiểm tra xem toàn bộ các ô của shape khi đặt tại baseCoord có hợp lệ (trong bounds & không bị trùng ô đã có)
    /// </summary>
    public bool CanPlaceShape(BlockShape shape, Vector2Int baseCoord)
    {
        if (shape == null) return false;
        int rows = shape.RowsCount;
        int cols = shape.ColumnsCount;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (shape.HasBlockAt(r, c))
                {
                    int targetR = baseCoord.x + r;
                    int targetC = baseCoord.y + c;

                    // 1. Phải nằm hoàn toàn trong phạm vi Grid
                    if (targetR < 0 || targetR >= maxRow || targetC < 0 || targetC >= maxColumn)
                    {
                        return false;
                    }

                    // 2. Ô đó chưa được chứa khối gạch nào
                    if (IsCellOccupied(targetR, targetC))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Kiểm tra xem khối shape này có thể đặt vào bất kỳ vị trí nào trên grid hay không (dùng check GameOver)
    /// </summary>
    public bool CanShapeBePlacedAnywhere(BlockShape shape)
    {
        if (shape == null) return false;
        int shapeRows = shape.RowsCount;
        int shapeCols = shape.ColumnsCount;
        int maxR = maxRow - shapeRows;
        int maxC = maxColumn - shapeCols;

        for (int r = 0; r <= maxR; r++)
        {
            for (int c = 0; c <= maxC; c++)
            {
                if (CanPlaceShape(shape, new Vector2Int(r, c)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Cập nhật hiển thị indicator khi kéo khối gạch.
    /// Nếu vị trí hợp lệ: hiện preview indicator mờ.
    /// Nếu vị trí không hợp lệ (ngoài grid hoặc trùng ô đã có gạch): ẩn hoàn toàn indicator.
    /// </summary>
    public void UpdateDragIndicator(BlockShape shape, Vector3 shapeWorldPos)
    {
        if (shape == null)
        {
            HideIndicator();
            return;
        }

        if (TryGetPlacementCoordinate(shape, shapeWorldPos, out Vector2Int baseCoord))
        {
            if (CanPlaceShape(shape, baseCoord))
            {
                ShowIndicator(shape, baseCoord);
                return;
            }
        }

        // Ngoài grid hoặc đã có block chiếm chỗ -> Không hiện indicator
        HideIndicator();
    }

    /// <summary>
    /// Hiển thị ghost indicator tại các ô mục tiêu
    /// </summary>
    public void ShowIndicator(BlockShape shape, Vector2Int baseCoord)
    {
        if (shape == null)
        {
            HideIndicator();
            return;
        }

        EnsureIndicatorRoot();

        int rows = shape.RowsCount;
        int cols = shape.ColumnsCount;
        int neededCount = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (shape.HasBlockAt(r, c))
                {
                    neededCount++;
                }
            }
        }

        while (indicatorPool.Count < neededCount)
        {
            indicatorPool.Add(CreateIndicatorCell());
        }

        Color ghostColor = shape.shapeData != null ? shape.shapeData.shapeColor : Color.white;
        ghostColor.a = indicatorAlpha;

        int indicatorIdx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (shape.HasBlockAt(r, c))
                {
                    int targetR = baseCoord.x + r;
                    int targetC = baseCoord.y + c;

                    GameObject indObj = indicatorPool[indicatorIdx];
                    indObj.transform.position = GetCellWorldPosition(targetR, targetC);
                    indObj.transform.localScale = CellScale;

                    var sr = indObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = ghostColor;
                        sr.sortingOrder = indicatorSortingOrder;
                    }

                    indObj.SetActive(true);
                    indicatorIdx++;
                }
            }
        }

        for (int i = indicatorIdx; i < indicatorPool.Count; i++)
        {
            indicatorPool[i].SetActive(false);
        }
    }

    /// <summary>
    /// Ẩn toàn bộ indicator preview
    /// </summary>
    public void HideIndicator()
    {
        if (indicatorPool == null) return;
        for (int i = 0; i < indicatorPool.Count; i++)
        {
            if (indicatorPool[i] != null)
            {
                indicatorPool[i].SetActive(false);
            }
        }
    }

    private void EnsureIndicatorRoot()
    {
        if (indicatorRoot == null)
        {
            var existing = transform.Find("IndicatorRoot");
            if (existing != null)
            {
                indicatorRoot = existing;
            }
            else
            {
                GameObject rootObj = new GameObject("IndicatorRoot");
                rootObj.transform.SetParent(transform);
                rootObj.transform.localPosition = Vector3.zero;
                rootObj.transform.localScale = Vector3.one;
                indicatorRoot = rootObj.transform;
            }
        }
    }

    private GameObject CreateIndicatorCell()
    {
        GameObject obj;
        if (cellPrefab != null)
        {
            obj = Instantiate(cellPrefab, indicatorRoot);
        }
        else
        {
            obj = new GameObject("IndicatorCell");
            obj.transform.SetParent(indicatorRoot);
            obj.AddComponent<SpriteRenderer>();
        }

        obj.name = "IndicatorCell";

        // Tắt mọi collider trên indicator cell để tránh chặn raycast
        var colliders = obj.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        ApplyCellScale(obj, cellSize);
        obj.SetActive(false);
        return obj;
    }

    /// <summary>
    /// Thử xếp khối shape lên grid tại toạ độ world chuột thả ra.
    /// Trả về true nếu đặt thành công, false nếu không thể đặt.
    /// </summary>
    public bool TryPlaceShape(BlockShape shape, Vector3 shapeWorldPos)
    {
        HideIndicator();

        if (shape == null) return false;

        if (!TryGetPlacementCoordinate(shape, shapeWorldPos, out Vector2Int baseCoord))
        {
            return false;
        }

        if (!CanPlaceShape(shape, baseCoord))
        {
            return false;
        }

        if (occupiedBlocks == null)
        {
            occupiedBlocks = new GameObject[maxRow, maxColumn];
        }

        int rows = shape.RowsCount;
        int cols = shape.ColumnsCount;

        int placedBlockCount = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (shape.HasBlockAt(r, c))
                {
                    placedBlockCount++;
                    int targetR = baseCoord.x + r;
                    int targetC = baseCoord.y + c;

                    GameObject blockObj = shape.GetBlockAt(r, c);
                    if (blockObj != null)
                    {
                        // Chuyển block sang làm con của Grid
                        blockObj.transform.SetParent(transform);
                        blockObj.transform.position = GetCellWorldPosition(targetR, targetC);
                        blockObj.transform.localScale = CellScale;

                        var sr = blockObj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingOrder = 2; // Nằm trên ô nền background
                        }

                        // Vô hiệu hóa collider để không còn bị raycast kéo lại
                        var col = blockObj.GetComponent<Collider2D>();
                        if (col != null)
                        {
                            col.enabled = false;
                        }

                        occupiedBlocks[targetR, targetC] = blockObj;
                    }
                }
            }
        }

        // Hủy khối shape cha và tách khỏi slot ngay lập tức
        shape.transform.SetParent(null);
        Destroy(shape.gameObject);

        // Kiểm tra và phá hủy các hàng ngang / cột dọc đã lấp đầy
        CheckAndClearLines();

        // Bắn sự kiện đặt khối thành công
        OnShapePlaced?.Invoke();
        OnShapePlacedWithCount?.Invoke(placedBlockCount);

        return true;
    }

    /// <summary>
    /// Quét và phá hủy các hàng ngang hoặc cột dọc đã được lấp đầy (Block Blast Line Clear)
    /// </summary>
    public void CheckAndClearLines()
    {
        if (occupiedBlocks == null) return;

        List<int> fullRows = new List<int>();
        for (int r = 0; r < maxRow; r++)
        {
            bool isFull = true;
            for (int c = 0; c < maxColumn; c++)
            {
                if (occupiedBlocks[r, c] == null)
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) fullRows.Add(r);
        }

        List<int> fullCols = new List<int>();
        for (int c = 0; c < maxColumn; c++)
        {
            bool isFull = true;
            for (int r = 0; r < maxRow; r++)
            {
                if (occupiedBlocks[r, c] == null)
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) fullCols.Add(c);
        }

        if (fullRows.Count == 0 && fullCols.Count == 0)
        {
            return;
        }

        // Gom các ô cần phá (dùng HashSet để ô giao nhau chỉ xử lý 1 lần)
        HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
        foreach (int r in fullRows)
        {
            for (int c = 0; c < maxColumn; c++)
            {
                cellsToClear.Add(new Vector2Int(r, c));
            }
        }

        foreach (int c in fullCols)
        {
            for (int r = 0; r < maxRow; r++)
            {
                cellsToClear.Add(new Vector2Int(r, c));
            }
        }

        foreach (var cell in cellsToClear)
        {
            GameObject blockObj = occupiedBlocks[cell.x, cell.y];
            occupiedBlocks[cell.x, cell.y] = null; // Trả cell về rỗng ngay lập tức

            if (blockObj != null)
            {
                AnimateAndDestroyBlock(blockObj);
            }
        }

        OnLinesCleared?.Invoke(fullRows.Count, fullCols.Count, cellsToClear.Count);
    }

    private void AnimateAndDestroyBlock(GameObject blockObj)
    {
        if (blockObj == null) return;

        var sr = blockObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOFade(0f, clearDuration).SetEase(Ease.InQuad);
        }

        blockObj.transform.DOScale(Vector3.zero, clearDuration)
            .SetEase(clearEase)
            .OnComplete(() =>
            {
                if (blockObj != null)
                {
                    Destroy(blockObj);
                }
            });
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

    /// <summary>
    /// Cập nhật vị trí của Grid trên màn hình Camera dựa trên tỷ lệ Viewport (screenPositionX, screenPositionY)
    /// </summary>
    public void UpdateGridPosition()
    {
        if (!centerOnCamera) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Vector3 targetPos = cam.ViewportToWorldPoint(new Vector3(screenPositionX, screenPositionY, cam.nearClipPlane));
        targetPos.z = transform.position.z;
        transform.position = targetPos;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        screenPositionX = Mathf.Clamp01(screenPositionX);
        screenPositionY = Mathf.Clamp01(screenPositionY);
        marginPercent = Mathf.Clamp(marginPercent, 0f, 0.4f);

        if (centerOnCamera)
        {
            UpdateGridPosition();
        }
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWidth, gridHeight, 0.1f));
    }
}