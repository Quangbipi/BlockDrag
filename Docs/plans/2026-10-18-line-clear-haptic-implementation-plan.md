# Rung (haptic) khi phá hàng/cột — Implementation Plan

Design đã duyệt: [2026-10-18-line-clear-haptic-design.md](../designs/2026-10-18-line-clear-haptic-design.md)
Chốt: 1 đường = `MediumImpact`, ≥2 đường = `HeavyImpact`; tôn trọng `GameData.setting.hapticOff`.

## Files ảnh hưởng

| File | Thay đổi |
|------|----------|
| `Assets/_Game/_BlockDrag/BlockDrag.asmdef` | Thêm reference `GUID:609b4fad904294b92a3463bf72b4762d` (`MOST_HapticFeedback`) |
| `Assets/_Game/_BlockDrag/Effects/LineClearImpactEffect.cs` | Thêm toggle + config haptic, gọi rung trong `PlayImpact`, hàm static `GetHapticType` |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Thêm reference `MOST_HapticFeedback` (test so sánh enum `HapticTypes`) |
| `Assets/Tests/EditMode/LineClearImpactEffectTests.cs` | Thêm test cho `GetHapticType` |
| `.agents/rules/architecture.md` | Ghi chú `BlockDrag` → `MOST_HapticFeedback` (plugin bên thứ ba, tầng Foundation) |

Không đụng prefab/scene: field mới có giá trị mặc định (`enableHaptic = true`), component đã có sẵn trong scene sẽ tự nhận default.

## Các bước

### 1. `BlockDrag.asmdef`
Thêm `"GUID:609b4fad904294b92a3463bf72b4762d"` vào `references`.

### 2. `LineClearImpactEffect.cs`
- `using Quang.Base;` và `using Solo.MOST_IN_ONE;`.
- Header `Toggle`: thêm `[SerializeField] private bool enableHaptic = true;`.
- Header mới `Haptic`:
  ```csharp
  [Tooltip("Số đường phá cùng lúc từ mức này trở lên dùng rung mạnh (HeavyImpact)")]
  [SerializeField] private int heavyHapticLineCount = 2;
  [Tooltip("Khoảng nghỉ tối thiểu giữa hai lần rung (giây)")]
  [SerializeField] private float hapticCooldown = 0.1f;
  ```
- Trong `PlayImpact(lineCount)`, sau glow:
  ```csharp
  if (enableHaptic)
  {
      PlayHaptic(lineCount);
  }
  ```
- Hàm private `PlayHaptic(int lineCount)`: return nếu `IsHapticOff()`; ngược lại `MOST_HapticFeedback.GenerateWithCooldown(GetHapticType(lineCount, heavyHapticLineCount), hapticCooldown)`.
- Hàm private static `IsHapticOff()`: `Locator.Data?.GetData<GameData>()?.setting?.hapticOff ?? false` (theo mẫu null-safe của `GameDataHighScoreStore`; nếu `setting` là struct thì bỏ `?.` tương ứng).
- Hàm public static `GetHapticType(int lineCount, int heavyLineCount)`: `lineCount >= heavyLineCount ? HeavyImpact : MediumImpact`, đặt cạnh `GetFadeDuration`.

### 3. `EditModeTests.asmdef`
Thêm `"GUID:609b4fad904294b92a3463bf72b4762d"` vào `references`.

### 4. `LineClearImpactEffectTests.cs`
Thêm:
- `GetHapticType_WhenSingleLine_ReturnsMediumImpact`
- `GetHapticType_WhenLinesReachHeavyThreshold_ReturnsHeavyImpact` (2 và 4 đường)

### 5. `architecture.md`
Trong mục Module Map/Layer Model, thêm dòng: `BlockDrag` (và `BlockDrag.UI`) được tham chiếu `MOST_HapticFeedback` (vendor, không phụ thuộc gì).

## Kiểm chứng

1. Unity refresh → `read_console` không có lỗi compile.
2. Chạy EditMode tests (`LineClearImpactEffectTests` + toàn bộ suite) — tất cả pass.
3. Editor: mở `GameScene`, kiểm tra `LineClearImpactEffect` hiện field `Enable Haptic`, `Heavy Haptic Line Count`, `Haptic Cooldown` với giá trị mặc định; Play, phá hàng → không lỗi console (Editor không rung thật).
4. Thiết bị thật (bạn tự kiểm): phá 1 đường rung vừa, 2+ đường rung mạnh; bật Haptic Off trong Setting → phá hàng không rung.
