# Kế Hoạch Triển Khai: Cấu Hình Tỷ Lệ Vị Trí Grid Trên Màn Hình (BlockGrid Screen Position Implementation Plan)

> **Ngày tạo:** 2026-09-29  
> **Tài liệu thiết kế đi kèm:** [`Docs/designs/2026-09-29-blockgrid-screen-position-design.md`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Docs/designs/2026-09-29-blockgrid-screen-position-design.md)  
> **Mục tiêu:** Bổ sung cấu hình tỷ lệ vị trí màn hình Camera (Viewport Ratio) cho [`BlockGrid.cs`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) với hai thanh trượt X và Y ([0f, 1f]), cập nhật tức thì trên Scene View thông qua `OnValidate()`.

---

## 1. Danh Sách File Ảnh Hưởng (Impacted Files & Boundaries)

| File | Hành động | Trách nhiệm |
| :--- | :--- | :--- |
| [`Assets/_Game/_BlockDrag/BlockGrid.cs`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) | **Modify** | Bổ sung các biến `screenPositionX`, `screenPositionY`, hàm `UpdateGridPosition()`, `OnValidate()` và các public properties |
| [`Docs/designs/2026-09-29-blockgrid-screen-position-design.md`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Docs/designs/2026-09-29-blockgrid-screen-position-design.md) | **Created** | Tài liệu đặc tả thiết kế, mô hình Viewport và sơ đồ tương tác |
| [`Docs/plans/2026-09-29-blockgrid-screen-position-implementation-plan.md`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Docs/plans/2026-09-29-blockgrid-screen-position-implementation-plan.md) | **Created** | Kế hoạch triển khai từng bước và tiêu chuẩn nghiệm thu |

---

## 2. Kế Hoạch Triển Khai Từng Bước (Step-by-Step Implementation)

### Bước 1: Khai báo các trường Serialized mới trên `BlockGrid.cs`
- Thêm trường `screenPositionX` với `[Range(0f, 1f)]`, giá trị mặc định là `0.5f` (căn giữa theo chiều ngang).
- Thêm trường `screenPositionY` với `[Range(0f, 1f)]`, giá trị mặc định là `0.5f` (căn giữa theo chiều dọc; ví dụ `0.6f` sẽ cách cạnh dưới $60\%$).
- Giữ nguyên `centerOnCamera` (mặc định `true`) để kiểm soát bật/tắt tự động căn theo màn hình, đồng thời cập nhật tooltip mô tả rõ ràng.

### Bước 2: Tách biệt logic cập nhật vị trí (`UpdateGridPosition`)
- Tạo hàm công khai `public void UpdateGridPosition()`.
- Lấy `Camera cam = targetCamera != null ? targetCamera : Camera.main`.
- Nếu `centerOnCamera == true` và camera hợp lệ:
  ```csharp
  Vector3 targetPos = cam.ViewportToWorldPoint(new Vector3(screenPositionX, screenPositionY, cam.nearClipPlane));
  targetPos.z = transform.position.z;
  transform.position = targetPos;
  ```
- Gọi `UpdateGridPosition()` bên trong `CalculateGridParameters()`.

### Bước 3: Tích hợp `OnValidate()` cho Editor phản hồi thời gian thực
- Triển khai `OnValidate()`:
  - Clamp các giá trị `screenPositionX` và `screenPositionY` trong đoạn $[0, 1]$.
  - Tự động gọi `UpdateGridPosition()` khi giá trị trên Inspector thay đổi, giúp Designer thấy ngay vị trí mới của Grid trên Scene View mà không cần chạy game hay bấm Generate lại ô.

### Bước 4: Khai báo các Public Properties
- Cung cấp getter/setter cho `ScreenPositionX`, `ScreenPositionY`, `CenterOnCamera`.
- Khi gán giá trị mới qua setter trong runtime, tự động gọi `UpdateGridPosition()`.

---

## 3. Tiêu Chí Kiểm Thử & Nghiệm Thu (Verification Criteria)

1. **Kiểm tra Inspector**:
   - Xuất hiện 2 slider `Screen Position X` và `Screen Position Y` với giới hạn từ `0` đến `1`.
   - Giá trị mặc định là `0.5` cho cả X và Y.
2. **Kiểm tra trực quan trong Unity Editor**:
   - Khi kéo `screenPositionY` lên `0.6`, tâm của Grid dịch chuyển lên vị trí cách mép dưới màn hình Camera $60\%$.
   - Khi kéo `screenPositionX` từ `0.5` sang `0.3`, tâm Grid dịch sang trái màn hình Camera $30\%$.
   - Khi bỏ chọn `centerOnCamera`, Grid giữ nguyên tọa độ tự do trong Scene.
3. **Kiểm tra biên dịch & tính tương thích**:
   - Code biên dịch thành công, không có lỗi linter/compiler trong assembly `BlockDrag.asmdef`.
   - Scene sẵn có (`GameScene.unity`) vẫn giữ nguyên thiết lập mà không bị mất tham chiếu.
