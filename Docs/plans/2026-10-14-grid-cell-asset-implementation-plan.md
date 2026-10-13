# Implementation Plan: Asset Ô Lưới & Khung Board

> **Ngày tạo:** 2026-10-14  
> **Design:** [`Docs/designs/2026-10-14-grid-cell-asset-design.md`](../designs/2026-10-14-grid-cell-asset-design.md)  
> **Assembly bị ảnh hưởng:** `BlockDrag`, `EditModeTests` (không đổi `.asmdef`, không đổi dependency)

---

## Bước 1 — Tạo sprite PNG
Sinh bằng một script Swift/AppKit một lần trong scratchpad (không commit script), ghi PNG vào `Assets/_Game/_BlockDrag/Resource/`:

| File | Kích thước | Nội dung |
|---|---|---|
| `GridCell_Empty.png` | 128×128 | Fill `#5B725A`; viền trong 2px `#435F48` cả 4 cạnh |
| `BoardFrame.png` | 64×64 | Rounded-rect bán kính 16px, fill `#87B586`, viền ngoài 2px `#A6C9A8`, góc ngoài trong suốt (anti-alias) |

## Bước 2 — Import settings (qua Unity MCP `manage_asset`/`execute_code` rồi refresh)
- Cả hai: Texture Type = Sprite (2D and UI), Sprite Mode = Single, Filter = Bilinear, Mip Maps = off, Compression = None (ảnh nhỏ, tránh nhòe màu vạch lưới).
- `GridCell_Empty`: Pixels Per Unit = **128** (bounds = 1 unit), Mesh Type = Full Rect.
- `BoardFrame`: PPU = **64**, Mesh Type = **Full Rect**, Sprite Border = **(20, 20, 20, 20)**.
- Giữ nguyên các file `.meta` do Unity sinh.

## Bước 3 — Prefab
Tạo trong `Assets/_Game/_BlockDrag/Prefabs/`:
- `GridCellPrefab.prefab`: GameObject + `SpriteRenderer` (sprite `GridCell_Empty`, color trắng, sortingOrder 0).
- `BoardFramePrefab.prefab`: GameObject + `SpriteRenderer` (sprite `BoardFrame`, Draw Mode = Sliced, size (1,1), sortingOrder −1).

## Bước 4 — Sửa `Assets/_Game/_BlockDrag/BlockGrid.cs`
1. Thêm field trong header *Prefabs & References*:
   ```csharp
   [Tooltip("Prefab ghost khi kéo khối. Để trống => dùng cellPrefab")]
   [SerializeField] protected GameObject indicatorCellPrefab;
   [Tooltip("Prefab khung board (SpriteRenderer Draw Mode = Sliced). Để trống => không vẽ khung")]
   [SerializeField] protected GameObject boardFramePrefab;
   [Tooltip("Độ dày viền khung mỗi cạnh, tính theo tỉ lệ cellSize")]
   [Range(0f, 1f)]
   [SerializeField] protected float framePaddingRatio = 0.12f;
   ```
2. Thêm static helper thuần (để test):
   ```csharp
   public static Vector2 ComputeFrameSize(float gridWidth, float gridHeight, float cellSize, float paddingRatio)
   ```
   trả về `(gridWidth, gridHeight) + 2 * cellSize * max(0, paddingRatio)`.
3. Thêm `CreateBoardFrame()` gọi ở cuối `GenerateGrid()`: nếu `boardFramePrefab != null` → `Instantiate` làm con của grid, tên `BoardFrame`, `localPosition = 0`, `localScale = 1`; nếu có `SpriteRenderer` thì `drawMode = Sliced`, `size = ComputeFrameSize(...)`. Lưu vào field private `boardFrame`.
   - `ClearGrid()` đã xoá mọi child còn sót → khung tự bị xoá; chỉ cần gán `boardFrame = null`.
4. `CreateIndicatorCell()`: dùng `GameObject prefab = indicatorCellPrefab != null ? indicatorCellPrefab : cellPrefab;` thay cho `cellPrefab`.

Không đổi `gridCells`, `BlockBoard`, logic đặt/phá hàng.

## Bước 5 — Test EditMode
Tạo `Assets/Tests/EditMode/BlockGridBoardFrameTests.cs` (+ `.meta`), fixture `BlockGridBoardFrameTests`:
- `ComputeFrameSize_AddsPaddingOnEachSide` — grid 8×8, cellSize 1, ratio 0.12 → (8.24, 8.24).
- `ComputeFrameSize_NegativeRatio_ReturnsGridSize`.
- Việc ghost dùng `indicatorCellPrefab` (method private, cần scene) được kiểm tra thủ công ở Bước 7.

## Bước 6 — Wiring `GameScene`
Trên GameObject `BlockGrid` trong `Assets/_Game/_Scenes/Game/GameScene.unity`:
- `cellPrefab` → `GridCellPrefab`
- `indicatorCellPrefab` → `BlockPrefab`
- `boardFramePrefab` → `BoardFramePrefab`
- Giữ `spacing = 0`, `framePaddingRatio = 0.12`. Lưu scene.

## Bước 7 — Kiểm tra
1. `read_console`: không lỗi compile.
2. Chạy toàn bộ EditMode test → pass.
3. Play `GameScene` (Game view 1080×1920), chụp screenshot so với frame video:
   - Lưới 8×8 ô phẳng `#5B725A`, vạch tối giữa các ô và mép.
   - Khung bo góc bao quanh, phía sau ô.
   - Kéo khối: ghost vẫn bevel, preview phá hàng vẫn đúng; đặt khối & phá hàng bình thường; Replay giữ nguyên lưới + khung.
4. Đổi Game view sang iPad (tỉ lệ 3:4): khung vẫn khớp grid.
5. Nếu vạch/khung lệch so với video: chỉnh độ dày viền sprite hoặc `framePaddingRatio` (không đổi thiết kế).

## Files tác động
| Loại | File |
|---|---|
| Mới | `Assets/_Game/_BlockDrag/Resource/GridCell_Empty.png` (+meta) |
| Mới | `Assets/_Game/_BlockDrag/Resource/BoardFrame.png` (+meta) |
| Mới | `Assets/_Game/_BlockDrag/Prefabs/GridCellPrefab.prefab` (+meta) |
| Mới | `Assets/_Game/_BlockDrag/Prefabs/BoardFramePrefab.prefab` (+meta) |
| Mới | `Assets/Tests/EditMode/BlockGridBoardFrameTests.cs` (+meta) |
| Sửa | `Assets/_Game/_BlockDrag/BlockGrid.cs` |
| Sửa (serialized) | `Assets/_Game/_Scenes/Game/GameScene.unity` — 3 field mới trên `BlockGrid` |
