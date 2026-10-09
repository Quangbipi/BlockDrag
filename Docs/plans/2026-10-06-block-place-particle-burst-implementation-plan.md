# Implementation Plan: Hiệu Ứng Particle Tỏa Khi Đặt BlockShape

> **Ngày tạo:** 2026-10-06
> **Design:** `Docs/designs/2026-10-06-block-place-particle-burst-design.md`

---

## Bước 1 — `BlockGrid` phát sự kiện ô vừa đặt
**File:** `Assets/_Game/_BlockDrag/BlockGrid.cs`
- Thêm `public event System.Action<IReadOnlyList<Vector2Int>> OnShapePlacedCells;`
- Trong `TryPlaceShape`, sau khi tính `LastPlacedCenterPosition` và **trước** `CheckAndClearLines()`: `OnShapePlacedCells?.Invoke(placedCells);`
- Không đổi hành vi hiện có, các event cũ giữ nguyên.

## Bước 2 — Tạo `BlockPlaceEffect`
**File mới:** `Assets/_Game/_BlockDrag/Effects/BlockPlaceEffect.cs` (+ `.meta` do Unity sinh), namespace `Gameplay.BlockDrag`, assembly `BlockDrag`.
- Serialized fields theo bảng tham số trong design (mục 2.4) + `blockGrid` (tự `FindObjectOfType` nếu trống) + `materialOverride` (tuỳ chọn).
- `Awake`: tự tìm grid, `BuildParticleSystem()` tạo GameObject root `BlockPlaceVFX` với các module như design.
- `OnEnable`/`OnDisable`: subscribe/unsubscribe `blockGrid.OnShapePlacedCells`.
- `OnDestroy`: kill delayed tween, destroy `BlockPlaceVFX` và material tự tạo.
- `HandleShapePlaced(cells)`: copy danh sách ô, nếu `playDelay > 0` dùng `DOVirtual.DelayedCall`, ngược lại gọi `Play(cells)` ngay.
- `public void Play(IReadOnlyList<Vector2Int> cells)`: tính cạnh biên → emit hạt.
- `public static List<PerimeterEdge> CollectPerimeterEdges(IReadOnlyList<Vector2Int> cells)` + `public static int CalculateParticleCount(int edgeCount, float perEdge, int min, int max)` — thuần logic để test.

## Bước 3 — Unit test (Edit Mode)
**File mới:** `Assets/Tests/EditMode/BlockPlaceEffectTests.cs`
- `CollectPerimeterEdges_SingleCell_ReturnsFourEdges`
- `CollectPerimeterEdges_SShape_ReturnsTenEdges`
- `CollectPerimeterEdges_Square2x2_ExcludesInnerEdges` (8 cạnh)
- `CollectPerimeterEdges_EdgeNormalUp_PointsToPreviousRow` (hướng lên = `Vector2Int(-1,0)` trong grid, `Vector2.up` world)
- `CalculateParticleCount_ClampsToMinAndMax`
- `CollectPerimeterEdges_EmptyOrNull_ReturnsEmpty`

## Bước 4 — Gắn vào scene (thủ công trong Editor)
- Mở `Assets/_Game/_Scenes/Game/GameScene.unity`, tạo GameObject `BlockPlaceEffect` (hoặc thêm vào object gameplay manager sẵn có) → Add Component **Block Place Effect**.
- Kéo `BlockGrid` vào field `Block Grid` (hoặc để trống để tự tìm).
- Không sửa YAML scene bằng tay vì scene đang có thay đổi chưa commit.

## Bước 5 — Kiểm tra
1. Unity compile không lỗi/cảnh báo mới.
2. Chạy Edit Mode tests (Test Runner hoặc lệnh batchmode trong `AGENTS.md`) → toàn bộ pass.
3. Play GameScene:
   - Thả khối hợp lệ → sau ~0.2s hạt vuông nhiều màu bung quanh mép khối, dừng gần như ngay, nhỏ dần rồi mất trong ~0.45s.
   - Thả khối không hợp lệ → không có hạt.
   - Thả khối gây phá hàng → hạt vẫn phát, không lỗi khi block bị destroy.
   - Hạt nằm dưới block đã đặt, trên ô nền.
   - Thả liên tục nhiều khối nhanh → không giật, không tạo thêm GameObject (kiểm tra Hierarchy chỉ có 1 `BlockPlaceVFX`).
   - Restart game → hiệu ứng vẫn hoạt động.
4. So sánh trực quan với video, tinh chỉnh tham số Inspector nếu cần.

## Files ảnh hưởng

| File | Loại |
|---|---|
| `Assets/_Game/_BlockDrag/BlockGrid.cs` | Sửa |
| `Assets/_Game/_BlockDrag/Effects/BlockPlaceEffect.cs` | Mới |
| `Assets/Tests/EditMode/BlockPlaceEffectTests.cs` | Mới |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | Sửa thủ công trong Editor (gắn component) |

Không thay đổi `.asmdef`, package hay ranh giới kiến trúc.
