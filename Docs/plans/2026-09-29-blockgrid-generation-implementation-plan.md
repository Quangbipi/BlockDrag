# Kế Hoạch Triển Khai: Sinh Lưới Tự Động (BlockGrid Implementation Plan)

> **Ngày tạo:** 2026-09-29  
> **Tài liệu thiết kế đi kèm:** [`Docs/designs/2026-09-29-blockgrid-generation-design.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-09-29-blockgrid-generation-design.md)  
> **Mục tiêu:** Cập nhật file [`BlockGrid.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) sinh lưới ô vuông căn giữa màn hình thiết bị, cách đều trái/phải 10% chiều ngang màn hình và hỗ trợ cấu hình spacing.

---

## 1. Danh Sách File Ảnh Hưởng (Impacted Files & Boundaries)

| File | Hành động | Trách nhiệm |
| :--- | :--- | :--- |
| [`Assets/_Game/_BlockDrag/BlockGrid.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) | **Modify** | Hiện thực thuật toán tính toán kích thước, sinh lưới ô nền và cung cấp API truy vấn |
| [`Docs/designs/2026-09-29-blockgrid-generation-design.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-09-29-blockgrid-generation-design.md) | **Created** | Tài liệu đặc tả thiết kế, mô hình toán học và sơ đồ Mermaid |
| [`Docs/plans/2026-09-29-blockgrid-generation-implementation-plan.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/plans/2026-09-29-blockgrid-generation-implementation-plan.md) | **Created** | Kế hoạch triển khai từng bước và tiêu chí kiểm thử |

---

## 2. Kế Hoạch Triển Khai Từng Bước (Step-by-Step Implementation)

### Bước 1: Khai báo cấu trúc dữ liệu và tham số cấu hình trên `BlockGrid`
- Giữ nguyên kế thừa `MonoBehaviour`.
- Khai báo các serialized fields:
  ```csharp
  [Header("Grid Dimensions")]
  [SerializeField] protected int maxColumn = 8;
  [SerializeField] protected int maxRow = 8;
  [Tooltip("Khoảng cách giữa các ô")]
  [SerializeField] protected float spacing = 0.05f;
  [Tooltip("Tỷ lệ lề 2 bên màn hình (0.1 = 10%)")]
  [SerializeField] protected float marginPercent = 0.1f;

  [Header("Visual & Prefabs")]
  [Tooltip("Prefab ô nền 1x1")]
  [SerializeField] protected GameObject cellPrefab;
  [Tooltip("Camera hiển thị lưới (mặc định lấy Camera.main)")]
  [SerializeField] protected Camera targetCamera;
  ```
- Khai báo mảng 2D lưu trữ các GameObject ô lưới: `protected GameObject[,] gridCells;`

### Bước 2: Hiện thực thuật toán tính toán kích thước (`CalculateGridParameters`)
- Đo đạc kích thước World Space thông qua Orthographic Camera:
  - $\text{screenHeight} = 2 \times \text{cam.orthographicSize}$
  - $\text{screenWidth} = \text{screenHeight} \times \text{cam.aspect}$
  - $\text{usableWidth} = \text{screenWidth} \times (1 - 2 \times \text{marginPercent})$
- Tính toán kích thước cạnh ô vuông (`cellSize`):
  - $\text{cellSize} = (\text{usableWidth} - (\text{maxColumn} - 1) \times \text{spacing}) / \text{maxColumn}$
  - Cập nhật $\text{gridWidth} = \text{usableWidth}$ và $\text{gridHeight} = \text{maxRow} \times \text{cellSize} + (\text{maxRow} - 1) \times \text{spacing}$.

### Bước 3: Hiện thực logic sinh ô (`GenerateGrid`) và dọn dẹp (`ClearGrid`)
- `ClearGrid()`:
  - Xóa các đối tượng con đang tồn tại dưới `transform` (hỗ trợ cả khi chạy trong Editor thông qua `DestroyImmediate` và Play mode thông qua `Destroy`).
- `GenerateGrid()`:
  - Khởi tạo mảng `gridCells = new GameObject[maxRow, maxColumn];`
  - Tính tọa độ gốc $\text{startX}, \text{startY}$ cân đối theo tâm.
  - Vòng lặp $r = 0 \to \text{maxRow} - 1$ và $c = 0 \to \text{maxColumn} - 1$:
    - Tạo `GameObject cell = Instantiate(cellPrefab, transform);`
    - Đặt tên: `$"Cell_{r}_{c}"`
    - Gán `localPosition` tương ứng.
    - Scale sprite: đo kích thước gốc của `SpriteRenderer.sprite.bounds.size` trên prefab và gán `localScale` sao cho bề rộng và bề cao của ô bằng đúng `cellSize`.
    - Lưu vào `gridCells[r, c]`.

### Bước 4: Tích hợp ContextMenu & Vòng đời Unity
- Thêm `[ContextMenu("Generate Grid")]` và `[ContextMenu("Clear Grid")]` trên hàm để tiện thử nghiệm trực quan trong Unity Editor Scene view mà không cần bấm Play.
- Gọi `GenerateGrid()` trong hàm `Start()` khi chạy runtime.

### Bước 5: Cung cấp API phục vụ logic Gameplay tiếp theo
- Properties công khai:
  - `public float CellSize => cellSize;`
  - `public float GridWidth => gridWidth;`
  - `public float GridHeight => gridHeight;`
- Hàm tiện ích:
  - `public GameObject GetCell(int row, int col)`
  - `public Vector3 GetCellWorldPosition(int row, int col)`
  - `public Vector2Int? GetGridCoordinateFromWorldPos(Vector3 worldPos)`

---

## 3. Tiêu Chí Kiểm Thử & Xác Minh (Verification Criteria)

1. **Kiểm tra biên dịch C#**:
   - Chạy lệnh hoặc build xác nhận không có lỗi cú pháp, tuân thủ đúng assembly `BlockDrag.asmdef`.
2. **Kiểm tra tỷ lệ 10% lề màn hình**:
   - Khi thay đổi tỷ lệ màn hình (Aspect Ratio 9:16, 18:9, 19.5:9 trong Game View):
     - Mép trái nhất của ô `[0, 0]` cách mép trái màn hình đúng 10% chiều rộng màn hình.
     - Mép phải nhất của ô `[0, maxColumn - 1]` cách mép phải màn hình đúng 10% chiều rộng màn hình.
3. **Kiểm tra tham số `spacing`**:
   - Khi tăng/giảm `spacing`, các ô tự động co giãn (`cellSize` thay đổi tương ứng) sao cho tổng chiều ngang cả lưới luôn giữ nguyên đúng 80% chiều rộng màn hình.
4. **Kiểm tra tương thích với `BlockShape`**:
   - Giá trị `CellSize` trả về từ `BlockGrid` có thể truyền trực tiếp vào `BlockShape.Initialize(..., cellSize: grid.CellSize)` để kích thước các khối di chuyển khớp với từng ô của lưới.
