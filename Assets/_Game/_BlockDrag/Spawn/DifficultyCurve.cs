using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Độ khó (0..1) theo điểm so với HighScore chụp lúc bắt đầu ván:
    /// trước kỷ lục tăng nhẹ tới belowRecordMaxT, vượt kỷ lục thì tăng dần tới 1.
    /// </summary>
    public static class DifficultyCurve
    {
        public static float Evaluate(int score, int baselineHighScore, SpawnSettings settings)
        {
            if (settings == null) return 0f;

            float baseline = Mathf.Max(baselineHighScore, settings.minBaseline, 1);
            float s = Mathf.Max(0, score);
            float below = Mathf.Clamp01(settings.belowRecordMaxT);

            if (s < baseline)
            {
                return below * (s / baseline);
            }

            float range = Mathf.Max(settings.minOverRange, baseline * settings.overRecordRangeRatio, 1f);
            return below + (1f - below) * Mathf.Clamp01((s - baseline) / range);
        }

        /// <summary>
        /// Điểm chưa vượt kỷ lục đầu ván (người chơi mới dùng minBaseline làm kỷ lục)
        /// </summary>
        public static bool IsBelowRecord(int score, int baselineHighScore, SpawnSettings settings)
        {
            int minBaseline = settings != null ? settings.minBaseline : 0;
            return score < Mathf.Max(baselineHighScore, minBaseline);
        }

        /// <summary>
        /// Xác suất dùng assist hand: 1 khi chưa vượt kỷ lục; sau đó giảm dần về 0 hoặc tắt hẳn theo fadeAssistAfterRecord
        /// </summary>
        public static float AssistChance(bool belowRecord, float difficulty, SpawnSettings settings)
        {
            if (belowRecord) return 1f;
            if (settings == null || !settings.fadeAssistAfterRecord) return 0f;

            float below = Mathf.Clamp01(settings.belowRecordMaxT);
            float u = below >= 1f ? 1f : Mathf.Clamp01((difficulty - below) / (1f - below));
            return Mathf.Lerp(Mathf.Clamp01(settings.assistChanceAtRecord), 0f, u);
        }
    }
}
