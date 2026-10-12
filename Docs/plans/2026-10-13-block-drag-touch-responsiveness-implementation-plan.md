# Implementation Plan: Cải Thiện Độ Nhạy Chạm & Độ Mượt Khi Kéo Khối

> **Ngày tạo:** 2026-10-13  
> **Thiết kế:** [`Docs/designs/2026-10-13-block-drag-touch-responsiveness-design.md`](../designs/2026-10-13-block-drag-touch-responsiveness-design.md)  
> **Assembly:** `BlockDrag` (không đụng `BlockDrag.UI`, không sửa package `Quang.*`)

---

## 0. Quyết định chốt từ thiết kế

- **Đặt `targetFrameRate` ở đâu:** các package `Quang.*` không có bootstrap nào set frame rate. Vì vậy set trong `BlockDragInput.Awake` qua field `[SerializeField] int targetFrameRate = 60`. Nếu giá trị ≤ 0 thì giữ nguyên mặc định của Unity.
- **Thêm `BlockDragInput` vào scene:** gắn component lên GameObject đang chứa `BlockSpawner` trong `GameScene.unity`. Đây là thay đổi scene, cần nêu rõ trong PR.
- **Cách tính vùng chạm:** `BlockSpawner` tính kích thước world của vùng chạm. `BlockShape` quy đổi sang local bằng hàm static thuần để viết được unit test.

---

## 1. Các bước thực hiện

### Bước 1: `BlockShape`: vùng chạm & bật/tắt collider
File: [`Assets/_Game/_BlockDrag/BlockShape.cs`](../../Assets/_Game/_BlockDrag/BlockShape.cs)

1. Lưu `shapeSize`, tức kích thước khít của khối (local), lúc chạy `UpdateColliderBounds`.
2. Thêm hàm static thuần:
   ```csharp
   public static Vector2 CalculateTouchColliderSize(Vector2 shapeLocalSize, Vector2 touchWorldSize, float containerScale)
   // = max(shapeLocalSize, touchWorldSize / containerScale) theo từng trục; containerScale <= 0 → trả shapeLocalSize
   ```
3. Thêm `public void SetTouchArea(Vector2 worldSize, float containerScale)`. Hàm này đặt `boxCollider.size` theo kết quả hàm trên và đặt `offset = Vector2.zero`, để vùng chạm luôn nằm giữa slot.
4. Thêm `public void SetInputEnabled(bool enabled)`. Collider chỉ được bật khi `enabled && !IsDimmed`. Sửa `SetDimmed` để tôn trọng cờ này, tránh trường hợp bỏ dim thì bật lại collider của khối đang kéo.

### Bước 2: `BlockSpawner`: truyền vùng chạm cho khối mới
File: [`Assets/_Game/_BlockDrag/BlockSpawner.cs`](../../Assets/_Game/_BlockDrag/BlockSpawner.cs)

1. Thêm field `[SerializeField, Range(0.05f, 0.4f)] float touchAreaHeightPercent = 0.16f` (tính theo chiều cao màn hình).
2. Trong `CreateSpawnSlots`, lưu `touchAreaWorldSize`:
   - rộng = khoảng cách world giữa 2 slot liền kề = `worldWidth * (1 - 2*sideMarginPercent) / (SlotCount - 1)`;
   - cao = `worldHeight * touchAreaHeightPercent`.
3. Trong `SpawnRandomShapeAt`, sau khi set `localScale`, gọi `shapeComp.SetTouchArea(touchAreaWorldSize, spawnScaleRatio)`.

### Bước 3: `BlockDragHandler`: API drag mới & nhấc mượt
File: [`Assets/_Game/_BlockDrag/BlockDragHandler.cs`](../../Assets/_Game/_BlockDrag/BlockDragHandler.cs)

