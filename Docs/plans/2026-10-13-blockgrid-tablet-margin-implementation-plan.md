# Kế Hoạch Triển Khai: Margin Grid Theo Loại Màn Hình (Tablet / iPad)

> **Ngày tạo:** 2026-10-13  
> **Thiết kế:** [`2026-10-13-blockgrid-tablet-margin-design.md`](../designs/2026-10-13-blockgrid-tablet-margin-design.md)  
> **Assembly bị ảnh hưởng:** `BlockDrag`, `EditModeTests`. Không đổi `.asmdef` nào.

---

## 1. File thay đổi

| # | File | Loại | Nội dung |
| :--- | :--- | :--- | :--- |
| 1 | [`Assets/_Game/_BlockDrag/BlockGrid.cs`](../../Assets/_Game/_BlockDrag/BlockGrid.cs) | Sửa | 2 trường Inspector mới, `IsTabletAspect` (static), `GetEffectiveMarginPercent`, dùng trong `CalculateGridParameters`, clamp trong `OnValidate` |
| 2 | `Assets/Tests/EditMode/BlockGridTabletMarginTests.cs` (+ `.meta`) | Mới | Test nhận diện tablet theo tỉ lệ |
| 3 | `Assets/_Game/_Scenes/Game/GameScene.unity` | Serialized data | Unity tự ghi thêm 2 trường mặc định khi lưu scene; không cần sửa tay |

## 2. Các bước

### Bước 1: Trường Inspector mới (`BlockGrid.cs`, ngay sau `marginPercent`)
```csharp
[Tooltip("Lề trái/phải khi chạy trên tablet / iPad")]
[Range(0f, 0.4f)]
[SerializeField] protected float tabletMarginPercent = 0.2f;
[Tooltip("Tỷ lệ cạnh ngắn/cạnh dài màn hình từ ngưỡng này trở lên được coi là tablet (điện thoại ≤ 0.56, iPad ≈ 0.75)")]
[Range(0.5f, 1f)]
[SerializeField] protected float tabletAspectThreshold = 0.6f;
```

### Bước 2: Hàm nhận diện và margin hiệu lực
```csharp
/// <summary>
/// True nếu tỷ lệ cạnh ngắn/cạnh dài của màn hình đạt ngưỡng tablet (không phụ thuộc hướng xoay)
/// </summary>
public static bool IsTabletAspect(float width, float height, float threshold)
{
    float longSide = Mathf.Max(width, height);
    if (longSide <= 0f)
    {
        return false;
    }
    return Mathf.Min(width, height) / longSide >= threshold;
}

/// <summary>
/// Margin thực tế: tablet dùng tabletMarginPercent, thiết bị khác dùng marginPercent
/// </summary>
protected float GetEffectiveMarginPercent()
{
    return IsTabletAspect(Screen.width, Screen.height, tabletAspectThreshold) ? tabletMarginPercent : marginPercent;
}
```
`IsTabletAspect` là `public static` để test được mà không cần `Screen`.

### Bước 3: `CalculateGridParameters`
```csharp
float usableWidth = screenWorldWidth * Mathf.Clamp01(1f - 2f * GetEffectiveMarginPercent());
```

### Bước 4: `OnValidate`
```csharp
tabletMarginPercent = Mathf.Clamp(tabletMarginPercent, 0f, 0.4f);
tabletAspectThreshold = Mathf.Clamp(tabletAspectThreshold, 0.5f, 1f);
```

### Bước 5: Test `BlockGridTabletMarginTests`
| Test | Input (w, h, 0.6) | Kỳ vọng |
| :--- | :--- | :--- |
| `IsTabletAspect_IPhone_ReturnsFalse` | 1170, 2532 | false |
| `IsTabletAspect_Phone16x9_ReturnsFalse` | 1080, 1920 | false |
| `IsTabletAspect_IPadPortrait_ReturnsTrue` | 1536, 2048 | true |
| `IsTabletAspect_IPadLandscape_ReturnsTrue` | 2048, 1536 | true |
| `IsTabletAspect_AndroidTablet16x10_ReturnsTrue` | 1600, 2560 | true |
| `IsTabletAspect_ZeroSize_ReturnsFalse` | 0, 0 | false |

## 3. Kiểm tra
1. Unity compile không lỗi, console sạch (`read_console`).
2. Chạy EditMode tests: `BlockGridTabletMarginTests` và toàn bộ test cũ đều pass.
3. Thủ công: Game view chọn iPhone 12 Pro (1170x2532), Play → grid dùng `marginPercent` của Inspector. Chọn iPad (1536x2048), Play → grid hẹp lại theo margin 0.2, không đè khu spawn khối.
4. Trên `BlockGrid` trong `GameScene`: kiểm tra 2 trường mới hiện đúng giá trị mặc định 0.2 / 0.6.
