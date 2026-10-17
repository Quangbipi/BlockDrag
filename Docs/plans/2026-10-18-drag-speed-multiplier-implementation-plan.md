# Tăng tốc độ kéo khối — Implementation Plan

Design: [2026-10-18-drag-speed-multiplier-design.md](../designs/2026-10-18-drag-speed-multiplier-design.md)

## Files bị ảnh hưởng

| File | Assembly | Thay đổi |
|------|----------|----------|
| `Assets/_Game/_BlockDrag/BlockDragHandler.cs` | `BlockDrag` | Thêm field, anchor, hàm static, sửa `BeginDrag` + `UpdateDragPosition` |
| `Assets/Tests/EditMode/BlockDragSpeedTests.cs` (+ `.meta` do Unity sinh) | `EditModeTests` | Test mới cho `ComputeDragPosition` |

Không sửa `BlockDragInput`, `BlockGrid`, `.asmdef`, scene. Prefab `BlockShape.prefab` không cần sửa (field mới dùng giá trị mặc định; chỉ chỉnh khi tinh chỉnh trên device).

## Các bước

### Bước 1 — Field mới (`BlockDragHandler.cs`)
Thêm sau nhóm `Drag Lift & Scaling`:

```csharp
[Header("Drag Speed")]
[Tooltip("Hệ số nhân quãng đường khối đi so với ngón tay (1 = bám 1:1)")]
[Range(1f, 2.5f)]
[SerializeField] private float dragSpeedMultiplier = 1.3f;

[Tooltip("Giữ tâm khối trong vùng camera nhìn thấy khi kéo nhanh hơn ngón tay")]
[SerializeField] private bool clampToScreen = true;
```

Thêm field private `private Vector2 dragAnchorWorld;`.

### Bước 2 — Tách `GetFingerWorld()`
Hàm private trả về `Vector3` world của `lastScreenPos` (dùng `planeDistance` như hiện tại), dùng chung cho `BeginDrag` và `UpdateDragPosition`.

### Bước 3 — Neo anchor trong `BeginDrag`
Ngay sau `lastScreenPos = screenPos;`, nếu camera khác null: `dragAnchorWorld = GetFingerWorld();`.

### Bước 4 — Hàm static thuần
```csharp
public static Vector2 ComputeDragPosition(Vector2 anchor, Vector2 finger, float multiplier)
{
    return anchor + (finger - anchor) * multiplier;
}
```

### Bước 5 — Sửa `UpdateDragPosition`
```csharp
Vector2 xy = ComputeDragPosition(dragAnchorWorld, GetFingerWorld(), dragSpeedMultiplier);
if (clampToScreen) xy = ClampToCamera(cam, xy);
transform.position = new Vector3(xy.x, xy.y + currentLift, originalPosition.z);
```
`ClampToCamera`: tính min/max world từ `ViewportToWorldPoint((0,0))` và `((1,1))` ở cùng `planeDistance`, kẹp `xy` (trước khi cộng lift, rồi kẹp thêm `y + currentLift ≤ maxY` để khối không vượt mép trên). Cập nhật comment XML của hàm (không còn "bám 1:1").

### Bước 6 — Test EditMode `BlockDragSpeedTests`
- `ComputeDragPosition_MultiplierOne_ReturnsFinger`
- `ComputeDragPosition_FingerAtAnchor_ReturnsAnchor`
- `ComputeDragPosition_Multiplier1_5_ScalesDeltaFromAnchor` (anchor (1,1), finger (3,2) → (4,2.5))

## Kiểm tra

1. Unity compile sạch (`read_console` không lỗi).
2. Chạy EditMode tests — toàn bộ pass, gồm 3 test mới.
3. Editor Play `GameScene`:
   - Nhấc khối: khối vẫn hiện ngay trên ngón tay, nâng mượt như cũ.
   - Kéo: khối đi xa hơn chuột/ngón tay (~1.3×); indicator trên grid khớp vị trí khối; thả đặt đúng ô.
   - Kéo sát mép màn hình: khối không chạy ra ngoài.
   - Đặt `dragSpeedMultiplier = 1` → hành vi giống hệt bản cũ.
4. Bạn test trên device và tinh chỉnh `dragSpeedMultiplier` trên prefab `BlockShape` nếu cần.
