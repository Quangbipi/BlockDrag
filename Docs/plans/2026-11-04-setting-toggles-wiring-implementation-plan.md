# Nối Setting: Music / Sound / Vibration — Implementation Plan

Design: [2026-11-04-setting-toggles-wiring-design.md](../designs/2026-11-04-setting-toggles-wiring-design.md) (đã duyệt, **giữ** rung nhẹ khi bật lại Vibration)

## File bị ảnh hưởng

| File | Assembly | Thay đổi |
|---|---|---|
| `Assets/_Game/_BlockDrag/Effects/Haptics.cs` (+ `.meta`) | `BlockDrag` | **Mới** — cổng rung tập trung |
| `Assets/_Game/_BlockDrag/Effects/LineClearImpactEffect.cs` | `BlockDrag` | Dùng `Haptics.Play`, bỏ `IsHapticOff()` riêng |
| `Assets/_Game/_UI/Scripts/PerfectCanvas.cs` | `BlockDrag.UI` | 4 lệnh rung → `Haptics.Play` |
| `Assets/_Game/_UI/Scripts/Anim/UI_ANIM.cs` | `BlockDrag.UI` | 1 lệnh rung → `Haptics.Play` |
| `Assets/_Game/_UI/Scripts/SettingPopup.cs` | `BlockDrag.UI` | Lưu setting, rung nhẹ khi bật Vibration |
| `Assets/Tests/EditMode/HapticsTests.cs` (+ `.meta`) | `EditModeTests` | **Mới** — test logic tắt/bật |
| `.agents/rules/architecture.md` | — | Cập nhật dòng mô tả haptic |

Không đổi prefab, scene, package, `.asmdef`.

## Các bước

### Bước 1 — Tạo `Haptics`

`Assets/_Game/_BlockDrag/Effects/Haptics.cs`, namespace `Gameplay.BlockDrag`:

```csharp
public static class Haptics
{
    /// Người chơi đã tắt rung trong Setting (thiếu data thì coi như đang bật)
    public static bool IsOff => IsOffIn(Locator.Data?.GetData<GameData>());

    public static bool IsOffIn(GameData data) => data?.setting != null && data.setting.hapticOff;

    public static void Play(MOST_HapticFeedback.HapticTypes type, float cooldown)
    {
        if (IsOff) return;
        MOST_HapticFeedback.GenerateWithCooldown(type, cooldown);
    }
}
```

`IsOffIn` tách riêng để test thuần, không cần dựng `Locator.Data`.

### Bước 2 — `LineClearImpactEffect`

- `PlayHaptic`: thay 2 dòng thân bằng `Haptics.Play(GetHapticType(lineCount, heavyHapticLineCount), hapticCooldown);`
- Xoá method `IsHapticOff()`; bỏ `using` thừa nếu có.
- Giữ nguyên `GetHapticType` (đang có test).

### Bước 3 — `PerfectCanvas` & `UI_ANIM`

Thay `MOST_HapticFeedback.GenerateWithCooldown(X, c)` → `Haptics.Play(X, c)` (giữ nguyên type và cooldown). `UI_ANIM` thêm `using Gameplay.BlockDrag;`. Vẫn giữ `using Solo.MOST_IN_ONE;` cho enum `HapticTypes`.

### Bước 4 — `SettingPopup`

Trong `OnFunctionButtonClick`:

```csharp
case 2:
    settingData.hapticOff = !settingData.hapticOff;
    if (!settingData.hapticOff)
        Haptics.Play(MOST_HapticFeedback.HapticTypes.LightImpact, 0.1f);
    break;
```

Sau `switch`, trước `UpdateUI()`: `Locator.Data?.Save();`

Thêm `using Gameplay.BlockDrag;` và `using Solo.MOST_IN_ONE;`.

### Bước 5 — Test EditMode `HapticsTests`

- `IsOffIn_WhenHapticOffTrue_ReturnsTrue`
- `IsOffIn_WhenHapticOffFalse_ReturnsFalse`
- `IsOffIn_WhenDataNull_ReturnsFalse`

(`EditModeTests.asmdef` đã tham chiếu `BlockDrag` và `Quang.Base` qua `LineClearImpactEffectTests`; nếu thiếu thì báo lại trước khi sửa asmdef.)

### Bước 6 — Cập nhật `architecture.md`

Dòng vendor haptic: ghi rõ mọi lệnh rung đi qua `Gameplay.BlockDrag.Haptics` (tôn trọng `GameData.setting.hapticOff`).

## Kiểm chứng

1. **Biên dịch**: `read_console` (UnityMCP) không có lỗi sau khi refresh.
2. **Test**: chạy EditMode — `HapticsTests` + `LineClearImpactEffectTests` + `SettingPopupTests` (nếu có) đều pass.
3. **Grep**: `grep -rn "MOST_HapticFeedback.Generate" Assets/_Game` chỉ còn trong `Haptics.cs`.
4. **Editor (thủ công, bạn kiểm hoặc mình kiểm qua MCP)**:
   - Play từ `LoadStart` → mở Setting ở Home: tắt Music → BGM im; bật lại → BGM nghe tiếp.
   - Tắt Sound → tiếng click / đặt block / phá hàng im; bật lại → có tiếng.
   - Thoát Play Mode, Play lại → 3 switch giữ đúng trạng thái, audio áp dụng đúng.
5. **Thiết bị** (bạn kiểm): tắt Vibration → phá hàng / Perfect không rung; bật lại → rung nhẹ một nhát ngay lúc bấm.

## Commit

Một commit: `Setting: persist toggles and gate all haptics on hapticOff` (kèm `.meta` mới).
