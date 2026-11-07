# Event Firebase: thua ván và mốc combo — Implementation Plan

**Ngày:** 2026-11-07
**Design:** [2026-11-07-gameplay-analytics-events-design.md](../designs/2026-11-07-gameplay-analytics-events-design.md) (đã duyệt, phương án A)
**Trạng thái:** Đã duyệt, đã triển khai

## Tóm tắt event

| Event | Category | Tham số |
|---|---|---|
| `game_lose` | `AnalyticsCategory.Design` | `score` (int), `high_score` (int) |
| `combo_milestone` | `AnalyticsCategory.Design` | `combo` (int), `score` (int) |

## Các bước

### Bước 1 — `ComboMilestoneTracker` (C# thuần)

**Tạo** `Assets/_Game/_BlockDrag/Analytics/ComboMilestoneTracker.cs` (+ `.meta` do Unity sinh), namespace `Gameplay.BlockDrag`.

```csharp
public sealed class ComboMilestoneTracker
{
    public const int DefaultStep = 5;
    public ComboMilestoneTracker(int step = DefaultStep);   // step <= 0 → ArgumentOutOfRangeException
    public int LastMilestone { get; }
    // Combo mới < combo trước → reset chuỗi. Ghi các mốc step*k trong (LastMilestone, combo] vào results (Clear trước).
    public void Update(int combo, List<int> results);
    public void Reset();
}
```

- Dùng `List<int>` truyền vào để không cấp phát mỗi lần combo đổi.
- `combo <= 0` coi như reset.

### Bước 2 — `BlockDragAnalytics` (MonoBehaviour)

**Tạo** `Assets/_Game/_BlockDrag/Analytics/BlockDragAnalytics.cs`, `[AddComponentMenu("Gameplay/Block Drag Analytics")]`.

- `[SerializeField] ScoreManager scoreManager; [SerializeField] BlockSpawner blockSpawner;` — tự `FindObjectOfType` trong `Awake` nếu trống (giống `BlockDragAudio`).
- `OnEnable`: đăng ký `scoreManager.OnComboChanged`, `blockSpawner.OnGameOver`; `OnDisable`: huỷ đăng ký. Bỏ qua tham chiếu `null`.
- `HandleComboChanged(int combo, int bonus)`: `tracker.Update(combo, buffer)`; mỗi mốc → `LogEvent("combo_milestone", Design, { combo, score })`.
- `HandleGameOver()`: `LogEvent("game_lose", Design, { score, high_score = max(HighScore, CurrentScore) })`.
- Tên event và tham số để thành `const string` trong class (`GameLoseEvent`, `ComboMilestoneEvent`, `ParamScore`, …).
- Gửi qua hàm `internal static` thuần nhận `IAnalyticsService` để test được không cần scene: `LogGameLose(IAnalyticsService, int score, int highScore)`, `LogComboMilestone(IAnalyticsService, int combo, int score)`. Cả hai bỏ qua khi service `null`.

Không sửa `ScoreManager`, `BlockSpawner`, asmdef (`BlockDrag` đã tham chiếu `Quang.Base`).

### Bước 3 — Gắn vào scene

**Sửa** `Assets/_Game/_Scenes/Game/GameScene.unity`: thêm component `BlockDragAnalytics` vào GameObject **`BlockGrid`** (đang chứa `BlockDragAudio`), gán `scoreManager` và `blockSpawner` trong Inspector. Thực hiện qua Unity (MCP hoặc Editor), lưu scene.

### Bước 4 — Unit test

**Tạo** `Assets/Tests/EditMode/ComboMilestoneTrackerTests.cs`:

| Test | Kỳ vọng |
|---|---|
| `Update_ComboReachesFive_ReportsFive` | 1→2→3→4→5 → `[5]` ở bước cuối, các bước trước rỗng |
| `Update_ComboSkipsOverFive_ReportsFive` | 4→6 → `[5]` |
| `Update_ComboJumpsAcrossTwoMilestones_ReportsBoth` | 3→11 → `[5, 10]` |
| `Update_ComboStaysAboveMilestone_ReportsOnce` | 5→6→7 → chỉ lần đầu `[5]` |
| `Update_AfterComboDrops_ReportsMilestoneAgain` | 7→1→6 → `[5]` lần thứ hai |
| `Update_NewChainStartsAboveMilestone_ReportsIt` | 1→6 (chuỗi mới phá 6 hàng) → `[5]` |
| `Constructor_NonPositiveStep_Throws` | `step = 0` → exception |

**Tạo** `Assets/Tests/EditMode/BlockDragAnalyticsTests.cs` với một `IAnalyticsService` giả ghi lại `LogEvent`:

| Test | Kỳ vọng |
|---|---|
| `LogGameLose_SendsNameCategoryAndParams` | `game_lose`, `Design`, `score`, `high_score` đúng |
| `LogComboMilestone_SendsNameCategoryAndParams` | `combo_milestone`, `Design`, `combo`, `score` đúng |
| `Log_WhenServiceNull_DoesNotThrow` | không ném lỗi |

Nếu `EditModeTests.asmdef` chưa thấy được `internal` của `BlockDrag` thì dùng `InternalsVisibleTo` sẵn có hoặc đổi hai hàm sang `public static` (kiểm tra khi làm).

### Bước 5 — Cập nhật tài liệu

**Sửa** `.agents/rules/architecture.md`, mục *Gameplay Structure*: thêm dòng `Analytics/BlockDragAnalytics` + `ComboMilestoneTracker` (event `game_lose`, `combo_milestone` qua `Locator.Analytics`). Không đổi ranh giới module.

## File bị ảnh hưởng

| File | Thao tác |
|---|---|
| `Assets/_Game/_BlockDrag/Analytics/ComboMilestoneTracker.cs` (+ folder/.meta) | Tạo |
| `Assets/_Game/_BlockDrag/Analytics/BlockDragAnalytics.cs` (+ .meta) | Tạo |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | Sửa (thêm component vào `BlockGrid`) |
| `Assets/Tests/EditMode/ComboMilestoneTrackerTests.cs` (+ .meta) | Tạo |
| `Assets/Tests/EditMode/BlockDragAnalyticsTests.cs` (+ .meta) | Tạo |
| `.agents/rules/architecture.md` | Sửa (1 dòng) |

## Kiểm tra

1. Unity biên dịch không lỗi (`read_console`).
2. Chạy EditMode tests: test mới pass, test cũ không vỡ.
3. Play `GameScene` trong Editor: chơi tới combo ≥ 5 và thua ván; xác nhận qua log của analytics/Console rằng `combo_milestone` gửi đúng mốc và `game_lose` gửi **1 lần** mỗi ván.
4. (Bạn tự làm trên thiết bị) Firebase DebugView thấy 2 event và tham số; đăng ký `combo` (và nếu muốn `score`, `high_score`) làm custom dimension trong Firebase Console để xem theo mốc.
