# Kế Hoạch Triển Khai: Hỗ Trợ Đầu Game (Assist Hand) & Thưởng Dọn Sạch Bàn (Perfect Clear)

> **Ngày tạo:** 2026-10-13  
> **Thiết kế:** [`2026-10-13-early-game-assist-perfect-clear-design.md`](../designs/2026-10-13-early-game-assist-perfect-clear-design.md)  
> **Assembly bị ảnh hưởng:** `BlockDrag`, `Quang.Base` (asmref `_BlockDrag/Base`, chỉ `IScoreService`), `BlockDrag.UI`, `EditModeTests`. Không đổi `.asmdef` / `.asmref` nào.

---

## 1. File thay đổi

| # | File | Loại | Nội dung |
| :--- | :--- | :--- | :--- |
| 1 | `Assets/_Game/_BlockDrag/Spawn/ShapeFamily.cs` (+ `.meta`) | Mới | Tách class `Family` lồng trong `HandGenerator` ra thành `internal sealed class ShapeFamily` để `AssistHandBuilder` dùng chung |
| 2 | `Assets/_Game/_BlockDrag/Spawn/AssistHandBuilder.cs` (+ `.meta`) | Mới | Dựng hand theo chuỗi phá hàng, `PlacementScore` |
| 3 | [`Spawn/HandGenerator.cs`](../../Assets/_Game/_BlockDrag/Spawn/HandGenerator.cs) | Sửa | Dùng `ShapeFamily`. `Generate(..., bool belowRecord)`, chọn assist hoặc random |
| 4 | [`Spawn/DifficultyCurve.cs`](../../Assets/_Game/_BlockDrag/Spawn/DifficultyCurve.cs) | Sửa | `IsBelowRecord(score, baseline, settings)`, `AssistChance(belowRecord, difficulty, settings)` |
| 5 | [`Spawn/SpawnSettings.cs`](../../Assets/_Game/_BlockDrag/Spawn/SpawnSettings.cs) | Sửa | Nhóm `[Header("Assist")]`: `fadeAssistAfterRecord`, `assistChanceAtRecord`, `assistTolerance`, `clearLineScore`, `perfectClearScore`, `contactScore` |
| 6 | [`BlockSpawner.cs`](../../Assets/_Game/_BlockDrag/BlockSpawner.cs) | Sửa | Tính `belowRecord`, truyền vào `Generate` |
| 7 | [`BlockGrid.cs`](../../Assets/_Game/_BlockDrag/BlockGrid.cs) | Sửa | `event Action<Vector3> OnBoardCleared`, `BoardCenterPosition`, bắn event trong `CheckAndClearLines` |
| 8 | [`Config/ScoreConfigSO.cs`](../../Assets/_Game/_BlockDrag/Config/ScoreConfigSO.cs) | Sửa | `perfectClearBonus` (300), `perfectClearMultiplyByCombo` (true), cập nhật `CreateDefault` |
| 9 | [`Base/IScoreService.cs`](../../Assets/_Game/_BlockDrag/Base/IScoreService.cs) | Sửa | `event Action<int, Vector3> OnPerfectClear` |
| 10 | [`ScoreManager.cs`](../../Assets/_Game/_BlockDrag/ScoreManager.cs) | Sửa | Subscribe/unsubscribe `OnBoardCleared`, `HandlePerfectClear(Vector3)` |
| 11 | [`_UI/Scripts/ComboTextEffect.cs`](../../Assets/_Game/_UI/Scripts/ComboTextEffect.cs) | Sửa | Overload `Play(string label, string value, Vector3 pos, Action onComplete = null)`. `Play(int, …)` gọi lại overload này |
| 12 | [`_UI/Scripts/ComboEffectManager.cs`](../../Assets/_Game/_UI/Scripts/ComboEffectManager.cs) | Sửa | Bind/unbind `OnPerfectClear`, `HandlePerfectClear(int bonus, Vector3 pos)` |
| 13 | [`Config/SpawnConfig.asset`](../../Assets/_Game/_BlockDrag/Config/SpawnConfig.asset) | Sửa (serialized) | Field mới nhận giá trị mặc định khi Unity reserialize. Kiểm tra lại trong Inspector |
| 14 | [`Config/ScoreConfig.asset`](../../Assets/_Game/_BlockDrag/Config/ScoreConfig.asset) | Sửa (serialized) | Như trên: `perfectClearBonus` = 300, `perfectClearMultiplyByCombo` = true |
| 15 | `Assets/Tests/EditMode/AssistHandBuilderTests.cs` (+ `.meta`) | Mới | Test assist |
| 16 | [`Tests/EditMode/HandGeneratorTests.cs`](../../Assets/Tests/EditMode/HandGeneratorTests.cs) | Sửa | Cập nhật chữ ký `Generate`, test `AssistChance` |
| 17 | [`Tests/EditMode/ScoreSystemTests.cs`](../../Assets/Tests/EditMode/ScoreSystemTests.cs) | Sửa | Test thưởng Perfect Clear |
| 18 | [`Tests/EditMode/BlockDragLocatorTests.cs`](../../Assets/Tests/EditMode/BlockDragLocatorTests.cs) | Sửa | `FakeScoreService` thêm `OnPerfectClear` (bắt buộc để compile) |
| 19 | [`Tests/EditMode/ComboEffectTests.cs`](../../Assets/Tests/EditMode/ComboEffectTests.cs) | Sửa | Test overload `Play(string, string, …)` |
| 20 | [`.agents/rules/architecture.md`](../../.agents/rules/architecture.md) | Sửa | Dòng `Spawn/` thêm `AssistHandBuilder` |

