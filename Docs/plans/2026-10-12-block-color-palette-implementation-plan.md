# Kế Hoạch Triển Khai: Bộ Màu Random Cho Block Khi Spawn

> **Ngày tạo:** 2026-10-12  
> **Thiết kế:** [`2026-10-12-block-color-palette-design.md`](../designs/2026-10-12-block-color-palette-design.md)  
> **Assembly bị ảnh hưởng:** `Quang.Data` (qua asmref `Assets/_Game/_BlockDrag/Data`), `BlockDrag`, `EditModeTests`. Không đổi `.asmdef` / `.asmref` nào.

---

## 1. File thay đổi

| # | File | Loại | Nội dung |
| :--- | :--- | :--- | :--- |
| 1 | `Assets/_Game/_BlockDrag/Data/BlockColorPaletteSO.cs` (+ `.meta`) | Mới | ScriptableObject palette |
| 2 | `Assets/_Game/_BlockDrag/Data/DataManager.BlockDrag.cs` (+ `.meta`) | Mới | Partial `DataManager`: field `gameplayData`, `blockColorPalette`, hook `TryGetGameSOData<T>` |
| 3 | [`Assets/_Game/_BlockDrag/BlockShape.cs`](../../Assets/_Game/_BlockDrag/BlockShape.cs) | Sửa | `ShapeColor`, tham số `color` cho `Initialize`, `SetDimmed` dùng `ShapeColor` |
| 4 | [`Assets/_Game/_BlockDrag/BlockSpawner.cs`](../../Assets/_Game/_BlockDrag/BlockSpawner.cs) | Sửa | Lấy palette, chọn màu không trùng trong lượt |
| 5 | [`Assets/_Game/_BlockDrag/BlockGrid.cs`](../../Assets/_Game/_BlockDrag/BlockGrid.cs) | Sửa | `GetShapeColor` đọc `shape.ShapeColor` |
| 6 | `Assets/_Game/_Data/DataSO/BlockColorPalette.asset` (+ `.meta`) | Mới | 6 màu mẫu |
| 7 | [`Assets/_Game/_Data/DataManager.prefab`](../../Assets/_Game/_Data/DataManager.prefab) | Sửa (serialized data) | Gán `blockColorPalette`; `gameplayData` đã có sẵn trong YAML |
| 8 | `Assets/Tests/EditMode/BlockColorPaletteTests.cs` (+ `.meta`) | Mới | Test logic chọn màu |
| 9 | [`.agents/rules/architecture.md`](../../.agents/rules/architecture.md) | Sửa | Mục asmref `_BlockDrag/Data` liệt kê thêm palette SO và partial `DataManager` |

## 2. Các bước

### Bước 1: `BlockColorPaletteSO`
```csharp
namespace Gameplay.BlockDrag
{
    [CreateAssetMenu(fileName = "BlockColorPalette", menuName = "Game/Block Color Palette")]
    public class BlockColorPaletteSO : ScriptableObject
    {
        [Tooltip("Bộ màu dùng để random cho khối khi spawn")]
        [SerializeField] private List<Color> colors = new List<Color>();

        public IReadOnlyList<Color> Colors => colors;

        // Ưu tiên màu chưa có trong exclude; dùng hết thì random toàn list. Alpha luôn = 1.
        public bool TryGetRandomColor(IReadOnlyList<Color> exclude, out Color color) { ... }
    }
}
```
- Dùng một `List<Color>` tạm static để gom ứng viên, tránh cấp phát mỗi lần spawn.
- Không mở API ghi: test tạo SO bằng `ScriptableObject.CreateInstance` và gán `colors` qua `SerializedObject` (Editor).

### Bước 2: Partial `DataManager`
```csharp
namespace Quang.Data
{
    using Gameplay.BlockDrag;

    public partial class DataManager
    {
        [SerializeField] private GameplayData gameplayData;          // tên field khớp YAML cũ trong prefab
        [SerializeField] private BlockColorPaletteSO blockColorPalette;

        partial void TryGetGameSOData<T>(ref T result) where T : ScriptableObject
        {
            if (typeof(T) == typeof(GameplayData)) result = gameplayData as T;
            else if (typeof(T) == typeof(BlockColorPaletteSO)) result = blockColorPalette as T;
        }
    }
}
```
- Kiểm tra trước khi viết: `GameplayData` thuộc namespace nào (`Quang.Data`?) và không có partial nào khác đã khai báo field tên `gameplayData`.

