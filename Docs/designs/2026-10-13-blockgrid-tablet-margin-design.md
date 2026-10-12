# Thiết Kế Kỹ Thuật: Margin Grid Theo Loại Màn Hình (Tablet / iPad)

> **Ngày tạo:** 2026-10-13  
> **Tài liệu tham chiếu:** [`.agents/rules/architecture.md`](../../.agents/rules/architecture.md)  
> **Files liên quan:**  
> - [`Assets/_Game/_BlockDrag/BlockGrid.cs`](../../Assets/_Game/_BlockDrag/BlockGrid.cs)  

---

## 1. Bối Cảnh & Yêu Cầu

### 1.1. Hiện trạng
- `BlockGrid.marginPercent` (Inspector, mặc định 0.1) quyết định lề trái/phải của grid.
- `CalculateGridParameters()` dùng nó để tính `usableWidth = screenWorldWidth * (1 - 2 * marginPercent)`, rồi suy ra `cellSize`.
- Grid luôn chiếm theo **chiều ngang** màn hình. Trên tablet/iPad (tỉ lệ gần 4:3), màn hình rộng hơn nhiều so với điện thoại nên grid to quá mức và lấn chiều dọc (đè lên khu spawn khối và UI).

### 1.2. Mục tiêu
1. Trên tablet và iPad: margin = **0.2**.
2. Trên các thiết bị khác: dùng giá trị `marginPercent` chỉnh trong Inspector (không thay đổi hành vi hiện tại).
3. Giá trị 0.2 và ngưỡng nhận diện tablet chỉnh được trong Inspector, không hard-code.
4. Hoạt động được trong Editor Game view (đổi resolution sang iPad để xem ngay).

### 1.3. Ngoài phạm vi
- Không đổi `spacing`, `screenPositionX/Y` theo thiết bị.
- Không tự cập nhật lại grid khi xoay màn hình / đổi resolution lúc runtime (grid hiện chỉ tính ở `Start`).

---

## 2. Thiết Kế

### 2.1. Cách nhận diện tablet
Dùng **tỉ lệ cạnh ngắn / cạnh dài** của màn hình (`Screen.width`, `Screen.height`):

| Thiết bị | Tỉ lệ | ngắn/dài |
|---|---|---|
| iPhone X trở lên | 9:19.5 | ~0.46 |
| Điện thoại Android phổ biến | 9:16 – 9:21 | 0.43 – 0.56 |
| iPad (mọi đời) | 3:4 – ~3:4.3 | 0.70 – 0.75 |
| Tablet Android | 10:16 – 3:4 | 0.625 – 0.75 |

Ngưỡng mặc định **0.6**: `ratio >= 0.6` → tablet. Điện thoại không có máy nào vượt 0.5625, tablet không có máy nào dưới 0.625, nên có khoảng an toàn ở cả hai phía.

Lý do không dùng cách khác:
- `Screen.dpi` + đường chéo vật lý: nhiều thiết bị Android báo dpi sai, và trả 0 trong Editor.
- `SystemInfo.deviceModel` (chuỗi "iPad"): chỉ áp dụng iOS, không bắt được tablet Android, không test được trong Editor.
- Vấn đề thực tế là **tỉ lệ màn hình** làm grid quá to, nên đo tỉ lệ là đúng nguyên nhân. Điện thoại gập (Galaxy Fold mở ra, ~0.85) cũng sẽ nhận margin 0.2, đó là hành vi mong muốn.

### 2.2. Thay đổi trong `BlockGrid`
Thêm 2 trường Inspector trong header `Spacing & Margins`:

```csharp
[Tooltip("Lề trái/phải khi chạy trên tablet / iPad")]
[Range(0f, 0.4f)]
[SerializeField] protected float tabletMarginPercent = 0.2f;
[Tooltip("Tỷ lệ cạnh ngắn/cạnh dài màn hình từ ngưỡng này trở lên được coi là tablet (điện thoại ≤ 0.56, iPad ≈ 0.75)")]
[Range(0.5f, 1f)]
[SerializeField] protected float tabletAspectThreshold = 0.6f;
```

Thêm 2 hàm:

```csharp
protected bool IsTabletScreen()          // short/long >= tabletAspectThreshold
protected float GetEffectiveMarginPercent() // IsTabletScreen() ? tabletMarginPercent : marginPercent
```

`CalculateGridParameters()` đổi `marginPercent` thành `GetEffectiveMarginPercent()`. `marginPercent` trong Inspector giữ nguyên ý nghĩa và giá trị.

### 2.3. Luồng xử lý

```mermaid
flowchart TD
    A[Start] --> B[GenerateGrid]
    B --> C[CalculateGridParameters]
    C --> D[GetEffectiveMarginPercent]
    D --> E{IsTabletScreen?<br/>min(w,h) / max(w,h) >= tabletAspectThreshold}
    E -- Có: iPad / tablet --> F[margin = tabletMarginPercent 0.2]
    E -- Không: điện thoại --> G[margin = marginPercent từ Inspector]
    F --> H["usableWidth = screenWorldWidth * (1 - 2*margin)"]
    G --> H
    H --> I[cellSize, gridWidth, gridHeight]
    I --> J[UpdateGridPosition]
```

### 2.4. Ảnh hưởng & kiến trúc
- Chỉ sửa `BlockGrid.cs` trong assembly `BlockDrag`; không thêm phụ thuộc mới, không chạm `BlockDrag.UI` hay package `Quang.*`.
- `cellSize` thay đổi trên tablet → các thứ đọc `cellSize` (scale khối khi thả vào grid, preview) tự đồng bộ theo, vì đều lấy từ `BlockGrid`.
- Scene `GameScene.unity` sẽ serialize thêm 2 trường mới với giá trị mặc định; không cần migrate dữ liệu.

### 2.5. Kiểm thử
- EditMode test cho phép tính thuần (tách thành hàm static nhận `width, height, threshold`): 1170x2532 → không phải tablet; 1536x2048 → tablet; 1600x2560 (10:16) → tablet; 1080x1920 → không phải tablet; landscape 2048x1536 → tablet.
- Thủ công: Game view đặt iPhone 12 (margin Inspector) và iPad (margin 0.2), so sánh kích thước grid.