---

## 2. Các bước

### Bước 1: `ShapeFamily` (refactor, không đổi hành vi)
```csharp
internal sealed class ShapeFamily
{
    public string Key;
    public readonly List<ShapeVariant> Variants = new List<ShapeVariant>();
    public float EarlyWeight, LateWeight;
    public int MinCells = int.MaxValue;
    public float Weight(float difficulty);
}
```
- `HandGenerator` đổi `Family` thành `ShapeFamily`. Logic giữ nguyên.
- Chạy lại `HandGeneratorTests` để xác nhận không đổi hành vi.

### Bước 2: `SpawnSettings` + `DifficultyCurve`
```csharp
[Header("Assist (dựng hand theo chuỗi phá hàng)")]
public bool fadeAssistAfterRecord = true;            // false = tắt assist ngay khi vượt kỷ lục
[Range(0,1)] public float assistChanceAtRecord = 0.5f;
[Range(0,1)] public float assistTolerance = 0.15f;
public float clearLineScore = 100f;
public float perfectClearScore = 10000f;
public float contactScore = 0.5f;
```
```csharp
public static bool IsBelowRecord(int score, int baselineHighScore, SpawnSettings s)
    => score < Mathf.Max(baselineHighScore, s.minBaseline);

public static float AssistChance(bool belowRecord, float difficulty, SpawnSettings s)
{
    if (belowRecord) return 1f;
    if (!s.fadeAssistAfterRecord) return 0f;
    float u = belowRecordMaxT >= 1 ? 1 : Clamp01((difficulty - belowRecordMaxT) / (1 - belowRecordMaxT));
    return Lerp(s.assistChanceAtRecord, 0f, u);
}
```

### Bước 3: `AssistHandBuilder`
```csharp
internal sealed class AssistHandBuilder
{
    public AssistHandBuilder(IReadOnlyList<ShapeFamily> families, SpawnSettings settings, System.Random rng);
    // Trả hand đã dựng (có thể ít hơn count nếu không còn placement hợp lệ)
    public List<ShapeVariant> Build(BlockBoard board, int count, float difficulty, ISet<string> excludedFamilies);
    public static float PlacementScore(BlockBoard sim, ShapeVariant v, Vector2Int pos, SpawnSettings s);
}
```
- Bàn mô phỏng `sim` và bàn nháp `probe` được cấp phát một lần trong builder, cùng kích thước bàn thật (tạo lại nếu kích thước đổi).
- **PlacementScore**: `probe.CopyFrom(sim)`, `probe.Place`, đếm hàng/cột đầy (`lines`), rồi tính:
  - Nếu `lines > 0`: `probe.ClearFullLinesInPlace()`; bàn trống thì `+perfectClearScore`; cộng `lines * clearLineScore`.
  - Nếu `lines == 0`: setup = Σ (ô đã lấp / kích thước)² trên các hàng/cột khối chạm vào (tính trên `probe` trước khi phá).
  - Contact: đếm cạnh của các ô khối giáp ô đã chiếm hoặc mép bàn (tính trên `sim`) × `contactScore`.
