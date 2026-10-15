# Implementation Plan: LoseCanvas hiển thị Score (count-up) và HighScore

Design: [2026-10-14-losecanvas-score-display-design.md](../designs/2026-10-14-losecanvas-score-display-design.md)

## Impacted files
| File | Module | Thay đổi |
|---|---|---|
| `Assets/_Game/_UI/Scripts/LoseCanvas.cs` | `BlockDrag.UI` | Thêm logic set score/highscore + tween đếm |
| `Assets/_Game/Resources/UI/LoseCanvas.prefab` | asset | Gán `scoreTxt` → `ScoreTxt`, `highScoreTxt` → `BestScoreTxt` (hiện chưa được gán) |

Không đổi `.asmdef` (`BlockDrag.UI` đã tham chiếu `DOTween.Modules`, `BlockDrag`, `Unity.TextMeshPro`). Không đổi package hay scene.

## Steps

### 1. `LoseCanvas.cs`
1. Thêm `using DG.Tweening;`.
2. Thêm field:
   ```csharp
   [SerializeField]
   float scoreCountDuration = 0.5f;
   Tween scoreTween;
   ```
3. Thêm method:
   ```csharp
   /// <summary>
   /// Hiện HighScore ngay và chạy điểm ván vừa chơi tăng dần từ 0
   /// </summary>
   void ShowScore()
   {
       KillScoreTween();
       IScoreService score = Locator.Score;
       int finalScore = score != null ? score.CurrentScore : 0;
       int highScore = score != null ? Mathf.Max(finalScore, score.HighScore) : 0;

       highScoreTxt.text = highScore.ToString();
       scoreTxt.text = "0";
       if (finalScore <= 0) return;

       int shown = 0;
       scoreTween = DOTween.To(() => shown, v =>
           {
               shown = v;
               scoreTxt.text = v.ToString();
           }, finalScore, scoreCountDuration)
           .SetEase(Ease.OutQuad)
           .SetUpdate(true)
           .SetTarget(this);
   }

   void KillScoreTween()
   {
       scoreTween?.Kill();
       scoreTween = null;
   }
   ```
4. Gọi `ShowScore()` trong `Open(object param)` (sau phần set nút, chạy cho cả thắng lẫn thua).
5. Dừng tween khi đóng/huỷ canvas:
   - override `protected override void OnClose()` → `base.OnClose(); KillScoreTween();`
   - trong `OnDestroy()` thêm `KillScoreTween();`
6. Không sửa phần thay đổi chưa commit liên quan đến fix-compile (chỉ thêm code).

### 2. Prefab `LoseCanvas.prefab`
- Mở prefab qua Unity Editor (UnityMCP `manage_prefabs` / `manage_components`, hoặc thao tác trong Inspector) và gán:
  - `scoreTxt` → child `ScoreTxt` (TMP_Text)
  - `highScoreTxt` → child `BestScoreTxt` (TMP_Text)
- Giữ nguyên `.meta`.

## Verification
1. **Compile:** `read_console` (UnityMCP) không có lỗi sau khi Unity refresh.
2. **Prefab wiring:** đọc lại prefab và xác nhận hai field đã trỏ tới `ScoreTxt` và `BestScoreTxt`.
3. **Manual (Play Mode, scene gameplay):**
   - Chơi tới Game Over với điểm > 0 → `ScoreTxt` chạy từ 0 lên điểm cuối trong khoảng 0.5s; `BestScoreTxt` hiện kỷ lục ngay.
   - Lập kỷ lục mới → `BestScoreTxt` = điểm vừa đạt.
   - Bấm Try Again ngay lúc đang đếm → không có lỗi/NullReference, lần thua sau đếm lại từ 0.
4. **Automated:** Không thêm EditMode test — logic chỉ là binding UI + tween (không có data logic mới); kiểm tra thủ công như trên.