1. Xoá `OnMouseDown / OnMouseDrag / OnMouseUp`. Thay bằng các hàm public:
   - `bool CanBeginDrag` → `!isDragging && blockShape != null && !blockShape.IsDimmed` (**không** còn chặn khi `isReturning`).
   - `BeginDrag(Vector2 screenPos)`: kill `moveTween/scaleTween/liftTween`; `isReturning = false`; `blockShape.SetInputEnabled(false)`; nâng sorting; tween scale về `dragScale` với `Ease.OutQuad`; tween `currentLift` từ 0 → `LiftWorld` trong `pickUpDuration` (`DOTween.To`, `Ease.OutQuad`); cập nhật vị trí; gọi `UpdateDragIndicator`; bắn `OnBeginDragEvent`.
   - `Drag(Vector2 screenPos)`: lưu `lastScreenPos`, cập nhật vị trí, gọi indicator, bắn `OnDraggingEvent`.
   - `EndDrag()`: giữ logic thả cũ (`TryPlaceShape` hoặc `ReturnToOrigin`).
   - `CancelDrag()`: `isDragging = false`, `HideIndicator`, bắn `OnEndDragEvent`, rồi `ReturnToOrigin()`.
2. **Bỏ `originalPosition = transform.position` trong `BeginDrag`.** Gốc đã được `SetOrigin` set lúc spawn. Nếu nhấc lại giữa lúc đang bay về mà gán lại thì gốc sẽ thành vị trí lơ lửng. `originalScale` cũng giữ theo `SetOrigin`.
3. Chỗ nâng khối:
   - Thay field `dragOffset` bằng `[SerializeField] float dragLiftCells = 2f`. Có thêm `[FormerlySerializedAs]` hoặc không đều được, vì kiểu dữ liệu khác nên giá trị cũ không chuyển sang được; giá trị trên prefab sẽ quay về mặc định.
   - `LiftWorld = dragLiftCells * (blockGrid != null ? blockGrid.CellSize : 1f)`.
   - Giữ property `DragOffset` trả về `new Vector3(0, LiftWorld, 0)` (hiện chưa có code bên ngoài dùng, giữ để tương thích).
4. `UpdateDragPosition(Vector2 screenPos)` nhận vị trí màn hình thay cho `Input.mousePosition`: `position = fingerWorld + (0, currentLift)`, z = `originalPosition.z`. Mỗi frame dùng `lastScreenPos` để khối vẫn nâng dần lên kể cả khi ngón tay đứng yên. Việc này do `BlockDragInput` gọi `Drag` ở cả pha `Stationary`.
5. `ReturnToOrigin`: trong `OnComplete` gọi thêm `blockShape.SetInputEnabled(true)`. `OnDestroy` kill thêm `liftTween`.

### Bước 4: `BlockDragInput` (mới)
File mới: `Assets/_Game/_BlockDrag/BlockDragInput.cs` (+ `.meta`, do Unity tạo)

```csharp
namespace Gameplay.BlockDrag
{
    public class BlockDragInput : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private int targetFrameRate = 60;
        private BlockDragHandler activeHandler;
        private int activeFingerId = -1;   // -1: chuột
        // Awake: set Application.targetFrameRate; Input.multiTouchEnabled giữ mặc định
        // Update: touchCount > 0 ? HandleTouches() : HandleMouse()
        // TryBegin(screenPos): Physics2D.OverlapPoint(world) → GetComponent<BlockDragHandler>() → CanBeginDrag → BeginDrag
        // OnDisable: nếu đang giữ khối → CancelDrag()
    }
}
```

- `HandleTouches`: duyệt `Input.GetTouch(i)`.
  - `Began`: nếu chưa giữ khối nào thì gọi `TryBegin` và ghi nhớ `fingerId`.
  - Chỉ xử lý các pha `Moved/Stationary/Ended/Canceled` khi `fingerId == activeFingerId`.
