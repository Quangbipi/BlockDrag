# Kế Hoạch Triển Khai: Cân Bằng Độ Khó Khi Sinh Khối (Smart Hand Generator)

> **Ngày tạo:** 2026-10-13  
> **Thiết kế:** [`2026-10-13-spawn-difficulty-design.md`](../designs/2026-10-13-spawn-difficulty-design.md)  
> **Assembly bị ảnh hưởng:** `BlockDrag`, `EditModeTests`. Không đổi `.asmdef` / `.asmref` nào, không đụng `Quang.*`, không đụng `BlockDrag.UI`.

---

## 0. Dữ liệu shape (đã xử lý)

- `Block_L_2_1` đã bị xoá (trùng hình với `Block_L_2`). `Block_I_2` sửa lại thành 2 ô.
- Vẫn dedupe theo **family** (key chuẩn hoá của 4 góc xoay) để chặn trường hợp sau này lại có 2 asset cùng hình.
- `GameScene` còn một phần tử `availableShapes` trỏ tới GUID của `Block_L_2_1` đã xoá, thành ra **null**. Bước 8 xoá phần tử này khỏi list. Ngoài ra `HandGenerator` bỏ qua shape null, nên lỡ sót cũng không lỗi.

---

## 1. File thay đổi

| # | File | Loại | Nội dung |
| :--- | :--- | :--- | :--- |
| 1 | [`Assets/_Game/_BlockDrag/Board/BlockBoard.cs`](../../Assets/_Game/_BlockDrag/Board/BlockBoard.cs) | Sửa | `OccupiedCount`, `FilledRatio`, `CopyFrom`, `Place` |
| 2 | `Assets/_Game/_BlockDrag/Spawn/ShapeVariant.cs` (+ `.meta`) | Mới | Footprint đã xoay, `Key`, `FamilyKey`, `BuildDistinct` |
| 3 | `Assets/_Game/_BlockDrag/Spawn/SpawnSettings.cs` (+ `.meta`) | Mới | POCO tham số tuning + `CreateDefault()` |
| 4 | `Assets/_Game/_BlockDrag/Spawn/DifficultyCurve.cs` (+ `.meta`) | Mới | `Evaluate(score, baselineHighScore, settings)` |
| 5 | `Assets/_Game/_BlockDrag/Spawn/HandSolver.cs` (+ `.meta`) | Mới | `IsSolvable`, `CanClearAnyLine` |
| 6 | `Assets/_Game/_BlockDrag/Spawn/HandGenerator.cs` (+ `.meta`) | Mới | Sinh hand theo thuật toán trong design |
| 7 | `Assets/_Game/_BlockDrag/Config/SpawnConfigSO.cs` (+ `.meta`) | Mới | `[CreateAssetMenu("BlockDrag/Spawn Config")]`: `SpawnSettings` + `List<ShapeWeight>` |
| 8 | `Assets/_Game/_BlockDrag/Config/SpawnConfig.asset` (+ `.meta`) | Mới | Asset với weight mặc định (mục 3) |
| 9 | [`Assets/_Game/_BlockDrag/BlockSpawner.cs`](../../Assets/_Game/_BlockDrag/BlockSpawner.cs) | Sửa | Dùng `HandGenerator`, chụp baseline HighScore |
| 10 | [`Assets/_Game/_Scenes/Game/GameScene.unity`](../../Assets/_Game/_Scenes/Game/GameScene.unity) | Sửa (serialized) | Gán `spawnConfig`, xoá phần tử null trong `availableShapes` |
| 11 | `Assets/Tests/EditMode/HandGeneratorTests.cs` (+ `.meta`) | Mới | Test generator, solver, variant, difficulty |
| 12 | [`Assets/Tests/EditMode/BlockBoardTests.cs`](../../Assets/Tests/EditMode/BlockBoardTests.cs) | Sửa | Test API mới của `BlockBoard` |
| 13 | [`.agents/rules/architecture.md`](../../.agents/rules/architecture.md) | Sửa | Thêm `Spawn/` vào mục *Gameplay Structure* |
| 14 | — | — | Asset `Block_L_2_1` (đã xoá) và `Block_I_2` (đã sửa) do bạn xử lý |

---

## 2. Các bước

### Bước 1: Mở rộng `BlockBoard`
```csharp
public int OccupiedCount { get; }            // đếm khi gọi, bàn 8x8 rẻ
public float FilledRatio => (float)OccupiedCount / (Rows * Columns);
public void CopyFrom(BlockBoard other);      // cùng kích thước, Array.Copy occupied; khác kích thước → ArgumentException
public void Place(IShapeFootprint shape, Vector2Int baseCoord); // SetOccupied các ô của shape, không kiểm tra
```
- Không đổi hành vi các API hiện có.
- `BlockGrid` không cần sửa.

