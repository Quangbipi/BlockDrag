# UI Startup Flow — Implementation Plan

Ngày: 2026-10-18 · Design: [2026-10-18-ui-startup-flow-design.md](../designs/2026-10-18-ui-startup-flow-design.md)

Quyết định đã chốt: LoadStart ở Build Settings index 1 (bạn đã thêm), HighScore count-up chạy lại mỗi lần Home mở, khoá kéo khối bằng cách tắt `BlockDragInput`.

## Bước 1 — `InitLoadingCanvas` tự chạy fake loading

File: `Assets/_Game/_UI/Scripts/InitLoadingCanvas.cs` (assembly `BlockDrag.UI`)

- Thêm field:
  - `[SerializeField] bool autoLoadOnStart = true;`
  - `[SerializeField] float fakeLoadDuration = 0.5f;`
  - `[SerializeField] string gameSceneName = "GameScene";`
- Field private `Tween loadingTween; bool isLoadingScene;`
- `protected override void Start()`: gọi `base.Start()`; nếu `autoLoadOnStart` → `StartFakeLoading()`.
- `StartFakeLoading()`: `SetPercentage(0)`; nếu `fakeLoadDuration <= 0` → `SetPercentage(1)` + `LoadGameScene()`; ngược lại `DOVirtual.Float(0f, 1f, fakeLoadDuration, SetPercentage).SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject).OnComplete(LoadGameScene)`.
- `LoadGameScene()`: return nếu `isLoadingScene`; set cờ; `SceneGameManager.Ins.LoadingSceneAsync(gameSceneName)`.
- `SetPercentage`: clamp `value` về 0..1 trước khi gán bar/text.
- Xoá `Awake` rỗng (chỉ có code comment) nếu không cần.

## Bước 2 — Gỡ auto-load của package khỏi LoadStart scene

File: `Assets/_Game/_Scenes/Game/LoadStart.unity`

- Xoá component `Quang.Base.Init.LoadStart` trên GameObject `LoadStart` (dùng Unity MCP `manage_components` hoặc Editor). Giữ nguyên `SceneGameManager`, `UIManager`, `DataManager`, `GameEventManager`, instance `InitLoadingCanvas`.
- Kiểm tra instance `InitLoadingCanvas` trong LoadStart: `autoLoadOnStart = true` (mặc định từ prefab).
- Instance `InitLoadingCanvas` (inactive) trong `GameScene.unity`: override `autoLoadOnStart = false` để an toàn.

## Bước 3 — `HomeCanvas` count-up HighScore

File: `Assets/_Game/_UI/Scripts/HomeCanvas.cs`

- Thêm `[SerializeField] float highScoreCountDuration = 0.5f;` và `readonly ScoreCountUp highScoreCounter = new ScoreCountUp();`
- `OnEnable()` (chạy mỗi lần canvas được bật khi Open/Show): `highScoreCounter.Duration = highScoreCountDuration; highScoreCounter.Snap(0); highScoreTxt.text = "0"; highScoreCounter.SetTarget(Locator.Score != null ? Locator.Score.HighScore : 0);`
- `Update()`: nếu `highScoreCounter.Tick(Time.unscaledDeltaTime)` → `highScoreTxt.text = highScoreCounter.Displayed.ToString();`
- `OnSetting`: bỏ `throw new NotImplementedException()` → no-op kèm comment TODO (tránh crash khi bấm).
- Thêm `OnDestroy` hủy đăng ký `_OnClick` của 2 nút (cặp với `Awake`) — nếu `UISCanvas.OnDestroy` là override thì gọi `base.OnDestroy()`.
- Dọn các `using` thừa chỉ khi chắc chắn không dùng (giữ thay đổi tối thiểu).

## Bước 4 — `GameSceneFlow` mở Home + khoá input

File mới: `Assets/_Game/_UI/Scripts/GameSceneFlow.cs` (+ `.meta`), namespace `UI`, assembly `BlockDrag.UI`.

```csharp
public class GameSceneFlow : MonoBehaviour
{
    [SerializeField] BlockDragInput blockDragInput;
    HomeCanvas homeCanvas;

    void Start()
    {
        if (Locator.UI == null)
        {
            Debug.LogWarning("[GameSceneFlow] Chưa có UIManager (chạy thẳng GameScene?), bỏ qua HomeCanvas.");
            return;
        }
        SetInputEnabled(false);
        homeCanvas = Locator.UI.OpenUI<HomeCanvas>();
        homeCanvas._OnClose += OnHomeClosed;
    }

    void OnHomeClosed(UISCanvas _) => SetInputEnabled(true);
    void OnDestroy() { if (homeCanvas != null) homeCanvas._OnClose -= OnHomeClosed; }
    void SetInputEnabled(bool v) { if (blockDragInput != null) blockDragInput.enabled = v; }
}
```