### Bước 3: `BlockShape`
- Thêm `public Color ShapeColor { get; private set; } = Color.white;`.
- `Initialize(BlockShapeData data, GameObject singleBlockPrefab, BlockRotation rotation = BlockRotation.Rot_0, float cellSize = 0f, Color? color = null)`: đặt `ShapeColor = color ?? data.shapeColor` ngay đầu hàm; dòng tô màu dùng `ShapeColor`.
- `SetDimmed`: `Color originalColor = ShapeColor;`.

### Bước 4: `BlockSpawner`
- Thêm `private BlockColorPaletteSO colorPalette;`, `private bool paletteWarningLogged;`, `private readonly List<Color> usedColors = new List<Color>();`.
- `SpawnNewHand`: clear `usedColors`, gom `ShapeColor` của các `BlockShape` đang có trong slot, rồi spawn slot trống.
- `SpawnRandomShapeAt`: `Color color = PickColor(randomData);` → `usedColors.Add(color)` → truyền `color:` vào `Initialize`.
- `PickColor`: nếu `colorPalette == null` thì `colorPalette = Locator.Data?.GetSOData<BlockColorPaletteSO>()`; nếu vẫn null hoặc `TryGetRandomColor` trả `false` thì warning một lần và trả `data.shapeColor`.
- `SpawnRandomShapeAt` được gọi từ chỗ khác ngoài `SpawnNewHand` không? Kiểm tra; nếu có (vd. restart), đảm bảo `usedColors` được làm mới đúng.

### Bước 5: `BlockGrid`
- `GetShapeColor`: `return shape != null ? shape.ShapeColor : Color.white;`.
- Grep lại toàn bộ `shapeData.shapeColor` trong `Assets/_Game` để không sót chỗ đọc màu cũ.

### Bước 6: Asset và prefab
- Ưu tiên Unity MCP (`manage_scriptable_object`) để tạo `BlockColorPalette.asset` sau khi Unity compile xong, với 6 màu Block Blast:  
  đỏ `#E94B3C`, cam `#F39C12`, vàng `#F7D14C`, xanh lá `#4CC764`, xanh dương `#3A8DDE`, tím `#9B59B6`.
- Gán vào `DataManager.prefab` qua MCP (`manage_prefabs`/`manage_components`). Nếu MCP không kết nối: viết YAML asset theo GUID trong `BlockColorPaletteSO.cs.meta`, rồi thêm dòng `blockColorPalette: {fileID: 11400000, guid: <guid asset>, type: 2}` vào MonoBehaviour `DataManager` trong prefab.
- Không đụng các field mồ côi khác trong prefab (`levelData`).

### Bước 7: Test EditMode `BlockColorPaletteTests`
- `TryGetRandomColor_WhenEmpty_ReturnsFalse`
- `TryGetRandomColor_WhenHasColors_ReturnsColorFromList`
- `TryGetRandomColor_WhenColorHasZeroAlpha_ReturnsOpaqueColor`
- `TryGetRandomColor_WithExclude_ReturnsUnusedColor` (palette 3 màu, exclude 2 → luôn ra màu còn lại, lặp 20 lần)
- `TryGetRandomColor_WhenAllExcluded_ReturnsColorFromList`
- `EditModeTests.asmdef` đã tham chiếu `Quang.Data` và `BlockDrag`, không cần sửa.

### Bước 8: Cập nhật `architecture.md`
- Dòng `_BlockDrag/Data → Quang.Data`: thêm `BlockColorPaletteSO` và `partial class DataManager` (đăng ký SO của game qua `TryGetGameSOData`), kèm lý do: field của `DataManager` phải là kiểu mà `Quang.Data` nhìn thấy.

## 3. Kiểm chứng
1. Unity compile không lỗi (`read_console` / `getDiagnostics`).
2. Chạy EditMode tests: tất cả test cũ và 5 test mới pass.
3. Kiểm tra thủ công trong `GameScene` (chạy cả từ `LoadStart`):
   - 3 khối mỗi lượt có 3 màu khác nhau, đều thuộc palette.
   - Đặt 1–2 khối, lượt mới chỉ spawn khi hết tay; màu không trùng các khối mới khác trong lượt.
   - Khối không đặt được bị làm tối theo đúng màu của nó; hết dimmed thì sáng lại đúng màu.
   - Preview phá hàng glow theo màu khối đang kéo.
   - Bỏ palette khỏi prefab → khối dùng `BlockShapeData.shapeColor`, chỉ 1 warning.
   - `LoseCanvas` / `RevivePopup` đọc được `GameplayData` (không còn `NullReferenceException`).
4. `git status`: chỉ có các file trong bảng mục 1 cùng `.meta` của chúng.