- **Build**: mỗi bước, với mỗi family không bị loại thì duyệt mọi variant và mọi vị trí `CanPlace`, giữ placement điểm cao nhất. Pool = các family có `score ≥ top - |top| × assistTolerance`. Chọn trong pool theo `Weight(difficulty)`; nếu tổng weight của pool bằng 0 thì chọn đều. Sau đó `sim.Place` + `ClearFullLinesInPlace`.
- Đếm hàng đầy trên `probe`: thêm vào `BlockBoard` hàm `int CountFullLines()` (không cấp phát, dùng `IsRowFull`/`IsColumnFull` có sẵn).

### Bước 4: `HandGenerator.Generate`
```csharp
public List<ShapeVariant> Generate(BlockBoard board, int count, float difficulty,
                                   IReadOnlyList<ShapeVariant> inHand = null, bool belowRecord = false)
```
- Tính `assistChance = DifficultyCurve.AssistChance(belowRecord, difficulty, settings)`, rồi tung `rng.NextDouble() < assistChance`.
- Assist:
  - Nếu `inHand` không rỗng: thêm family của chúng vào `excludedFamilies`. Builder đặt các khối đó vào `sim` trước (greedy theo `PlacementScore`), để chuỗi tính tiếp từ bàn sau khi chúng đã nằm xuống. Hiện tại `inHand` luôn rỗng, nhưng giữ cho đúng tổng quát.
  - Gọi `builder.Build(...)`. Thiếu khối thì bù bằng luồng random, kiểm tra solvable cho cả hand.
  - Cuối cùng `Shuffle`.
- Random: giữ nguyên code hiện tại.
- Tham số mặc định `belowRecord = false`, nên test cũ vẫn chạy theo luồng random như trước. Riêng `fadeAssistAfterRecord = true` cho `assistChance` = 0.5 khi difficulty thấp, nên test cũ cần cấu hình tắt assist (xem bước 9).

### Bước 5: `BlockSpawner`
```csharp
int score = firstHandOfRun ? 0 : Locator.Score.CurrentScore;
bool belowRecord = DifficultyCurve.IsBelowRecord(score, runBaselineHighScore, Settings);
Generator.Generate(GetBoard(), emptySlots.Count, difficulty, inHand, belowRecord);
```
`EvaluateDifficulty` trả thêm `score` dùng chung (đổi thành `EvaluateRunState(out float difficulty, out bool belowRecord)`). Khi `Locator.Score == null` thì `belowRecord = true`.

### Bước 6: Perfect Clear: gameplay
- `BlockGrid`:
  - `public event Action<Vector3> OnBoardCleared;`
  - `public Vector3 BoardCenterPosition => (GetCellWorldPosition(0,0) + GetCellWorldPosition(maxRow-1, maxColumn-1)) * 0.5f;`
  - Trong `CheckAndClearLines`, sau 2 event `OnLinesCleared*`: `if (Board.OccupiedCount == 0) OnBoardCleared?.Invoke(BoardCenterPosition);`
- `ScoreConfigSO`: thêm 2 field + getter, có tooltip, thuộc `[Header("Perfect Clear")]`.
- `IScoreService`: `event Action<int, Vector3> OnPerfectClear; // (bonus, worldPos) khi dọn sạch bàn`.
- `ScoreManager`:
  - Thêm `OnBoardCleared` vào `SubscribeEvents`/`UnsubscribeEvents`.
  - ```csharp
    public void HandlePerfectClear(Vector3 boardCenter)
    {
        int multiplier = Config.PerfectClearMultiplyByCombo ? Mathf.Max(1, currentCombo) : 1;
        int bonus = Config.PerfectClearBonus * multiplier;
        if (bonus <= 0) return;
        AddScore(bonus);
        OnPerfectClear?.Invoke(bonus, boardCenter);
    }
    ```
  - Thứ tự đúng: `OnLinesClearedWithPos` (cập nhật `currentCombo`) chạy trước `OnBoardCleared`, nên `currentCombo` đã tính lần phá này.

### Bước 7: Perfect Clear: UI
- `ComboTextEffect`: tách phần thân `Play` hiện tại thành `Play(string label, string value, Vector3 spawnPosition, Action onComplete = null)`, thay `"COMBO"` và `$"x{comboCount}"` bằng tham số. `Play(int comboCount, …)` chỉ còn một dòng gọi overload.
- `ComboEffectManager`:
  - `BindScore`/`UnbindScore` thêm `OnPerfectClear`.
  - `HandlePerfectClear(int bonus, Vector3 pos) { … GetFromPool().Play("PERFECT", $"+{bonus}", pos); }`
