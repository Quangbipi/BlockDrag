using System;
using System.Collections.Generic;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Tính các mốc combo (bội số của step, mặc định 5) vừa vượt qua trong chuỗi combo hiện tại.
    /// Combo có thể nhảy cóc (4 → 6) vì tăng theo số hàng/cột phá cùng lúc, nên trả về mọi mốc bị vượt.
    /// Combo giảm (hết giờ combo / chơi lại) được coi là chuỗi mới.
    /// </summary>
    public sealed class ComboMilestoneTracker
    {
        public const int DefaultStep = 5;

        private readonly int step;
        private int lastCombo;

        public ComboMilestoneTracker(int step = DefaultStep)
        {
            if (step <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "Step must be positive.");
            }
            this.step = step;
        }

        public int LastMilestone { get; private set; }

        /// <summary>
        /// Cập nhật combo mới, ghi vào <paramref name="results"/> (đã Clear) các mốc mới đạt được theo thứ tự tăng dần
        /// </summary>
        public void Update(int combo, List<int> results)
        {
            results?.Clear();

            if (combo <= 0 || combo < lastCombo)
            {
                Reset();
            }

            if (combo <= 0) return;
            lastCombo = combo;

            for (int milestone = LastMilestone + step; milestone <= combo; milestone += step)
            {
                results?.Add(milestone);
                LastMilestone = milestone;
            }
        }

        public void Reset()
        {
            lastCombo = 0;
            LastMilestone = 0;
        }
    }
}
