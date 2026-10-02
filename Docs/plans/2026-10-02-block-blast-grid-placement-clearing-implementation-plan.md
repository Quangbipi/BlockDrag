# Kế Hoạch Triển Khai: Cơ Chế Xếp Khối (Placement), Chỉ Báo (Indicator) & Phá Hàng (Line Clearing) Kiểu Block Blast

> **Ngày tạo:** 2026-10-02  
> **Tài liệu thiết kế:** [`Docs/designs/2026-10-02-block-blast-grid-placement-clearing-design.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-10-02-block-blast-grid-placement-clearing-design.md)  
> **Trạng thái:** Hoàn thành (Completed)  

---

## 1. Các File Bị Ảnh Hưởng (Affected Files & Boundaries)

| File | Module (.asmdef) | Loại thay đổi | Mô tả |
| :--- | :--- | :--- | :--- |
| [`BlockShape.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockShape.cs) | `Gameplay.BlockDrag` | Sửa đổi | Lưu `CurrentRotation`, thêm map `(r, c) -> GameObject`, cung cấp thuộc tính `RowsCount`, `ColumnsCount`, `HasBlockAt(r, c)` và hàm trích xuất block con. |
| [`BlockGrid.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) | `Gameplay.BlockDrag` | Sửa đổi | Quản lý ma trận `occupiedBlocks[,]`, hàm tính `(baseRow, baseCol)`, hàm kiểm tra `CanPlaceShape`, hệ thống pool `indicatorCells`, hàm `TryPlaceShape` và thuật toán quét/phá hàng `CheckAndClearLines`. |
| [`BlockDragHandler.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockDragHandler.cs) | `Gameplay.BlockDrag` | Sửa đổi | Tích hợp gọi `blockGrid.UpdateDragIndicator` trong `OnMouseDrag` và `blockGrid.TryPlaceShape` trong `OnMouseUp` (nếu không đặt được thì `ReturnToOrigin`). |
| [`BlockSpawner.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockSpawner.cs) | `Gameplay.BlockDrag` | Sửa đổi | Lắng nghe `blockGrid.OnShapePlaced` để kiểm tra khi cả 3 slot trống thì tự động gọi `SpawnNewHand()`. |

---

## 2. Kế Hoạch Từng Bước Chi Tiết (Step-by-Step Implementation Plan)

### Bước 1: Nâng cấp `BlockShape.cs`
- Thêm thuộc tính lưu góc xoay:
  ```csharp
  public BlockRotation CurrentRotation { get; private set; } = BlockRotation.Rot_0;
  ```
- Thêm `Dictionary<Vector2Int, GameObject> blockMap` để lưu trữ block tương ứng với toạ độ `(r, c)`.
- Thêm các thuộc tính & helper:
  - `public int RowsCount => shapeData != null ? shapeData.GetRotatedRowsCount(CurrentRotation) : 0;`
  - `public int ColumnsCount => shapeData != null ? shapeData.GetRotatedColumnsCount(CurrentRotation) : 0;`
  - `public bool HasBlockAt(int r, int c) => shapeData != null && shapeData.HasBlockAtRotated(r, c, CurrentRotation);`
  - `public GameObject GetBlockAt(int r, int c)`
- Trong `Initialize(...)`, gán `CurrentRotation = rotation;` và lưu `blockMap[new Vector2Int(r, c)] = block;`.

### Bước 2: Nâng cấp `BlockGrid.cs` (Quản lý trạng thái ô & Indicator)
- Thêm mảng trạng thái chiếm chỗ:
  ```csharp
  private GameObject[,] occupiedBlocks;
  ```
- Khởi tạo `occupiedBlocks = new GameObject[maxRow, maxColumn]` trong `GenerateGrid()`.
- Thêm thuộc tính kiểm tra ô:
  ```csharp
  public bool IsCellOccupied(int row, int col);
  ```
- Thêm thuật toán tính toạ độ neo `(baseRow, baseCol)`:
  ```csharp
  public bool TryGetPlacementCoordinate(BlockShape shape, Vector3 shapeWorldPos, out Vector2Int baseCoord);
  ```
- Thêm hàm kiểm tra vị trí đặt khối:
  ```csharp
  public bool CanPlaceShape(BlockShape shape, Vector2Int baseCoord);
  ```
- Thêm hệ thống hiển thị Indicator:
  - Khởi tạo pool các ô ghost cell (dùng `singleBlockPrefab` hoặc sprite cell) làm con của một `indicatorContainer`.
  - Hàm `UpdateDragIndicator(BlockShape shape, Vector3 shapeWorldPos)`:
    - Nếu `TryGetPlacementCoordinate` trả về toạ độ và `CanPlaceShape` hợp lệ: Gọi `ShowIndicator(shape, baseCoord)`.
    - Ngược lại (có ô đã bị chiếm hoặc tràn lề): Gọi `HideIndicator()`.
  - Hàm `ShowIndicator(BlockShape shape, Vector2Int baseCoord)`: Bật và định vị các ô ghost tương ứng với màu `shape.shapeData.shapeColor` (alpha 0.4f).
  - Hàm `HideIndicator()`: Ẩn toàn bộ các ô ghost.

### Bước 3: Triển khai Đặt Khối & Quét Phá Hàng (Line Clearing) trong `BlockGrid.cs`
- Thêm hàm `TryPlaceShape(BlockShape shape, Vector3 shapeWorldPos)`:
  1. Kiểm tra tính hợp lệ qua `TryGetPlacementCoordinate` và `CanPlaceShape`. Nếu không hợp lệ $\rightarrow$ `return false`.
  2. Ẩn Indicator (`HideIndicator()`).
  3. Lặp qua tất cả ô `(r, c)` của khối:
     - Lấy `blockObj = shape.GetBlockAt(r, c)`.
     - Chuyển `blockObj.transform.SetParent(transform)`.
     - Cập nhật vị trí `blockObj.transform.position = GetCellWorldPosition(baseRow + r, baseCol + c)`.
     - Đặt scale chuẩn `CellScale` và trả `sortingOrder` về bình thường.
     - Lưu vào `occupiedBlocks[baseRow + r, baseCol + c] = blockObj`.
  4. Hủy GameObject cha `shape.gameObject`.
  5. Gọi `CheckAndClearLines()`.
  6. Kích hoạt sự kiện `OnShapePlaced?.Invoke()`.
  7. Trả về `true`.
- Thêm hàm `CheckAndClearLines()`:
  1. Duyệt tất cả các hàng `r`: nếu đủ `maxColumn` ô khác null $\rightarrow$ thêm vào `fullRows`.
  2. Duyệt tất cả các cột `c`: nếu đủ `maxRow` ô khác null $\rightarrow$ thêm vào `fullCols`.
  3. Nếu có hàng/cột đầy:
     - Dùng `HashSet<Vector2Int>` gom tất cả các ô cần phá.
     - Với mỗi ô: Lấy `blockObj`, gán `occupiedBlocks[r, c] = null` ngay lập tức.
     - Chạy DOTween hiệu ứng thu nhỏ (`DOScale(Vector3.zero, 0.2f)`) và mờ dần (`DOFade(0f, 0.2f)`), kết thúc gọi `Destroy(blockObj)`.
     - Bắn sự kiện `OnLinesCleared?.Invoke(fullRows.Count, fullCols.Count, cellsToClear.Count)`.

### Bước 4: Tích hợp vào `BlockDragHandler.cs`
- Thêm tham chiếu `BlockGrid blockGrid;` (tự động tìm trong `Awake()` nếu chưa gán).
- Trong `OnMouseDrag()`: Gọi `blockGrid.UpdateDragIndicator(blockShape, transform.position)`.
- Trong `OnMouseUp()`:
  - Gọi `bool placed = blockGrid.TryPlaceShape(blockShape, transform.position)`.
  - Nếu `!placed`: Gọi `ReturnToOrigin()` để khối bay mượt về lại slot chờ.
  - Luôn đảm bảo `blockGrid.HideIndicator()` được dọn sạch.

### Bước 5: Tích hợp Cấp Khối Mới vào `BlockSpawner.cs`
- Lắng nghe sự kiện `blockGrid.OnShapePlaced`:
  - Kiểm tra nếu tất cả các slot trong `spawnSlots` không còn khối con nào (`childCount == 0`):
    - Tự động gọi `SpawnNewHand()` để sinh 3 khối mới lên 3 slot.

---

## 3. Tiêu Chí Xác Minh & Kiểm Thử (Verification & Testing)

1. **Kiểm tra Biên Dịch (Compilation Check)**:
   - Toàn bộ code compile thành công không lỗi trong module `BlockDrag.asmdef`.
2. **Kiểm tra Kéo Thả & Indicator (Drag & Ghost Preview)**:
   - Kéo khối từ slot vào Grid: Indicator hiển thị đúng hình dạng khối, đúng vị trí các ô gạch và có độ trong suốt mờ mờ.
   - Kéo khối ra ngoài Grid: Indicator tự động ẩn.
   - Kéo khối đè lên ô đã có gạch sẵn: Indicator lập tức biến mất (ẩn hoàn toàn) theo đúng yêu cầu đề bài.
3. **Kiểm tra Đặt Khối (Placement)**:
   - Thả tay khi Indicator đang hiển thị: Khối tách khỏi slot, gắn cố định vào Grid tại các ô mục tiêu.
   - Thả tay khi không có Indicator (vị trí không hợp lệ hoặc đè lên ô đã có): Khối tự động bay về lại vị trí slot ban đầu (`ReturnToOrigin`).
4. **Kiểm tra Phá Hàng (Line Clearing)**:
   - Xếp đủ 1 hàng ngang: Toàn bộ hàng ngang biến mất kèm animation thu nhỏ, các ô trong hàng trở lại trạng thái rỗng và có thể đặt khối mới vào tiếp.
   - Xếp đủ 1 cột dọc: Toàn bộ cột dọc biến mất và cell trở lại rỗng.
   - Xếp tạo combo vừa 1 hàng ngang vừa 1 cột dọc giao nhau: Cả hàng và cột biến mất chính xác, ô giao nhau không bị lỗi hay exception.
5. **Kiểm tra Tự Động Sinh Khối (Spawn New Hand)**:
   - Đặt lần lượt cả 3 khối trong 3 slot lên Grid: Ngay khi khối thứ 3 được đặt thành công, 3 khối mới lập tức được sinh ra tại 3 slot.
