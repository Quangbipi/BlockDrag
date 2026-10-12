# Implementation Plan: Viền Sáng Báo Trước Hàng Sẽ Bị Phá

> **Ngày tạo:** 2026-10-12
> **Design:** `Docs/designs/2026-10-12-line-clear-preview-glow-design.md`

---

## Bước 1 — `BlockBoard` dự đoán hàng/cột sẽ đầy
**File:** `Assets/_Game/_BlockDrag/Board/BlockBoard.cs`
- Thêm `public LineClearResult FindLinesCompletedBy(IShapeFootprint shape, Vector2Int baseCoord)`:
  - `!CanPlace(...)` → `LineClearResult.None`.
  - Gom tập ô footprint (toạ độ tuyệt đối) + tập hàng/cột shape chạm vào.
  - Hàng `r` đầy khi mọi `c`: `occupied[r,c] || footprint.Contains((r,c))`; tương tự cho cột.
  - Gom `cells` bằng `HashSet<Vector2Int>` như `FindFullLines`. Không ghi vào `occupied`.
- Hàng/cột trả về theo thứ tự tăng dần (khớp `FindFullLines`).

## Bước 2 — Test `BlockBoard`
**File:** `Assets/Tests/EditMode/BlockBoardTests.cs` (thêm vào fixture sẵn có, bàn 4×4)
- `FindLinesCompletedBy_WhenShapeFillsLastGapInRow_ReturnsThatRow`
- `FindLinesCompletedBy_WhenShapeCompletesRowAndColumn_ReturnsBothWithoutDuplicateCells`
- `FindLinesCompletedBy_WhenNoLineCompleted_ReturnsNone`
- `FindLinesCompletedBy_WhenCannotPlace_ReturnsNone`
- `FindLinesCompletedBy_DoesNotMutateBoard`
- `FindLinesCompletedBy_MatchesFindFullLinesAfterActualPlacement`

## Bước 3 — `BlockGrid` tính preview & phát event
**File:** `Assets/_Game/_BlockDrag/BlockGrid.cs`
- Field mới dưới header `Indicator Settings`: `[Range(0,1)] clearPreviewGhostAlpha = 1f`.
- Event: `public event System.Action<LineClearResult, Color> OnClearPreviewChanged;`
- State: `LineClearResult clearPreview = LineClearResult.None; Color clearPreviewColor;` + property `ClearPreview`.
- `public GameObject GetOccupiedBlock(int row, int col)` (bounds-safe, trả null nếu ngoài/không có).
- `UpdateDragIndicator`: nhánh hợp lệ → `SetClearPreview(Board.FindLinesCompletedBy(shape, baseCoord), shapeColor)` **trước** `ShowIndicator`; nhánh không hợp lệ → `HideIndicator()` (đã tắt preview).
- `ShowIndicator`: ghost cell nằm trong `clearPreview.Cells` dùng alpha `clearPreviewGhostAlpha`.
- `HideIndicator()`: thêm `SetClearPreview(LineClearResult.None, default)` → tự động áp dụng cho `TryPlaceShape` và `ResetBoard`.
- `private void SetClearPreview(LineClearResult result, Color color)`: so sánh Rows/Columns (SequenceEqual) và màu với state hiện tại; giống nhau → return; khác → cập nhật và `Invoke`. Hai kết quả cùng `!HasLines` coi là bằng nhau (bỏ qua màu).

## Bước 4 — Tạo `LineClearPreviewEffect`
**File mới:** `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs` (+ `.meta` do Unity sinh), `[AddComponentMenu("Gameplay/Line Clear Preview Effect")]`.
- Serialized fields theo bảng mục 2.8 của design + `blockGrid` (tự `FindObjectOfType`) + `materialOverride` cho hạt.
- Vòng đời giống `BlockPlaceEffect`: `Awake` (tìm grid, tạo root `LineClearPreviewVFX`, sinh glow sprite nếu trống, dựng ParticleSystem), `OnEnable/OnDisable` subscribe/unsubscribe + `Hide()`, `OnDestroy` destroy root, texture/sprite/material tự tạo, `DOTween.Kill(this)`.
- `HandlePreviewChanged(result, color)` → `result.HasLines ? Show(result, color) : Hide()`.
- `Show`: `RestoreTint()` → tint ô mới → bố trí viền (pool `SpriteRenderer`) cho từng row/col → bật pulse tween → `isPreviewing = true`.
- `Hide`: `RestoreTint()`, fade-out nhanh + tắt viền, `isPreviewing = false` (hạt còn lại tự tàn).
- `Update`: khi `isPreviewing`, tích luỹ `sparklesPerSecondPerLine × lines × dt` và `Emit` từng hạt dọc 2 mép dài của các viền.
- Hàm tĩnh thuần logic để test:
  - `public static Rect GetLineLocalRect(bool isRow, int index, int rows, int cols, float cellSize, float spacing, float padding)` — khung viền trong toạ độ cục bộ grid.
  - `public static Texture2D CreateGlowTexture(int size, ...)` (texture sinh lúc chạy; test kiểm tra tâm trong suốt, viền có alpha).
- Khôi phục màu: chỉ khi `blockGrid.GetOccupiedBlock(cell) == tinted.Renderer.gameObject`.

## Bước 5 — Test effect
**File mới:** `Assets/Tests/EditMode/LineClearPreviewEffectTests.cs`
- `GetLineLocalRect_Row_SpansFullGridWidthPlusPadding`
- `GetLineLocalRect_Column_CenteredOnColumn`
- `GetLineLocalRect_TopRow_HasHighestY` (hàng 0 ở trên)
- `CreateGlowTexture_CenterTransparent_EdgeOpaque`

## Bước 6 — Gắn vào scene
- Dùng Unity MCP (hoặc thủ công): trong `GameScene.unity` thêm component **Line Clear Preview Effect** vào cùng GameObject đang chứa `BlockPlaceEffect`, gán `BlockGrid`. Lưu scene.
- Scene đang có thay đổi chưa commit của bạn → chỉ thêm component, không đụng gì khác; báo lại trong phần tổng kết.

## Bước 7 — Kiểm tra
1. Refresh Unity, Console không lỗi/cảnh báo mới.
2. Chạy toàn bộ EditMode tests → pass.
3. Play `GameScene` (thủ công / bạn xác nhận):
   - Kéo khối lấp đầy hàng → hàng đổi màu khối, viền hồng nhấp nháy, hạt lấp lánh.
   - Lấp đầy cả hàng + cột → 2 viền.
   - Kéo đi chỗ khác / ra ngoài grid → màu cũ trở lại, viền tắt.
   - Thả → hàng phá như cũ, ô còn lại đúng màu gốc; Replay khi đang kéo không để lại viền.

## Files tác động

| File | Loại |
|---|---|
| `Assets/_Game/_BlockDrag/Board/BlockBoard.cs` | Sửa |
| `Assets/_Game/_BlockDrag/BlockGrid.cs` | Sửa |
| `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs` (+ .meta) | Mới |
| `Assets/Tests/EditMode/BlockBoardTests.cs` | Sửa |
| `Assets/Tests/EditMode/LineClearPreviewEffectTests.cs` (+ .meta) | Mới |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | Sửa (thêm component) |

Không thay đổi `.asmdef`, package hay ranh giới kiến trúc.
