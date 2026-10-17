# Home Open Anim Timing — Implementation Plan

Ngày: 2026-10-18
Design: [2026-10-18-home-open-anim-timing-design.md](../designs/2026-10-18-home-open-anim-timing-design.md)
Lựa chọn: loading **tắt tức thì** (không fade).

## File bị ảnh hưởng

| File | Loại | Thay đổi |
|---|---|---|
| `Assets/_Game/_UI/Scripts/InitLoadingCanvas.cs` | sửa | `Current`, `Dismiss()`, giữ loading sau `OnSceneLoaded` + safety timeout |
| `Assets/_Game/_UI/Scripts/GameSceneFlow.cs` | sửa | `Start` → coroutine preload / settle / handoff cùng frame |

Không sửa prefab, scene, package `com.quang.*`, DOTween settings. Field serialized mới có giá trị mặc định trong code → không cần chỉnh Inspector.

## Bước 1 — `InitLoadingCanvas.cs`

1. Thêm:
   ```csharp
   public static InitLoadingCanvas Current { get; private set; }

   [Tooltip("Sau khi GameScene load xong, nếu không ai gọi Dismiss trong thời gian này (giây, unscaled) thì tự tắt")]
   [SerializeField]
   float dismissTimeout = 3f;

   bool isDismissed;
   ```
2. `Awake()`: `Current = this;`
3. `OnGameSceneLoaded`: bỏ `Destroy(gameObject)`, thay bằng
   ```csharp
   DOVirtual.DelayedCall(dismissTimeout, Dismiss, true).SetLink(gameObject);
   ```
4. Thêm:
   ```csharp
   /// <summary>Tắt loading; GameScene gọi cùng frame mở UI đầu tiên để không lộ scene</summary>
   public void Dismiss()
   {
       if (isDismissed) return;
       isDismissed = true;
       Destroy(gameObject);
   }
   ```
5. `OnDestroy`: thêm `if (Current == this) Current = null;`

## Bước 2 — `GameSceneFlow.cs`

1. Thêm field:
   ```csharp
   [Tooltip("Số frame chờ sau khi preload HomeCanvas (loading vẫn che) để qua hitch load scene rồi mới mở Home")]
   [SerializeField]
   int settleFrames = 2;
   ```
2. `Start` thành `IEnumerator Start()`:
   ```csharp
   if (Locator.UI == null)
   {
       Debug.LogWarning(...); // giữ nguyên
       DismissLoading();
       yield break;
   }
   SetInputEnabled(false);
   Locator.UI.GetUI<HomeCanvas>();          // preload inactive, hitch bị loading che
   for (int i = 0; i < settleFrames; i++) yield return null;

   // Cùng 1 frame: mở Home (Bg full màn che scene) rồi tắt loading
   homeCanvas = Locator.UI.OpenUI<HomeCanvas>();
   DismissLoading();
   if (homeCanvas == null)
   {
       SetInputEnabled(true);
       yield break;
   }
   homeCanvas._OnClose += OnHomeClosed;
   ```
3. Thêm helper:
   ```csharp
   static void DismissLoading()
   {
       if (InitLoadingCanvas.Current != null) InitLoadingCanvas.Current.Dismiss();
   }
   ```
4. `OnDestroy`, `OnHomeClosed`, `SetInputEnabled`: giữ nguyên.

## Bước 3 — Kiểm tra

Tự động:
- `read_console` sau khi Unity compile: 0 error.
- Chạy EditMode tests hiện có (`Assets/Tests/EditMode`) → không regression. (Thay đổi là timing frame/MonoBehaviour, không có logic thuần để thêm EditMode test có ý nghĩa.)

Thủ công trong Editor (bắt đầu Play từ `CryptoLoader`, hoặc `LoadStart`):
1. Loading chạy 0 → 100%, giữ 100% rất ngắn rồi chuyển thẳng sang Home — **không thấy GameScene/bàn chơi giữa chừng** (có thể soi bằng Frame Debugger / quay màn hình tốc độ chậm).
2. Thấy rõ `Logo` scale 0.6→1 và nút Start scale 0→1 (OutBack), HighScore đếm 0 → giá trị cuối.
3. Trước khi nhấn Start không kéo được khối; nhấn Start → Home đóng, `GameplayCanvas` hiện, kéo khối bình thường.
4. Play thẳng `GameScene` (không có loading): Home vẫn mở có anim, không lỗi.
5. Tạm tắt `GameSceneFlow` trong GameScene → loading tự tắt sau ~3s (safety timeout), không kẹt.
6. Hierarchy sau handoff: không còn `InitLoadingCanvas` dưới UIManager.