- Fallback `blockDragInput == null` → `FindObjectOfType<BlockDragInput>()` trong `Awake`.
- Lưu ý: kiểm tra `Locator.UI` có trả về null khi chưa đăng ký hay throw — điều chỉnh check cho đúng khi code.

## Bước 5 — Wiring GameScene

File: `Assets/_Game/_Scenes/Game/GameScene.unity`

- Thêm `GameSceneFlow` lên GameObject `GameManager` (hoặc GameObject mới `GameSceneFlow`), gán `blockDragInput`.
- Kiểm tra sorting: `HomeCanvas` (mở qua UIManager) hiển thị trên `GameplayCanvas`. Nếu bị đè → chỉnh `sortingOrder` trong `Resources/UI/HomeCanvas.prefab`.
- Xác nhận `HomeCanvas.highScoreTxt` đã gán text `HightScore` trong prefab.

## Bước 6 — Build Settings

- Xác nhận `ProjectSettings/EditorBuildSettings.asset` có thứ tự `CryptoLoader (0) → LoadStart (1) → GameScene (2)`. **Hiện file trên đĩa chưa có LoadStart** — có thể bạn chưa Save Project; mình sẽ kiểm tra lại, nếu vẫn thiếu thì thêm vào.

## Impacted files

| File | Loại |
|---|---|
| `Assets/_Game/_UI/Scripts/InitLoadingCanvas.cs` | sửa |
| `Assets/_Game/_UI/Scripts/HomeCanvas.cs` | sửa |
| `Assets/_Game/_UI/Scripts/GameSceneFlow.cs` (+ `.meta`) | mới |
| `Assets/_Game/_Scenes/Game/LoadStart.unity` | gỡ component `LoadStart` |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | thêm `GameSceneFlow`, override `autoLoadOnStart=false` |
| `Assets/_Game/Resources/UI/HomeCanvas.prefab` | chỉ khi cần sửa sorting/wiring |
| `ProjectSettings/EditorBuildSettings.asset` | chỉ khi LoadStart vẫn thiếu |

Không sửa package `com.quang.*`, không đổi asmdef, không đổi dependency direction.

Lưu ý: `HomeCanvas.cs`, `HomeCanvas.prefab`, `InitLoadingCanvas.prefab`, `GameScene.unity`, `LoadStart.unity` đang có thay đổi chưa commit của bạn — mình chỉ sửa thêm đúng phần trên, không revert gì.

## Verification

1. **Compile**: `read_console` không có error sau khi Unity refresh.
2. **EditMode tests**: chạy toàn bộ EditMode (`ScoreCountUpTests` đã cover logic count-up dùng lại) — phải pass.
3. **Play từ LoadStart** (Editor):
   - Bar chạy 0% → 100% trong ~0.5s, text `%` khớp, rồi vào GameScene.
   - HomeCanvas hiện đầu tiên; HighScore chạy từ 0 → HighScore hiện tại trong ~0.5s (HighScore = 0 → hiện "0").
   - Khi Home đang mở, kéo khối phía sau **không** được.
   - Nhấn Start → Home ẩn, GameplayCanvas + bàn chơi hiện, kéo khối được.
   - Console không có error/exception.
4. **Play thẳng GameScene**: chỉ có warning `[GameSceneFlow]`, input vẫn hoạt động, không crash.
5. Mình sẽ báo kết quả từng mục; mục nào không tự chạy được qua Unity MCP sẽ nêu rõ để bạn kiểm tay.

## Bổ sung (đã duyệt, đã làm) — mở GameplayCanvas khi nhấn Start

- `GameSceneFlow.OnHomeClosed`: `Locator.UI.OpenUI<GameplayCanvas>()` trước `SetInputEnabled(true)`.
- Verification thêm: sau khi nhấn Start, `Locator.UI.IsOpened<GameplayCanvas>() == true`, Score/HighScore trên GameplayCanvas đúng, chơi được và cộng điểm hiển thị bình thường.

## Bổ sung (đã duyệt, đã làm) — tắt InitLoadingCanvas khi vào GameScene

- `InitLoadingCanvas.LoadGameScene()`: `SceneGameManager.Ins.OnSceneLoaded += OnGameSceneLoaded;` rồi mới `LoadingSceneAsync`.
- `OnGameSceneLoaded(int)`: huỷ đăng ký, `Destroy(gameObject)`. `OnDestroy` cũng huỷ đăng ký (an toàn) và gọi `base.OnDestroy()`.
- Verification thêm: trong GameScene không còn instance `InitLoadingCanvas` nào active dưới `UIManager`; Home hiện ngay sau loading.