- Màu và cỡ chữ: giữ như combo ở bước này. Chỉnh sau khi bạn xem thực tế.

### Bước 8: Asset
- Refresh Unity. Kiểm tra `SpawnConfig.asset` và `ScoreConfig.asset` hiện đủ field mới với giá trị mặc định. Nếu YAML cũ không có field thì Unity dùng giá trị khởi tạo trong code. Lưu lại asset để ghi field mới xuống file.

### Bước 9: Tests
`AssistHandBuilderTests` (dùng lại cách tạo shape của `HandGeneratorTests`):
- `PlacementScore_LineClear_ScoresHigherThanNoClear`
- `PlacementScore_PerfectClear_ScoresHighest` (bàn chỉ còn 1 hàng thiếu 3 ô, I_3 lấp vào làm trống bàn)
- `Build_RowMissingThreeCells_FirstChainPieceClearsLine`: hand có khối phá được hàng đó.
- `Build_ChainedClears_SecondPieceClearsAfterFirst`: dựng bàn có 2 hàng chờ phá. Mô phỏng đặt theo chuỗi thì cả 2 hàng bị phá. Kiểm tra bằng solver chạy chế độ "đếm số lần phá tối đa" trong test helper.
- `Build_ManySeeds_NeverDuplicatesFamily_AndAlwaysSolvable` (300 seed, bàn ngẫu nhiên)
- `Build_EmptyBoard_ReturnsCountShapes`

`HandGeneratorTests`:
- Các test cũ gọi `Generate` với settings `fadeAssistAfterRecord = false` để giữ đúng luồng random đang được test.
- `AssistChance_BelowRecord_IsOne`
- `AssistChance_AfterRecord_FadeOff_IsZero`
- `AssistChance_AfterRecord_FadeOn_LerpsFromAtRecordToZero`
- `IsBelowRecord_UsesMinBaselineForNewPlayer`
- `Generate_BelowRecord_UsesAssist` (bàn có hàng chờ phá; 50 seed đều có khối phá hàng)

`ScoreSystemTests`:
- `HandlePerfectClear_WithCombo3_AwardsBonusTimesCombo` (300 × 3 = 900)
- `HandlePerfectClear_MultiplyDisabled_AwardsBaseBonus`
- `HandlePerfectClear_RaisesOnPerfectClearWithBonusAndPosition`

`ComboEffectTests`:
- `Play_WithLabelAndValue_SetsTexts` ("PERFECT", "+900")
- Test cũ `Play(int)` vẫn ra "COMBO" / "x3".

`BlockDragLocatorTests`: `FakeScoreService` thêm `public event Action<int, Vector3> OnPerfectClear { add { } remove { } }`.

---

## 3. Kiểm Chứng

**Tự động:**
- Chạy toàn bộ EditMode tests. Test mới và test cũ đều phải pass. Riêng 6 test `GameOverCheckTests` đã lỗi từ trước (prefab null), không thuộc task này.
- Đo `Generate` ở chế độ assist bằng `execute_code` (300 bàn ngẫu nhiên). Mục tiêu: trung bình dưới 1 ms, lâu nhất dưới 3 ms trong Editor.

**Thủ công trong `GameScene`:**
1. Ván mới (chưa tới kỷ lục): gần như mỗi hand đều có khối khớp chỗ cần phá; đặt đúng thì phá liên tiếp, ra "COMBO xN".
2. Dọn sạch bàn: chữ "PERFECT +N" hiện ở giữa bàn, điểm cộng đúng `300 × combo`.
3. Vượt kỷ lục với `fadeAssistAfterRecord = true`: hand "ngon" thưa dần. Đặt `false`: ngay sau kỷ lục, hand trở lại kiểu random.
4. Đổi `perfectClearBonus` và `perfectClearMultiplyByCombo` trong `ScoreConfig.asset`: điểm thưởng thay đổi theo.
5. Restart: hand đầu ván mới là assist.

## 4. Ghi chú PR
- **Serialized data:** field mới trong `SpawnConfig.asset` và `ScoreConfig.asset`.
- **Contract:** `IScoreService` thêm event `OnPerfectClear`. Mọi implementation phải thêm event này.
- **Gameplay thay đổi lớn ở đầu game:** cần QA cảm nhận độ dễ/khó trước và sau kỷ lục.
