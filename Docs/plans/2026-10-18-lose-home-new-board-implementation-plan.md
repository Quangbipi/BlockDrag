# Lose → Home → Start: tạo bàn mới — Implementation Plan

Design: [2026-10-18-lose-home-new-board-design.md](../designs/2026-10-18-lose-home-new-board-design.md)

## File bị ảnh hưởng

| File | Assembly | Thay đổi |
|------|----------|----------|
| `Assets/_Game/_UI/Scripts/GameSceneFlow.cs` | `BlockDrag.UI` | Thêm xử lý `HomeCanvas._OnOpen` |

Không đổi prefab, scene, package, `.asmdef`, hay contract `IBoardSessionService`.

## Các bước

1. **Đăng ký `_OnOpen`** trong `GameSceneFlow.Start()` ngay cạnh chỗ đăng ký `_OnClose`:
   ```csharp
   homeCanvas._OnOpen += OnHomeOpened;
   homeCanvas._OnClose += OnHomeClosed;
   ```
   Lần mở Home đầu tiên đã xảy ra trước khi đăng ký → không gọi lại (input đã bị khoá sẵn, board mới).

2. **Thêm `OnHomeOpened`**:
   ```csharp
   /// <summary>
   /// Home mở lại (vd. từ LoseCanvas): khoá kéo khối, ván đã thua thì reset bàn mới khi Bg Home còn che board
   /// </summary>
   void OnHomeOpened(UISCanvas canvas)
   {
       SetInputEnabled(false);
       IBoardSessionService session = Locator.BoardSession;
       if (session != null && session.IsGameOver)
       {
           session.RestartGame();
       }
   }
   ```

3. **Huỷ đăng ký** trong `OnDestroy()`:
   ```csharp
   homeCanvas._OnOpen -= OnHomeOpened;
   homeCanvas._OnClose -= OnHomeClosed;
   ```

4. `OnHomeClosed`, `LoseCanvas`, `HomeCanvas` giữ nguyên.

## Kiểm tra

1. Compile: `read_console` không có lỗi sau khi Unity refresh.
2. EditMode tests chạy pass (không có test mới vì logic nằm ở MonoBehaviour điều phối UI; `RestartGame` đã được dùng bởi Try Again).
3. Thủ công trong Editor (chạy từ `LoadStart`):
   - Start → chơi tới khi thua → LoseCanvas → bấm **Home**.
   - Trong lúc Home mở: không kéo được khối; HighScore hiển thị đúng.
   - Bấm **Start** → board trống, khay 3 khối mới, điểm = 0, nền màu mặc định, kéo/đặt khối bình thường.
   - Thua lần nữa → **Try Again** vẫn reset như cũ.
   - Lần vào game đầu tiên (chưa thua) → Home → Start: hành vi không đổi.
