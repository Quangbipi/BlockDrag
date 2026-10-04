# Kế Hoạch Triển Khai: Hệ Thống Tính Điểm & Combo (Score & Combo System)

> **Ngày tạo:** 2026-10-04  
> **Tài liệu thiết kế:** [`Docs/designs/2026-10-04-block-blast-score-and-combo-system-design.md`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-10-04-block-blast-score-and-combo-system-design.md)  
> **Trạng thái:** Chờ xác nhận & Triển khai  

---

## 1. Danh Sách Tệp & Module Bị Tác Động (Impacted Files & Boundaries)

| Tệp / Asset | Hành Động | Module / Assembly | Trách Nhiệm |
| :--- | :--- | :--- | :--- |
| `Assets/_Game/_BlockDrag/Data/ScoreConfigSO.cs` | **Tạo mới** | `BlockDrag.asmdef` | ScriptableObject chứa cấu hình điểm ô, điểm dòng, thời gian combo, thời gian cộng thêm và hệ số nhân combo. |
| `Assets/_Game/_BlockDrag/Data/ScoreConfig.asset` | **Tạo mới** | Unity Asset | Asset dữ liệu ScriptableObject cấu hình mặc định (5s, 3s, 25 điểm). |
| `Assets/_Game/_BlockDrag/ScoreManager.cs` | **Tạo mới** | `BlockDrag.asmdef` | Quản lý logic tính điểm, đếm ngược thời gian combo, lưu HighScore và phát các sự kiện cập nhật điểm/combo. |
| `Assets/_Game/_BlockDrag/BlockGrid.cs` | **Chỉnh sửa** | `BlockDrag.asmdef` | Bổ sung sự kiện `OnShapePlacedWithCount` truyền số lượng SingleBlock khi đặt khối thành công. |
| `Assets/_Game/_BlockDrag/BlockSpawner.cs` | **Chỉnh sửa** | `BlockDrag.asmdef` | Tự động đảm bảo `ScoreManager` tồn tại trong Scene nếu chưa có (tương tự `GameOverUI`). |
| `Assets/_Game/_UI/Scripts/GameplayCanvas.cs` | **Chỉnh sửa** | `Hung.UI.asmdef` | Lắng nghe `ScoreManager` để cập nhật `ScoreTxt` và `HightScoreTxt` với hiệu ứng số sinh động. |
| `Assets/_Game/_BlockDrag/GameOverUI.cs` | **Chỉnh sửa** | `BlockDrag.asmdef` | Hiển thị điểm số đạt được và HighScore khi ván chơi kết thúc. |
| `Assets/Tests/EditMode/ScoreSystemTests.cs` | **Tạo mới** | `EditModeTests.asmdef` | Bộ kiểm thử EditMode tự động xác minh toàn bộ các ca tính điểm và combo logic. |

---

## 2. Kế Hoạch Triển Khai Từng Bước (Step-by-Step Implementation Steps)

### Bước 1: Tạo ScriptableObject `ScoreConfigSO` & Asset mặc định
1. Tạo class `ScoreConfigSO` kế thừa `ScriptableObject` tại `Assets/_Game/_BlockDrag/Data/ScoreConfigSO.cs`:
   - `pointsPerSingleBlock = 1`
   - `pointsPerLine = 25`
   - `initialComboDuration = 5f`
   - `comboBonusDuration = 3f`
   - `comboBonusMultiplier = 25`
   - Thêm phương thức helper `CreateDefault()` trả về cấu hình chuẩn nếu không có asset.
2. Tạo file asset `ScoreConfig.asset` tại `Assets/_Game/_BlockDrag/Data/ScoreConfig.asset`.

### Bước 2: Cập nhật `BlockGrid.cs` để truyền số lượng ô gạch
1. Khai báo event:
   ```csharp
   public event System.Action<int> OnShapePlacedWithCount;
   ```
2. Trong hàm `TryPlaceShape(BlockShape shape, Vector3 shapeWorldPos)`:
   - Trước khi `Destroy(shape.gameObject)`, đếm số SingleBlock hợp lệ (thông qua `shape.activeBlocks.Count` hoặc số block con).
   - Sau khi hoàn thành đặt khối, gọi `OnShapePlacedWithCount?.Invoke(singleBlockCount)`.
   - Giữ nguyên `OnShapePlaced?.Invoke()` để đảm bảo không làm gián đoạn các luồng hiện có.