- `HandleMouse` dùng cho Editor/Standalone: `GetMouseButtonDown(0)` → `TryBegin`; `GetMouseButton(0)` → `Drag`; `GetMouseButtonUp(0)` → `EndDrag`. Chỉ chạy khi `Input.touchCount == 0`, để trên mobile không bị xử lý 2 lần (`simulateMouseWithTouches`).
- Nếu `activeHandler` bị destroy giữa chừng (ví dụ restart game) thì reset trạng thái.
- Chạm qua UI: giữ đúng hành vi hiện tại của `OnMouse*` (không chặn theo EventSystem). Hiện không có UI nào nằm đè lên khu vực slot lúc đang chơi.

### Bước 5: Gắn vào scene
- Mở `Assets/_Game/_Scenes/Game/GameScene.unity` và thêm `BlockDragInput` vào GameObject có `BlockSpawner`. Gán `targetCamera` giống cấu hình của `BlockSpawner` (để trống thì dùng `Camera.main`).
- Kiểm tra prefab `BlockShape.prefab`: field `dragLiftCells` lấy mặc định 2.

### Bước 6: Test EditMode
File mới: `Assets/Tests/EditMode/BlockDragTouchAreaTests.cs`
- `CalculateTouchColliderSize_WhenTouchAreaLarger_UsesTouchAreaDividedByScale`
- `CalculateTouchColliderSize_WhenShapeLarger_KeepsShapeSize` (dùng cho khối I_5 nằm ngang)
- `CalculateTouchColliderSize_WhenScaleNotPositive_ReturnsShapeSize`
- `SetInputEnabled_WhenDimmed_KeepsColliderDisabled`, `SetDimmed_False_WhileInputDisabled_KeepsColliderDisabled`

Luồng drag và tween (DOTween, input) sẽ kiểm tra thủ công ở Bước 7.

---

## 2. Danh sách file bị ảnh hưởng

| File | Loại |
|---|---|
| `Assets/_Game/_BlockDrag/BlockShape.cs` | Sửa |
| `Assets/_Game/_BlockDrag/BlockSpawner.cs` | Sửa |
| `Assets/_Game/_BlockDrag/BlockDragHandler.cs` | Sửa (xoá `OnMouse*`, đổi `dragOffset` → `dragLiftCells`) |
| `Assets/_Game/_BlockDrag/BlockDragInput.cs` (+ .meta) | Mới |
| `Assets/_Game/_Scenes/Game/GameScene.unity` | Sửa scene: thêm component |
| `Assets/Tests/EditMode/BlockDragTouchAreaTests.cs` (+ .meta) | Mới |

**Migration serialized data:** field `dragOffset` (Vector3) bị bỏ, thay bằng `dragLiftCells` (float, mặc định 2) trên `BlockShape.prefab`.

---

## 3. Kiểm tra

**Tự động**
- Chạy EditMode tests (qua UnityMCP `run_tests` hoặc batchmode). Tất cả test cũ và mới phải pass.
- Không có lỗi compile hay warning mới trong console.

**Thủ công trong Editor (Game view: iPhone 9:19.5 và iPad 3:4)**
1. Bật Gizmos để xem collider của 3 khối: phủ gần hết ô slot, không chồng lên nhau, không lấn lên grid.
2. Chạm vào khoảng trống cạnh khối `Single` vẫn nhấc được khối.
3. Thả khối ra ngoài grid rồi bấm lại ngay khi nó đang bay về: khối được nhấc lại, và lần thả sau khối bay về đúng slot (gốc không bị đổi).
4. Lúc nhấc khối: khối nâng dần lên khoảng 2 ô, không nhảy tức thời, scale không bị vọt quá kích thước.
5. Khối bị dim (không còn chỗ đặt) thì không nhấc được.
6. Đặt khối, phá hàng, restart, game over: hành vi không đổi.

**Trên thiết bị thật (iOS + Android)**
1. Xác nhận chạy 60 FPS (dùng Ingame Debug Console hoặc Profiler).
2. Kéo nhanh và kéo chậm: khối bám ngón tay mượt.
3. Kéo một khối rồi chạm thêm ngón thứ hai: khối vẫn chỉ theo ngón đầu.
4. Đang kéo thì kéo thanh notification xuống hoặc chuyển app: khối bay về slot, không bị kẹt.
