# Kế Hoạch Triển Khai: Đổi Màu Background Khi Dọn Sạch Bàn

> **Thiết kế:** [`2026-10-14-perfect-clear-bg-color-design.md`](../designs/2026-10-14-perfect-clear-bg-color-design.md)  
> **Ngày tạo:** 2026-10-14

## Files bị ảnh hưởng

| File | Module | Thay đổi |
|---|---|---|
| `Assets/_Game/_UI/Scripts/GameplayCanvas.cs` | `BlockDrag.UI` | Thêm bảng màu, đăng ký `OnPerfectClear` và `OnGameRestarted`, tween `Camera.main.backgroundColor` |
| `Assets/_Game/Resources/UI/GameplayCanvas.prefab` | Prefab | Tắt object `BG`, gán `_bgColors` và `_bgFadeDuration` |

Không có thay đổi về asmdef, package hay scene.

## Các bước

1. **GameplayCanvas.cs**
   - Thêm `[SerializeField] Color[] _bgColors` và `float _bgFadeDuration = 0.5f`.
   - Thêm state `bgColorIndex` và `bgColorTween`.
   - Thêm `HandlePerfectClear(int, Vector3)`: tăng index vòng tròn rồi chạy `Camera.main.DOColor(...)`.
   - Thêm `ResetBgColor()`: kill tween, đặt index = 0 và gán thẳng `Camera.main.backgroundColor = _bgColors[0]`.
   - Đăng ký và gỡ trong `BindScore`/`UnbindScore` (`OnPerfectClear`) và `BindSession`/`UnbindSession` (`OnGameRestarted`).
   - Gọi `ResetBgColor()` trong `Open()`. Kill `bgColorTween` trong `OnDestroy()`.
2. **GameplayCanvas.prefab**
   - `BG`: đặt `m_IsActive: 0` để không che board.
   - Gán `_bgColors` = 5 màu (màu đầu là màu nâu cũ của BG) và `_bgFadeDuration = 0.5`.

## Kiểm tra (Unity Editor)
1. Mở `GameScene` và Play. Nền có màu `_bgColors[0]`, board và HUD hiển thị đầy đủ.
2. Dọn sạch bàn: nền chuyển mượt sang màu kế tiếp. Dọn sạch nhiều lần thì màu xoay vòng.
3. Thua rồi Restart: nền quay về màu đầu.
4. Console không có lỗi hoặc warning về DOTween hay null reference.

Không thêm EditMode test: logic chỉ là nối event với tween trong MonoBehaviour UI, nên kiểm tra thủ công trong Editor.
