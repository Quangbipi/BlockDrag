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
    }
}
