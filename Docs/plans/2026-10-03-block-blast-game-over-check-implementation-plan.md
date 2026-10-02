# Kế Hoạch Triển Khai: Chức Năng Kiểm Tra Thua (Game Over Check) & Khối Không Đặt Được Kiểu Block Blast

> **Ngày tạo:** 2026-10-03  
> **Tài liệu thiết kế:** [`Docs/designs/2026-10-03-block-blast-game-over-check-design.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-10-03-block-blast-game-over-check-design.md)  
> **Tài liệu tham chiếu:** [`.agents/rules/task_planning.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/.agents/rules/task_planning.md) & [`.agents/rules/architecture.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/.agents/rules/architecture.md)  

---

## 1. Danh Sách File Ảnh Hưởng (Impacted Files & Boundaries)

| File / Module | Loại thay đổi | Trách nhiệm |
|---|---|---|
| [`Assets/_Game/_BlockDrag/BlockDrag.asmdef`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockDrag.asmdef) | Chỉnh sửa | Bổ sung references `"Unity.TextMeshPro"` và `"UnityEngine.UI"` để phục vụ UI Game Over. |
| [`Assets/_Game/_BlockDrag/BlockGrid.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs) | Chỉnh sửa | Bổ sung hàm `ResetBoard()` để dọn sạch bàn cờ khi chơi lại; tối ưu hàm `CanShapeBePlacedAnywhere`. |
| [`Assets/_Game/_BlockDrag/BlockShape.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockShape.cs) | Chỉnh sửa | Bổ sung cơ chế làm mờ/làm sáng (`SetDimmed`), lưu trạng thái `IsDimmed`, bật/tắt collider khi bị vô hiệu hóa. |
| [`Assets/_Game/_BlockDrag/BlockSpawner.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockSpawner.cs) | Chỉnh sửa | Quét các khối còn lại sau mỗi nước đi, cập nhật dimming, phát hiện thua, kích hoạt sự kiện `OnGameOver`, cung cấp hàm `RestartGame()`. |
| [`Assets/_Game/_BlockDrag/GameOverUI.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/GameOverUI.cs) | Tạo mới | Điều khiển hiển thị Game Over popup, hiệu ứng Fade/Scale DOTween, nút "Chơi lại", hỗ trợ tự động tạo giao diện runtime độc lập. |

---

## 2. Kế Hoạch Từng Bước Cụ Thể (Step-by-Step Implementation)

### Bước 1: Bổ sung references cho `BlockDrag.asmdef`
- Mở [`BlockDrag.asmdef`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockDrag.asmdef).
- Thêm `"Unity.TextMeshPro"` và `"UnityEngine.UI"` vào mảng `references`.
- Đảm bảo assembly `BlockDrag` có thể sử dụng các component UI mà không gặp lỗi biên dịch.

### Bước 2: Nâng cấp `BlockGrid.cs`
- Thêm phương thức:
  ```csharp
  /// <summary>
  /// Xóa sạch các khối gạch đang chiếm chỗ trên bàn cờ khi bắt đầu ván mới (Replay)
  /// </summary>
  public void ResetBoard();
  ```
- Duyệt qua mảng `occupiedBlocks[r, c]`: nếu ô nào khác `null`, phá hủy GameObject ô gạch và gán về `null`.
- Gọi `HideIndicator()` để dọn dẹp các ghost preview cell nếu có.

### Bước 3: Nâng cấp `BlockShape.cs`
- Thêm biến và thuộc tính:
  ```csharp
  public bool IsDimmed { get; private set; }
  ```
- Thêm phương thức `SetDimmed(bool dimmed)`:
  - Nếu `dimmed == true`:
    - Đổi màu toàn bộ `cachedRenderers` thành màu tối mờ (`alpha = 0.45f`, màu `RGB` tối bằng 40% màu gốc).
    - Tắt `BoxCollider2D` để chuột/touch không thể tương tác nhấc khối lên.
  - Nếu `dimmed == false`:
    - Khôi phục màu gốc `shapeData.shapeColor` (`alpha = 1f`).
    - Bật lại `BoxCollider2D`.
- Trong hàm `Initialize()`: đảm bảo reset trạng thái `IsDimmed = false`.

### Bước 4: Nâng cấp `BlockSpawner.cs`
- Khai báo các sự kiện:
  ```csharp
  public event System.Action OnGameOver;
  public event System.Action OnGameRestarted;
  ```
- Thêm phương thức `GetRemainingShapes()`:
  - Lấy danh sách các `BlockShape` hiện đang còn nằm trong các slot con của `spawnSlots`.
- Thêm phương thức `CheckPlacementsAndGameOver()`:
  - Quét từng khối trong `GetRemainingShapes()`.
  - Với mỗi khối: kiểm tra `blockGrid.CanShapeBePlacedAnywhere(shape)`.
  - Gọi `shape.SetDimmed(!canPlace)`.
  - Đếm số khối có thể đặt (`placeableCount`).
  - Nếu `placeableCount == 0`: kích hoạt Coroutine đếm lùi `0.35f` giây rồi bắn `OnGameOver?.Invoke()`.
- Tích hợp gọi `CheckPlacementsAndGameOver()`:
  - Gọi sau khi `SpawnNewHand()` hoàn tất.
  - Gọi trong `HandleShapePlaced()` sau khi đã đặt khối và kiểm tra sinh lượt mới.
- Thêm phương thức `ClearHand()`:
  - Hủy toàn bộ GameObject con trong các `spawnSlots`.
- Thêm phương thức `RestartGame()`:
  - Gọi `blockGrid.ResetBoard()`.
  - Gọi `ClearHand()`.
  - Gọi `SpawnNewHand()`.
  - Bắn sự kiện `OnGameRestarted?.Invoke()`.

### Bước 5: Tạo mới `GameOverUI.cs`
- Tạo script tại [`Assets/_Game/_BlockDrag/GameOverUI.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/GameOverUI.cs).
- Các chức năng chính:
  - Đăng ký lắng nghe sự kiện `OnGameOver` và `OnGameRestarted` từ `BlockSpawner`.
  - Tạo CanvasGroup điều khiển `alpha` và `interactable`.
  - Nền tối mờ toàn màn hình (Dark Overlay).
  - Dialog Panel chứa tiêu đề "GAME OVER" và thông báo "Không còn nước đi nào!".
  - Nút "Chơi lại" (Play Again) với màu sắc nổi bật, khi bấm sẽ gọi `BlockSpawner.RestartGame()`.
  - Tự động sinh cấu trúc UI đẹp mắt runtime nếu trong scene chưa có sẵn prefab/canvas được gán, giúp kiểm thử ngay lập tức mà không cần thao tác thủ công trong Unity Editor.
  - Hiệu ứng xuất hiện mượt mà bằng DOTween (`DOFade` và `DOScale` với `Ease.OutBack`).

---

## 3. Tiêu Chí Xác Minh & Kiểm Thử (Verification Steps)

1. **Kiểm tra biên dịch:** Chạy build/test và kiểm tra không có lỗi cú pháp hoặc thiếu reference.
2. **Kiểm tra chức năng làm mờ khối không vừa (Dimming):**
   - Đặt các khối lên bàn cờ để lấp đầy phần lớn diện tích.
   - Quan sát các khối lớn trong slot: khối nào không có bất kỳ vị trí trống nào vừa vặn sẽ tự động tối màu và không thể click kéo được.
   - Khi đặt khối nhỏ khác làm nổ hàng $\rightarrow$ các khoảng trống mới mở ra $\rightarrow$ khối lớn tự động sáng trở lại và kéo thả được.
3. **Kiểm tra kích hoạt Game Over:**
   - Khi tất cả các khối còn lại trong khay không thể đặt được $\rightarrow$ sau 0.35s xuất hiện bảng Game Over.
4. **Kiểm tra chức năng Chơi lại (Replay):**
   - Nhấn nút "Chơi lại" $\rightarrow$ bảng Game Over ẩn đi, toàn bộ bàn cờ được reset về trạng thái trống ban đầu, 3 khối mới được tạo ra và có thể chơi tiếp tục.