### Bước 3: Hiện thực `ScoreManager.cs`
1. Tạo class `ScoreManager : MonoBehaviour` tại `Assets/_Game/_BlockDrag/ScoreManager.cs`:
   - Hỗ trợ Singleton instance: `public static ScoreManager Ins { get; private set; }`.
   - Cung cấp các thuộc tính `CurrentScore`, `HighScore`, `CurrentCombo`, `ComboTimer`, `IsComboActive`.
   - Lắng nghe:
     - `BlockGrid.OnShapePlacedWithCount` $\rightarrow$ cộng `count * pointsPerSingleBlock`.
     - `BlockGrid.OnLinesCleared` $\rightarrow$ tính điểm dòng cơ bản và xử lý chuỗi Combo:
       - Nếu combo đang kích hoạt: `currentCombo++`, `comboTimer += comboBonusDuration`, điểm thưởng = `(currentCombo - 1) * comboBonusMultiplier`.
       - Nếu chưa có combo: `currentCombo = 1`, `comboTimer = initialComboDuration`, `isComboActive = true`, điểm thưởng = 0.
     - `BlockSpawner.OnGameRestarted` $\rightarrow$ Reset điểm về 0, combo về 1, timer về 0.
     - `BlockSpawner.OnGameOver` $\rightarrow$ Lưu HighScore vào `PlayerPrefs`.
   - Trong `Update()`: đếm ngược `comboTimer -= Time.deltaTime`. Khi `comboTimer <= 0`, đặt `isComboActive = false`, reset `currentCombo = 1`, bắn `OnComboExpired`.

### Bước 4: Tích hợp hiển thị giao diện UI
1. **`GameplayCanvas.cs`**:
   - Khi `Open()` hoặc `Awake()`, đăng ký nhận sự kiện từ `ScoreManager`:
     - Cập nhật text `ScoreTxt` khi điểm thay đổi (với tween punch nhẹ).
     - Cập nhật text `HightScoreTxt`.
   - Hủy đăng ký sự kiện trong `OnDestroy()`.
2. **`GameOverUI.cs`**:
   - Thêm text hiển thị Final Score và HighScore trên bảng popup thông báo khi người chơi thua.
3. **`BlockSpawner.cs`**:
   - Tự động kiểm tra và thêm `ScoreManager` vào scene nếu chưa có (đảm bảo play test ngay lập tức mà không cần cấu hình thủ công trong Scene).

### Bước 5: Viết Unit Test tự động & Xác minh
1. Tạo file `Assets/Tests/EditMode/ScoreSystemTests.cs`:
   - Test 1: Đặt khối shape cộng đúng số điểm theo số ô gạch con.
   - Test 2: Phá 1 hàng/cột cộng đúng 25 điểm và kích hoạt 5s combo với combo = 1.
   - Test 3: Phá 2 dòng đồng thời cộng $2 \times 25 = 50$ điểm cơ bản.
   - Test 4: Phá dòng tiếp theo trong thời gian 5s $\rightarrow$ lên combo 2, thời gian được cộng thêm 3s, nhận thêm 25 điểm thưởng combo.
   - Test 5: Phá dòng lần 3 $\rightarrow$ lên combo 3, cộng tiếp 3s thời gian, nhận thêm 50 điểm thưởng combo ($25 \times (3 - 1)$).
   - Test 6: Khi combo timer đếm về 0 $\rightarrow$ combo tự reset về 1.
   - Test 7: Restart game $\rightarrow$ reset điểm về 0, combo về 1.
2. Chạy test và xác minh kết quả.

---

## 3. Tiêu Chí Nghiệm Thu (Acceptance Criteria)

- [ ] Đặt bất kỳ khối shape nào xuống bàn cờ: điểm tăng đúng bằng số SingleBlock của khối đó.
- [ ] Phá 1 hàng/cột: cộng 25 điểm. Phá đồng thời $N$ hàng/cột: cộng $N \times 25$ điểm.
- [ ] Phá hàng/cột lần đầu bắt đầu đếm ngược 5s cho Combo 1.
- [ ] Trong thời gian đếm ngược, nếu tiếp tục phá hàng/cột:
  - Tăng cấp Combo (2, 3, 4...).
  - Thời gian đang đếm được cộng thêm 3s.
  - Điểm thưởng combo được cộng thêm: $25 \times (\text{Combo} - 1)$ điểm.
- [ ] Khi hết thời gian đếm ngược: Combo tự động reset về 1.
- [ ] Có thể tinh chỉnh các tham số (5s, 3s, 25 điểm, ...) trực tiếp thông qua `ScoreConfigSO` ScriptableObject trong Unity Inspector.
- [ ] UI trên `GameplayCanvas` cập nhật điểm hiện tại và điểm kỷ lục mượt mà.
- [ ] Toàn bộ các bài kiểm thử tự động EditMode chạy thành công mà không có lỗi.
