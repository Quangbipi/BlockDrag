# Combo Text Screen Boundary Clamping Implementation Plan

**Date:** 2026-10-05  
**Feature:** Căn chỉnh toạ độ Combo Text Effect tránh bị khuất ở mép màn hình / camera  
**Design Reference:** [Docs/designs/2026-10-05-combo-text-screen-boundary-clamping-design.md](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-10-05-combo-text-screen-boundary-clamping-design.md)

---

## 1. Overview & Objectives
Đảm bảo khi xuất hiện hiệu ứng `ComboTextEffect`, khung chữ `"COMBO xN"` không bao giờ bị cắt cụt (clipping) ngoài màn hình hoặc bay tràn vào khu vực Top Header, kể cả khi khối `BlockShape` được đặt sát các cạnh bàn cờ (cột 0, cột 7, hàng 0).

---

## 2. Affected Files & Boundaries

| File | Module / Namespace | Type of Change | Purpose |
|---|---|---|---|
| `Assets/_Game/_UI/Scripts/ComboTextEffect.cs` | `Hung.UI` / `UI` | Modify | Thêm các thuộc tính padding biên và thuật toán clamping toạ độ UI. |
| `Assets/Tests/EditMode/ComboEffectTests.cs` | `Tests.EditMode` | Modify / Extend | Thêm test kiểm tra toạ độ khi bị clamp ở mép trái, mép phải và mép trên. |

---

## 3. Step-by-Step Implementation Steps

### Step 1: Thêm Boundary Settings và Logic Clamp trong `ComboTextEffect.cs`
- Thêm các trường có thể tinh chỉnh:
  - `horizontalPadding` (mặc định: `30f`)
  - `topPadding` (mặc định: `120f` - tính cả khoảng cách tới header điểm số)
  - `bottomPadding` (mặc định: `40f`)
- Tạo hàm công khai `Vector2 ClampToParentBounds(RectTransform parentRect, Vector2 localPoint)`:
  - Tính nửa chiều rộng `halfWidth = (rectTransform.rect.width > 0 ? rectTransform.rect.width : 400f) * 0.5f`.
  - Tính nửa chiều cao `halfHeight = (rectTransform.rect.height > 0 ? rectTransform.rect.height : 100f) * 0.5f`.
  - Tính giới hạn:
    - `minX = parentRect.rect.xMin + halfWidth + horizontalPadding;`
    - `maxX = parentRect.rect.xMax - halfWidth - horizontalPadding;`
    - `minY = parentRect.rect.yMin + halfHeight + bottomPadding;`
    - `maxY = parentRect.rect.yMax - halfHeight - floatUpDistance - topPadding;`
  - Nếu `minX <= maxX`, `localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX)`.
  - Nếu `minY <= maxY`, `localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY)`.
- Trong hàm `Play()`:
  - Gọi `localPoint = ClampToParentBounds(parentRect, localPoint);` trước khi gán vào `anchoredPosition`.

### Step 2: Cập nhật Unit Test trong `ComboEffectTests.cs`
- Thêm test `ComboTextEffect_ClampToParentBounds_RestrictsPositionWithinMargins`:
  - Thiết lập `parentRect` kích thước `1080 x 1920`.
  - Truyền toạ độ ở cực trái `x = -1000` $\rightarrow$ Kết quả được clamp về `minX`.
  - Truyền toạ độ ở cực phải `x = 1000` $\rightarrow$ Kết quả được clamp về `maxX`.
  - Truyền toạ độ ở cực trên `y = 1000` $\rightarrow$ Kết quả được clamp về `maxY`.

### Step 3: Verification
- Kiểm tra toàn bộ code compile sạch sẽ không warning/error.
- Chạy thử nghiệm trong Unity.
