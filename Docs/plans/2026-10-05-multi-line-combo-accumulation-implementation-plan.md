# Multi-Line Clear Combo Accumulation Implementation Plan

**Date:** 2026-10-05  
**Feature:** Multi-Line Clear Combo Accumulation Logic  
**Design Reference:** [Docs/designs/2026-10-05-multi-line-combo-accumulation-design.md](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Docs/designs/2026-10-05-multi-line-combo-accumulation-design.md)

---

## 1. Overview & Objectives
Cập nhật cơ chế tính combo trong [ScoreManager.cs](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/ScoreManager.cs):
- Phá hủy 1 hàng/cột: Tăng 1 combo.
- Phá hủy $N$ hàng/cột cùng lúc trong 1 lần đặt: Tăng thêm $N$ cấp combo.
  - Khởi đầu chuỗi phá ngay $N \ge 2$ dòng $\rightarrow$ Lập tức đạt Combo $N$ và kích hoạt hiệu ứng Text Combo $N$.
  - Đang trong chuỗi (Combo $C$) phá tiếp $N$ dòng $\rightarrow$ Lên Combo $C + N$ và kích hoạt hiệu ứng Text Combo tương ứng.

---

## 2. Affected Files & Boundaries

| File | Module / Namespace | Type of Change | Purpose |
|---|---|---|---|
| `Assets/_Game/_BlockDrag/ScoreManager.cs` | `Gameplay.BlockDrag` | Modify | Cập nhật hàm `HandleLinesCleared()` để tính `currentCombo` theo `linesCleared`. |
| `Assets/Tests/EditMode/ScoreSystemTests.cs` | `Tests.EditMode` | Modify / Extend | Thêm EditMode tests xác thực các kịch bản combo 1 dòng và nhiều dòng. |

---

## 3. Step-by-Step Implementation Steps

### Step 1: Modify `ScoreManager.cs`
- Trong `HandleLinesCleared(int rowsCleared, int colsCleared, int totalCellsCleared)`:
  - Tính `linesCleared = rowsCleared + colsCleared`.
  - Nếu `linesCleared <= 0` $\rightarrow$ return.
  - Nếu `isComboActive && comboTimer > 0f`:
    - `currentCombo += linesCleared` (thay vì `currentCombo++`).
    - `comboBonusPoints = (currentCombo - 1) * Config.ComboBonusMultiplier`.
    - `comboTimer += Config.ComboBonusDuration * linesCleared` (hoặc `Config.ComboBonusDuration`).
  - Nếu `!isComboActive`:
    - `currentCombo = linesCleared` (thay vì cố định `currentCombo = 1`).
    - `comboBonusPoints = (currentCombo - 1) * Config.ComboBonusMultiplier`.
    - `comboTimer = Config.InitialComboDuration`.
    - `isComboActive = true`.
    - Gọi `StartComboTimer(comboTimer)`.
- Khi `HandleLinesClearedWithPos(...)` được gọi:
  - Nếu `currentCombo >= 2`, phát `OnComboTriggered?.Invoke(currentCombo, placedPos)`.

### Step 2: Update and Add Tests in `ScoreSystemTests.cs`
- Test 1: `HandleLinesCleared_SingleLine_StartsCombo1` (Phá 1 dòng ban đầu $\rightarrow$ Combo 1, 25 điểm).
- Test 2: `HandleLinesCleared_MultiLineInitial_StartsAtMultiCombo` (Phá 2 dòng cùng lúc ban đầu $\rightarrow$ Combo 2, 50 điểm cơ bản + 25 điểm combo = 75 điểm, kích hoạt event combo 2).
- Test 3: `HandleLinesCleared_ConsecutiveMultiLine_AccumulatesCorrectly` (Combo 1 + phá tiếp 2 dòng $\rightarrow$ Combo 3).
- Test 4: `HandleLinesCleared_TripleLineInitial_StartsAtCombo3` (Phá 3 dòng cùng lúc ban đầu $\rightarrow$ Combo 3, 75 điểm cơ bản + 50 điểm combo = 125 điểm).

### Step 3: Verification
- Chạy toàn bộ EditMode tests để đảm bảo không hồi quy logic điểm số.
- Kiểm tra tính tương thích với UI Combo Text (khi nhận event `OnComboTriggered` với combo 2, 3, 4...).
