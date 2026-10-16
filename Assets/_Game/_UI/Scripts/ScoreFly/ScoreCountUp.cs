using UnityEngine;

namespace UI
{
    /// <summary>
    /// Đếm số điểm hiển thị tăng dần từ số đang hiện lên Target trong Duration giây (ease OutQuad).
    /// C# thuần để unit-test; GameplayCanvas gọi Tick mỗi frame.
    /// </summary>
    public sealed class ScoreCountUp
    {
        private int from;
        private float elapsed;

        public int Displayed { get; private set; }
        public int Target { get; private set; }

        /// <summary>
        /// Thời gian chạy từ số đang hiển thị lên Target; &lt;= 0 thì nhảy số ngay
        /// </summary>
        public float Duration { get; set; } = 1f;

        public bool IsRunning => Displayed != Target;

        /// <summary>
        /// Gán ngay số hiển thị (bind lần đầu, ván mới)
        /// </summary>
        public void Snap(int value)
        {
            from = value;
            Displayed = value;
            Target = value;
            elapsed = 0f;
        }

        /// <summary>
        /// Đặt số cuối cần chạy tới. Nhỏ hơn số đang hiển thị thì Snap;
        /// lớn hơn thì chạy lại từ số đang hiển thị để không giật lùi khi có điểm mới chen vào.
        /// </summary>
        public void SetTarget(int target)
        {
            if (target == Target) return;

            if (target < Displayed || Duration <= 0f)
            {
                Snap(target);
                return;
            }

            from = Displayed;
            Target = target;
            elapsed = 0f;
        }

        /// <summary>
        /// Trả về true nếu số hiển thị thay đổi
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning) return false;

            int previous = Displayed;
            elapsed += deltaTime;

            if (Duration <= 0f || elapsed >= Duration)
            {
                Displayed = Target;
            }
            else
            {
                float t = elapsed / Duration;
                float eased = 1f - (1f - t) * (1f - t);
                Displayed = Mathf.Min(Target, from + Mathf.RoundToInt((Target - from) * eased));
            }

            return Displayed != previous;
        }
    }
}
