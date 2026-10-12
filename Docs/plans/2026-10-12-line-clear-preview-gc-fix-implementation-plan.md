# Implementation Plan: Loại Bỏ Rác GC Khi Kéo Khối

> **Ngày tạo:** 2026-10-12
> **Design:** `Docs/designs/2026-10-12-line-clear-preview-gc-fix-design.md`

---

## Bước 1 — `BlockBoard` dùng lại buffer
**File:** `Assets/_Game/_BlockDrag/Board/BlockBoard.cs`
- Field mới (cấp trong constructor):
  - `private readonly bool[,] footprintScratch;` (Rows × Columns)
  - `private readonly bool[] rowScratch;` (Rows), `private readonly bool[] colScratch;` (Columns)
  - `private readonly List<Vector2Int> footprintCells = new List<Vector2Int>();`
- Viết lại `FindLinesCompletedBy`:
  1. `!CanPlace` → `None`.
  2. Duyệt mask shape: đánh dấu `footprintScratch[r,c]`, `rowScratch[r]`, `colScratch[c]`, thêm vào `footprintCells`.
  3. Duyệt `r = 0..Rows-1` có `rowScratch[r]`: kiểm tra đầy bằng `occupied || footprintScratch`; đầy → thêm vào `fullRows` (lazy `new List<int>` khi gặp hàng đầy đầu tiên). Tương tự cột.
  4. Trong `finally`: duyệt `footprintCells` xoá `footprintScratch`, `rowScratch`, `colScratch` tại đúng các chỉ số đó; `footprintCells.Clear()`.
  5. Không có hàng/cột đầy → `LineClearResult.None`; có → dựng `HashSet` cells như cũ.
- Thứ tự rows/cols vẫn tăng dần (duyệt theo chỉ số).

## Bước 2 — Test buffer không rò trạng thái
**File:** `Assets/Tests/EditMode/BlockBoardTests.cs`
- `FindLinesCompletedBy_CalledRepeatedlyAtDifferentPositions_DoesNotLeakPreviousFootprint`: hàng 0 thiếu (0,3); gọi với `Single` tại (0,3) → có hàng 0; sau đó gọi tại (1,0) → `None`; rồi lại tại (0,3) → có hàng 0.
- `FindLinesCompletedBy_WhenCannotPlace_DoesNotLeakIntoNextCall`: gọi với vị trí không hợp lệ, rồi gọi vị trí hợp lệ không đầy → `None`.
- Các test cũ của `FindLinesCompletedBy` giữ nguyên (đảm bảo hành vi không đổi).

## Bước 3 — `BlockGrid` cache vị trí đặt
**File:** `Assets/_Game/_BlockDrag/BlockGrid.cs`
- Field: `protected BlockShape previewShape; protected Vector2Int previewBaseCoord; protected bool hasPreviewCache;`
- `UpdateDragIndicator`, nhánh `CanPlaceShape == true`:
  ```csharp
  if (hasPreviewCache && previewShape == shape && previewBaseCoord == baseCoord) return;
  SetClearPreview(Board.FindLinesCompletedBy(shape, baseCoord), GetShapeColor(shape));
  ShowIndicator(shape, baseCoord);
  previewShape = shape; previewBaseCoord = baseCoord; hasPreviewCache = true;
  ```
- `HideIndicatorCells()`: đầu hàm `hasPreviewCache = false; previewShape = null;` (trước early-return `indicatorPool == null`).
- `ShowIndicator` (public) gọi trực tiếp từ ngoài: cũng đặt `hasPreviewCache = false` ở đầu để lần `UpdateDragIndicator` kế tiếp luôn vẽ lại đúng.
- XML doc của `UpdateDragIndicator`: ghi chú "bỏ qua khi shape và ô không đổi".

## Bước 4 — `LineClearPreviewEffect.Update` thoát sớm
**File:** `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs`
- Field `private bool glowsVisible;` — true khi `LayoutGlows` bật viền, false khi `ApplyGlowAlpha` tắt viền.
- Đầu `Update`: `if (!isPreviewing && fade <= 0f && !glowsVisible) return;`

## Bước 5 — Kiểm tra
1. Refresh Unity, Console không lỗi/cảnh báo mới.
2. EditMode tests: `BlockBoardTests`, `LineClearPreviewEffectTests` pass (6 test `GameOverCheckTests` fail sẵn do prefab null — không liên quan).
3. Play Mode qua script (như lần trước):
   - Gọi `UpdateDragIndicator` 2 lần cùng ô → event `OnClearPreviewChanged` chỉ phát 1 lần, preview vẫn bật.
   - Đo `GC.GetAllocatedBytesForCurrentThread()` quanh 100 lần gọi `UpdateDragIndicator` cùng ô → kỳ vọng 0 B.
   - Rời ô / reset → preview tắt, màu ô trở về như cũ.
4. (Bạn xác nhận) Profiler → CPU → cột GC Alloc khi giữ khối đứng yên: 0 B từ `BlockGrid`.

## Files tác động

| File | Loại |
|---|---|
| `Assets/_Game/_BlockDrag/Board/BlockBoard.cs` | Sửa |
| `Assets/_Game/_BlockDrag/BlockGrid.cs` | Sửa |
| `Assets/_Game/_BlockDrag/Effects/LineClearPreviewEffect.cs` | Sửa |
| `Assets/Tests/EditMode/BlockBoardTests.cs` | Sửa |

Không đổi scene, prefab, `.asmdef` hay ranh giới kiến trúc.