### Bước 2: `ShapeVariant`
```csharp
public sealed class ShapeVariant : IShapeFootprint
{
    public BlockShapeData Data { get; }
    public BlockRotation Rotation { get; }
    public int RowsCount { get; }
    public int ColumnsCount { get; }
    public int CellCount { get; }
    public string Key { get; }        // vd "2x2:1101"
    public string FamilyKey { get; }  // min(Key) của 4 góc xoay
    public bool HasBlockAt(int r, int c);

    public static List<ShapeVariant> BuildDistinct(BlockShapeData data); // 1..4 variant, bỏ góc trùng Key
}
```
- Footprint copy ra `bool[,]` một lần, nên solver không phải gọi `string.Split` của `BlockShapeData.HasBlockAt` hàng nghìn lần.
- `HasBlockAtRotated` hiện có được dùng để dựng footprint, đảm bảo khớp 100% với cách `BlockShape` hiển thị.

### Bước 3: `SpawnSettings` + `SpawnConfigSO`
```csharp
[Serializable] public class SpawnSettings
{
    public AnimationCurve helpChanceByFill;   // (0,.15) (.4,.3) (.7,.8) (1,.9), tangent linear
    [Range(0,1)] public float lateHelpMultiplier = 0.5f;
    public int minBaseline = 1000;
    [Range(0,1)] public float belowRecordMaxT = 0.3f;
    public float overRecordRangeRatio = 0.5f;
    public int minOverRange = 500;
    public int maxAttempts = 30;
    public int maxSolverNodes = 20000;
    public static SpawnSettings CreateDefault();
}

[Serializable] public class ShapeWeight { public BlockShapeData shape; public float earlyWeight = 1; public float lateWeight = 1; }

public class SpawnConfigSO : ScriptableObject
{
    [SerializeField] private SpawnSettings settings = SpawnSettings.CreateDefault();
    [SerializeField] private List<ShapeWeight> weights;
    public SpawnSettings Settings => settings;
    public bool TryGetWeight(BlockShapeData shape, out ShapeWeight w);
}
```
Shape không có trong `weights` thì nhận weight 1/1. Weight ≤ 0 thì shape không bao giờ ra.

### Bước 4: `DifficultyCurve`
Công thức theo design mục 2.2. Clamp `score` âm về 0. `baselineHighScore < minBaseline` thì dùng `minBaseline`.

### Bước 5: `HandSolver`
```csharp
public static bool IsSolvable(BlockBoard board, IReadOnlyList<IShapeFootprint> shapes, int maxNodes);
public static bool CanClearAnyLine(BlockBoard board, IShapeFootprint shape);
```
- DFS đệ quy. Mỗi tầng sâu dùng một `BlockBoard` scratch riêng, cấp phát sẵn 3 bàn và dùng lại giữa các lần gọi, nên không sinh GC.
- Mỗi tầng: với mỗi shape chưa dùng và mỗi vị trí `CanPlace`, thì `CopyFrom` sang tầng kế, `Place`, `ClearFullLines`, rồi đệ quy.
- Cắt nhánh: nếu một shape chưa dùng không `CanPlaceAnywhere` trên bàn hiện tại *và* không có shape nào phá hàng được thì không thể cứu, trả false sớm. (Tối ưu tuỳ chọn; chỉ thêm nếu đo thấy chậm.)
- Đếm node; vượt `maxNodes` thì trả false.
- `CanClearAnyLine`: duyệt vị trí `CanPlace`, gọi `FindLinesCompletedBy(...)`, có hàng/cột thì true.

### Bước 6: `HandGenerator`
```csharp
public sealed class HandGenerator
{
    public HandGenerator(IReadOnlyList<BlockShapeData> shapes, SpawnConfigSO config, System.Random rng);
    public List<ShapeVariant> Generate(BlockBoard board, int count, float difficulty,
                                       IReadOnlyList<IShapeFootprint> inHand, IReadOnlyCollection<string> inHandFamilies);
}
```
- Constructor: build pool variant, nhóm theo `FamilyKey`, tính weight early/late cho mỗi family. Bỏ qua shape null hoặc rỗng.
- `Generate`: theo đúng pseudocode trong design. `wantHelp` tung **một lần**. `clearing` tính một lần cho mỗi lần gọi. Solver nhận `inHand + hand mới`.
- `FallbackHand`: duyệt family theo `CellCount` nhỏ nhất tăng dần, thêm dần sao cho hand vẫn solvable. Không đủ thì trả hand toàn khối nhỏ nhất khác family.
- Pool rỗng thì trả list rỗng (spawner log warning).

### Bước 7: Nối vào `BlockSpawner`
- Thêm `[SerializeField] private SpawnConfigSO spawnConfig;`, `private HandGenerator handGenerator;`, `private int runBaselineHighScore = -1;`.
- `Start` / `RestartGame`: `runBaselineHighScore = -1`. Tạo `handGenerator` lazy với `new System.Random()`.
- `SpawnNewHand`:
  1. Gom khối còn trên tay (`BlockShape` → `ShapeVariant` tương ứng) để truyền `inHand` / `inHandFamilies`. Code giữ cho tổng quát, dù hiện tại thường là rỗng.
  2. Nếu `runBaselineHighScore < 0` và `Locator.Score != null` thì chụp `HighScore`.
  3. `t = DifficultyCurve.Evaluate(Locator.Score?.CurrentScore ?? 0, max(0, runBaselineHighScore), settings)`.
  4. `hand = handGenerator.Generate(blockGrid.Board, emptySlots, t, …)`.
  5. Đổ lần lượt vào các slot trống bằng `SpawnShapeAt(slot, variant)`. Đây là `SpawnRandomShapeAt` đổi tên, nhận `variant.Data` và `variant.Rotation` thay vì random.
