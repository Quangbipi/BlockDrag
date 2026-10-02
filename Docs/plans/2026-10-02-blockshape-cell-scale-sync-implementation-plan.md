# Kế Hoạch Triển Khai: Đồng Bộ Tỷ Lệ (Scale) SingleBlockPrefab Với CellObj Trong BlockGrid

> **Ngày tạo:** 2026-10-02  
> **Tài liệu thiết kế:** [`Docs/designs/2026-10-02-blockshape-cell-scale-sync-design.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-10-02-blockshape-cell-scale-sync-design.md)  
> **Trạng thái:** Chờ xác nhận (Pending Alignment)  

---

## 1. Các File Bị Ảnh Hưởng (Affected Files & Boundaries)

| File | Module (.asmdef) | Loại thay đổi | Mô tả |
| :--- | :--- | :--- | :--- |
| [`BlockGrid.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) | `Gameplay.BlockDrag` | Sửa đổi | Thêm property `CellScale`, chuyển `ApplyCellScale` thành `public` và cập nhật `CellScale`. |
| [`BlockShape.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockShape.cs) | `Gameplay.BlockDrag` | Sửa đổi | Bổ sung hàm `ApplyBlockScale` và gọi sau khi `Instantiate` từng block con 1x1. |
| [`BlockSpawner.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockSpawner.cs) | `Gameplay.BlockDrag` | Sửa đổi | Bổ sung `Awake()` tự động tìm `BlockGrid` bằng `FindObjectOfType<BlockGrid>()` nếu bị null. |

---

## 2. Kế Hoạch Từng Bước (Step-by-Step Implementation Plan)

### Bước 1: Cập nhật `BlockGrid.cs`
- Thêm property public:
  ```csharp
  public Vector3 CellScale { get; protected set; } = Vector3.one;
  ```
- Nâng cấp phương thức `ApplyCellScale` từ `private` thành `public` để tái sử dụng.
- Cập nhật giá trị `CellScale = cellObj.transform.localScale;` bên trong `ApplyCellScale`.

### Bước 2: Khắc phục lỗi `blockGrid == null` trong `BlockSpawner.cs`
- Thêm hàm `Awake()` trong `BlockSpawner.cs`:
  ```csharp
  private void Awake()
  {
      if (blockGrid == null)
      {
          blockGrid = FindObjectOfType<BlockGrid>();
      }
  }
  ```
- Đảm bảo trong `Start()` và `SpawnRandomShapeAt()` lấy được `blockGrid.CellSize` hợp lệ (> 0f).

### Bước 3: Áp dụng Scale cho các Block con trong `BlockShape.cs`
- Thêm hàm `ApplyBlockScale(GameObject blockObj, float targetSize)` trong `BlockShape.cs`.
- Trong hàm `Initialize(...)`, ngay sau khi `GameObject block = Instantiate(singleBlockPrefab, transform);`:
  - Gọi `ApplyBlockScale(block, blockSize.x);`
  - Đảm bảo `block.transform.localScale` được thiết lập chính xác bằng tỷ lệ của `cellObj` trong grid.

---

## 3. Tiêu Chí Xác Minh & Kiểm Thử (Verification & Testing)

1. **Kiểm tra biên dịch (Compilation Check)**:
   - Toàn bộ code compile không có lỗi cú pháp hay cảnh báo trong module `BlockDrag.asmdef`.
2. **Kiểm tra Runtime trong Play Mode**:
   - Khi chạy Scene, `BlockSpawner` không còn cảnh báo hoặc lỗi null reference liên quan đến `blockGrid`.
   - Các `singleBlockPrefab` được sinh ra bên trong `BlockShape` có giá trị `transform.localScale` trùng khớp 100% với `transform.localScale` của các ô `Cell_r_c` trong `BlockGrid`.
   - Khi kéo thả `BlockShape` (ở trạng thái kéo chuẩn `DragScale = 1.0`), các ô vuông của `BlockShape` khớp khít với các ô của `BlockGrid`.
