# ComboTextEffect: ẩn điểm cộng khi PERFECT & căn VFX giữa số combo — Implementation Plan

Design: [2026-10-16-combo-text-perfect-and-vfx-center-design.md](../designs/2026-10-16-combo-text-perfect-and-vfx-center-design.md)

## File bị ảnh hưởng

| File | Assembly | Thay đổi |
|---|---|---|
| `Assets/_Game/_UI/Scripts/ComboTextEffect.cs` | `BlockDrag.UI` | Field `numberVfx`, ẩn/hiện số + VFX, căn VFX, căn label |
| `Assets/_Game/_UI/Scripts/ComboEffectManager.cs` | `BlockDrag.UI` | PERFECT gọi `Play` với value rỗng |
| `Assets/_Game/Resources/Prefabs/ComboTextEffect.prefab` | — | Gán `numberVfx` = `Flash_star_ellow_green` |
| `Assets/Tests/EditMode/ComboEffectTests.cs` | `EditModeTests` | Sửa test PERFECT, thêm test mới |

Không có file mới, không đổi `.asmdef`, không có migration dữ liệu serialized (chỉ thêm 1 field, giá trị mặc định null).

## Các bước

### Bước 1 — `ComboTextEffect.cs`

1. Thêm `using UnityEngine.UI;` (cho `LayoutRebuilder`).
2. Thêm field trong nhóm `UI / Text References`:
   ```csharp
   [Tooltip("VFX luôn căn giữa text số combo; bị ẩn khi không có số (PERFECT)")]
   [SerializeField] private Transform numberVfx;
   ```
   cùng property `public Transform NumberVfx => numberVfx;` để test kiểm tra.
3. Thêm field private `HorizontalAlignmentOptions labelDefaultAlignment` + `bool labelAlignmentCached`; cache alignment gốc của `comboLabelText` trong `EnsureReferences()` (chỉ lần đầu).
4. `InitReferences(...)`: thêm tham số cuối tùy chọn `Transform vfx = null` → gán `numberVfx` (không làm vỡ chỗ gọi cũ trong `ComboEffectManager` và test).
5. Trong `Play(string label, string value, ...)`, sau khối cập nhật text:
   - `bool hasValue = !string.IsNullOrEmpty(value);`
   - `numberContainer.gameObject.SetActive(hasValue)` (nếu `numberContainer` khác null).
   - `comboLabelText.horizontalAlignment = hasValue ? labelDefaultAlignment : HorizontalAlignmentOptions.Center;`
   - Gọi `AlignVfxToNumber(hasValue)`.
6. Phương thức mới `private void AlignVfxToNumber(bool hasValue)`:
   - `numberVfx == null` → return.
   - `numberVfx.gameObject.SetActive(hasValue)`; nếu `!hasValue` → return.
   - `comboNumberText.ForceMeshUpdate();` + `LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform)` (nếu có `rectTransform`).
   - `float centerX = numberContainer.localPosition.x + comboNumberText.textBounds.center.x;`
   - `numberVfx.localPosition = new Vector3(centerX, numberVfx.localPosition.y, numberVfx.localPosition.z);`
   - Lưu ý: gọi **trước** khi set `numberContainer.localScale = 0`, và chỉ dùng localPosition (giả định VFX là con trực tiếp của root như prefab hiện tại).
7. Trong sequence: bước "số nảy" chỉ `Append` khi `hasValue`.
8. Cập nhật XML doc của overload `Play(label, value)`: value rỗng → ẩn số và VFX.

### Bước 2 — `ComboEffectManager.cs`

- `HandlePerfectClear`: `effect.Play("PERFECT", string.Empty, spawnPosition);` (giữ chữ ký `int bonus`, không đổi event).

### Bước 3 — Prefab `ComboTextEffect.prefab`

- Qua Unity MCP (`manage_components` / `manage_prefabs`) hoặc sửa YAML: set `numberVfx: {fileID: 2379834537969980639}` (Transform stripped của `Flash_star_ellow_green`) trên component `ComboTextEffect`.
- Giữ nguyên thay đổi chưa commit hiện có của bạn (VFX instance, scale, y = -19). Giá trị X = 110 vẫn để nguyên, runtime sẽ ghi đè.

### Bước 4 — Tests `ComboEffectTests.cs`

- SetUp: tạo thêm `GameObject("Vfx")` con của prefab, truyền vào `InitReferences(..., vfxGo.transform)`.
- Sửa `ComboTextEffect_PlayWithLabelAndValue_SetsCustomTexts` thành dùng value `"+900"` với label tùy ý — giữ ý nghĩa "custom text" (number vẫn hiện).
- Thêm:
  - `Play_WithEmptyValue_HidesNumberAndVfx` — `Play("PERFECT", "", ...)` → `ComboNumberText.gameObject.activeSelf == false`, `NumberVfx.gameObject.activeSelf == false`.
  - `Play_ComboAfterPerfect_ShowsNumberAndVfxAgain` — cùng instance: PERFECT rồi `Play(12, ...)` → cả hai active, text `x12`.
  - `HandlePerfectClear_DoesNotShowBonus` — gọi `effectManager.HandlePerfectClear(900, ...)`, instance active có label `PERFECT` và số bị ẩn.

## Kiểm tra

1. `read_console` sau khi Unity compile — không có lỗi.
2. Chạy EditMode tests (`run_tests` qua Unity MCP, hoặc lệnh batchmode trong AGENTS.md) — `ComboEffectTests` pass toàn bộ.
3. Thủ công trong Editor (bạn kiểm): chơi tới combo x2, x9, x10, x25 (+ x100 nếu được) → sao nằm giữa số; Perfect clear → chỉ chữ `PERFECT`, không có số, không có sao, chữ nằm giữa.

## Rủi ro

- `textBounds` đọc khi Canvas chưa layout có thể sai frame đầu → đã xử lý bằng `ForceMeshUpdate` + `ForceRebuildLayoutImmediate`.
- Nếu sau này bạn kéo VFX vào làm con của object khác (không phải root), công thức localPosition cần đổi — có ghi chú trong code.