- Xoá `AvailableRotations` (không còn dùng).
- Thứ tự gọi khi `RestartGame`: `blockGrid.ResetBoard()` chạy trước `SpawnNewHand`, nên generator thấy bàn trống. Đúng như mong muốn.
- **Lưu ý `RestartGame`:** cần kiểm tra `ScoreManager` reset điểm *trước* hay *sau* `SpawnNewHand`. Nếu sau, ván mới sẽ tính `t` theo điểm của ván cũ. Xử lý: khi `runBaselineHighScore` vừa reset thì dùng `score = 0` cho hand đầu tiên.

### Bước 8: Asset & scene
- Tạo `Config/SpawnConfig.asset` với weight ở mục 3, gán vào `BlockSpawner` trong `GameScene` (qua UnityMCP hoặc Inspector).
- Xoá phần tử null (GUID `Block_L_2_1` đã xoá) khỏi `availableShapes` trong `GameScene`.

### Bước 9: Tests (`EditModeTests`)
`BlockBoardTests`:
- `FilledRatio_AfterPlacingCells_ReturnsOccupiedFraction`
- `CopyFrom_CopiesOccupancyIndependently`
- `Place_MarksAllShapeCellsOccupied`

`HandGeneratorTests` (shape tạo bằng `ScriptableObject.CreateInstance<BlockShapeData>()` + gán `rows`, `System.Random` seed cố định):
- `BuildDistinct_Square_ReturnsSingleVariant`
- `BuildDistinct_IShape_ReturnsTwoVariants`
- `FamilyKey_RotatedEquivalentAssets_AreEqual` (2 shape tạo trong test là cùng hình khác góc xoay)
- `DifficultyCurve_BelowRecord_ScalesUpToBelowRecordMaxT`
- `DifficultyCurve_AboveRecordByRange_ReturnsOne`
- `DifficultyCurve_NoRecord_UsesMinBaseline`
- `IsSolvable_EmptyBoard_ReturnsTrue`
- `IsSolvable_PieceNeedsLineClearFirst_ReturnsTrue` (bàn được dựng để khối thứ 2 chỉ đặt được sau khi khối 1 phá hàng)
- `IsSolvable_NoRoom_ReturnsFalse`
- `CanClearAnyLine_RowMissingThreeCells_TrueForI3`
- `Generate_ManySeeds_NeverDuplicatesFamily` (500 seed)
- `Generate_ManySeeds_HandAlwaysSolvable` (500 seed, bàn ngẫu nhiên đầy 30–70%)
- `Generate_HelpChanceOne_ContainsClearingPiece`
- `Generate_HelpChanceZeroAndBoardEmpty_StillReturnsCountShapes`
- `Generate_ZeroWeightShape_NeverSpawns`
- `Generate_BoardAlmostFull_FallsBackToSmallPieces`

---

## 3. Weight mặc định trong `SpawnConfig.asset`

| Shape | Số ô | early | late |
| :--- | :---: | :---: | :---: |
| Single | 1 | 2 | 1 |
| I_2, Diagonal_2 | 2 | 3 | 2 |
| I_3, L_2, Diagonal_3 | 3 | 3 | 2 |
| Square, I_4, L, T, S, Z | 4 | 2 | 2 |
| Rec, I_5, L_3 | 5–6 | 1 | 2 |
| Square_3x3 | 9 | 0.5 | 1.5 |

---

## 4. Kiểm Chứng

**Tự động:**
- Chạy EditMode tests (UnityMCP `run_tests` hoặc batchmode). Toàn bộ test cũ và mới phải pass.

**Thủ công trong Editor (`GameScene`):**
1. Chơi nhiều ván, đổ bàn tới khoảng 70% đầy. Hand không bao giờ "chết ngay" (luôn có cách đặt hết 3 khối).
2. Không bao giờ thấy 2 khối cùng hình trong một hand .
3. Khi bàn gần đầy, khối phá hàng xuất hiện thường xuyên hơn.
4. Dùng Ingame Debug Console hoặc tạm log `t`, `pHelp` để xác nhận: chưa tới kỷ lục thì `t ≤ 0.3`; vượt kỷ lục thì `t` tăng dần tới 1.
5. Restart giữa ván: hand đầu tiên của ván mới dùng `t ≈ 0`.
6. Profiler: `SpawnNewHand` dưới 2 ms trên Editor. Nếu cần thì thử thêm trên một máy Android tầm trung.
7. Bỏ trống `spawnConfig` trên `BlockSpawner`: game vẫn chạy với tham số mặc định, có log warning một lần.

## 5. Ghi chú PR
- Có migration **serialized data**: asset mới `SpawnConfig.asset`, `GameScene.unity` gán field mới.
- Ghi chú rõ gameplay thay đổi (cách sinh khối) để QA test cảm giác độ khó.
