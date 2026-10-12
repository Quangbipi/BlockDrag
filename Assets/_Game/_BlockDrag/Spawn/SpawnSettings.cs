using System;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Tham số tuning cho HandGenerator và DifficultyCurve
    /// </summary>
    [Serializable]
    public class SpawnSettings
    {
        [Header("Help (khối phá hàng)")]
        [Tooltip("Xác suất hand có ít nhất 1 khối phá được hàng/cột, theo tỷ lệ ô đã chiếm (0..1)")]
        public AnimationCurve helpChanceByFill = CreateDefaultHelpCurve();

        [Tooltip("Hệ số nhân xác suất hỗ trợ khi độ khó = 1 (0.5 = còn một nửa)")]
        [Range(0f, 1f)] public float lateHelpMultiplier = 0.5f;

        [Header("Difficulty theo HighScore")]
        [Tooltip("Mốc tối thiểu khi người chơi chưa có kỷ lục, tránh ván đầu bị khó tối đa ngay")]
        [Min(1)] public int minBaseline = 1000;

        [Tooltip("Độ khó tối đa khi chưa vượt kỷ lục")]
        [Range(0f, 1f)] public float belowRecordMaxT = 0.3f;

        [Tooltip("Vượt kỷ lục thêm bao nhiêu phần (theo kỷ lục) thì đạt độ khó tối đa")]
        [Min(0f)] public float overRecordRangeRatio = 0.5f;

        [Tooltip("Khoảng điểm tối thiểu sau khi vượt kỷ lục để đạt độ khó tối đa")]
        [Min(1)] public int minOverRange = 500;

        [Header("Generator")]
        [Tooltip("Số lần thử sinh hand trước khi dùng fallback khối nhỏ")]
        [Min(1)] public int maxAttempts = 30;

        [Tooltip("Trần số bước DFS cho mỗi lần kiểm tra hand đặt hết được")]
        [Min(1)] public int maxSolverNodes = 20000;

        public static SpawnSettings CreateDefault()
        {
            return new SpawnSettings();
        }

        private static AnimationCurve CreateDefaultHelpCurve()
        {
            return CreateLinearCurve(
                new Vector2(0f, 0.15f),
                new Vector2(0.4f, 0.3f),
                new Vector2(0.7f, 0.8f),
                new Vector2(1f, 0.9f));
        }

        /// <summary>
        /// Curve nội suy tuyến tính đi qua các điểm (x tăng dần)
        /// </summary>
        public static AnimationCurve CreateLinearCurve(params Vector2[] points)
        {
            Keyframe[] keys = new Keyframe[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                float inTangent = i > 0 ? Slope(points[i - 1], points[i]) : 0f;
                float outTangent = i < points.Length - 1 ? Slope(points[i], points[i + 1]) : 0f;
                keys[i] = new Keyframe(points[i].x, points[i].y, inTangent, outTangent);
            }
            return new AnimationCurve(keys);
        }

        private static float Slope(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(b.x, a.x) ? 0f : (b.y - a.y) / (b.x - a.x);
        }
    }
}
