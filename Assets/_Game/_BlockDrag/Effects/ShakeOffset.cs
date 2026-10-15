using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Tính độ lệch rung màn hình theo thời gian: mỗi bước (1 / frequency giây) đổi sang hướng khác,
    /// biên độ giảm tuyến tính về 0 ở cuối duration. Không dùng UnityEngine.Random để không đụng state random của gameplay.
    /// </summary>
    public static class ShakeOffset
    {
        // Độ lệch ngẫu nhiên quanh hướng ngược bước trước => 2 bước liền nhau luôn cách nhau ít nhất 60°
        private const float StepJitter = Mathf.PI * 2f / 3f;

        /// <summary>
        /// Offset theo tỉ lệ chiều rộng màn hình (x và y cùng đơn vị pixel), Vector2.zero khi ngoài [0, duration)
        /// </summary>
        public static Vector2 Evaluate(float elapsed, float duration, float amplitude, float frequency, int seed)
        {
            if (duration <= 0f || elapsed < 0f || elapsed >= duration) return Vector2.zero;

            int step = Mathf.FloorToInt(elapsed * Mathf.Max(0.01f, frequency));
            float angle = Hash01(seed, -1) * Mathf.PI * 2f
                          + step * Mathf.PI
                          + (Hash01(seed, step) - 0.5f) * StepJitter;
            float magnitude = amplitude * (1f - elapsed / duration);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * magnitude;
        }

        /// <summary>
        /// Hash số nguyên (seed, step) về [0, 1)
        /// </summary>
        public static float Hash01(int seed, int step)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u ^ (uint)step * 0x85EBCA77u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
