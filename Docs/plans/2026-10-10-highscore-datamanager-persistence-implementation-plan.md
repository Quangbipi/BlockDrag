# Kế Hoạch Triển Khai: Lưu High Score qua DataManager (GameData)

> **Ngày tạo:** 2026-10-10  
> **Tài liệu thiết kế:** [`Docs/designs/2026-10-10-highscore-datamanager-persistence-design.md`](../designs/2026-10-10-highscore-datamanager-persistence-design.md)  

---

## 1. Tệp & Module Bị Tác Động

| Tệp | Hành động | Assembly | Nội dung |
| :--- | :--- | :--- | :--- |
| `Assets/_Game/_Base/GameData.cs` | Sửa | `Hung.Base` | Thêm `public int highScore;` vào `GameData.UserData`. |
| `Assets/_Game/_BlockDrag/IHighScoreStore.cs` | Tạo mới | `BlockDrag` | Interface `Load()` / `Save(int)`. |
| `Assets/_Game/_BlockDrag/GameDataHighScoreStore.cs` | Tạo mới | `BlockDrag` | Đọc/ghi `GameData.user.highScore` qua `Locator.Data`, fallback `Database`, migrate key cũ. |
| `Assets/_Game/_BlockDrag/ScoreManager.cs` | Sửa | `BlockDrag` | Bỏ `PlayerPrefs`; dùng `IHighScoreStore`; dirty flag; lưu khi GameOver/Restart/Pause/Quit. |
| `Assets/Tests/EditMode/ScoreSystemTests.cs` | Sửa | `EditModeTests` | Inject fake store trong `SetUp`; thêm test load/save/dirty. |
| `Assets/Tests/EditMode/GameDataHighScoreStoreTests.cs` | Tạo mới | `EditModeTests` | Test migrate key cũ và `Save` lấy `max`. |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Sửa | `EditModeTests` | Thêm reference `Hung.Base` (`GUID:d8f99e86a031141438d186833c629fd7`) để dùng `Locator`, `IDataService`, `GameData`. |

Không đổi scene, prefab hay asmdef của runtime (`BlockDrag` đã tham chiếu `Hung.Base`).

---

## 2. Các Bước

1. **`GameData.UserData`**: thêm `public int highScore;` (JSON cũ thiếu trường → `0`).
2. **`IHighScoreStore`**: `int Load();` và `void Save(int highScore);`.
3. **`GameDataHighScoreStore`**:
   - `GetGameData()`: `Locator.Data?.GetData<GameData>()`; nếu `null` → `fallbackData ??= Database.Load<GameData>()`.
   - `Persist()`: `Locator.Data.Save()` nếu có, ngược lại `Database.Save(fallbackData)`.
   - `Load()`: nếu `PlayerPrefs.HasKey(legacyKey)` → `user.highScore = Max(user.highScore, legacy)`, `Persist()`, `PlayerPrefs.DeleteKey(legacyKey)`. Trả về `user.highScore`.
   - `Save(v)`: nếu `v > user.highScore` → gán và `Persist()`.
4. **`ScoreManager`**:
   - Xoá `HighScoreKey` / `PlayerPrefs`.
   - Thêm field `highScoreStore`, `isHighScoreDirty`; property `HighScoreStore` lazy tạo `GameDataHighScoreStore`.
   - `SetHighScoreStore(IHighScoreStore)` → gán store, `LoadHighScore()`.
   - `AddScore`: khi vượt kỷ lục đặt `isHighScoreDirty = true`.
   - `SaveHighScore()`: đồng bộ `highScore` với `currentScore`, chỉ gọi `store.Save` khi dirty.
   - `HandleGameRestarted`: `SaveHighScore()` trước `ResetScoreAndCombo()`.
   - Thêm `OnApplicationPause(bool paused)` (lưu khi `paused`) và `OnApplicationQuit()`.
5. **Tests**: cập nhật `EditModeTests.asmdef`, `ScoreSystemTests`, thêm `GameDataHighScoreStoreTests`.

---

## 3. Kiểm Thử & Xác Minh

**Tự động (EditMode):**
- `SetHighScoreStore_LoadsHighScoreFromStore`
- `SaveHighScore_AfterBeatingRecord_WritesNewHighScoreToStore`
- `SaveHighScore_WithoutNewRecord_DoesNotWriteToStore`
- `Load_WithLegacyPlayerPrefsKey_MigratesValueAndDeletesKey`
- `Load_WithLegacyValueLowerThanGameData_KeepsGameDataValue`
- `Save_WithLowerValue_DoesNotOverwriteHigherHighScore`
- Toàn bộ test `ScoreSystemTests` cũ vẫn pass.

**Thủ công (Editor):**
1. Vào game từ `LoadStart`, phá kỷ lục, thua → khởi động lại Play Mode → `HightScoreTxt` hiển thị đúng kỷ lục.
2. Chạy thẳng `GameScene` (không có `DataManager`), phá kỷ lục, dừng Play Mode → chạy lại → kỷ lục vẫn còn.
3. Với dữ liệu cũ có key `BlockDrag_HighScore`: sau lần chạy đầu, key bị xoá và kỷ lục hiển thị không đổi.
