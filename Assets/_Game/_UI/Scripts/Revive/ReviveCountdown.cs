using System;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Mốc thời gian đếm ngược của RevivePopup (C# thuần, tick bằng unscaled delta):
    /// số hiển thị giảm 1 mỗi giây từ seconds → 0, đỏ từ redFromSecond trở xuống,
    /// Skip hiện sau skipDelay, hết giờ sau khi giữ số 0 thêm zeroHold giây.
    /// </summary>
    public sealed class ReviveCountdown
    {
        public event Action<int> OnSecondChanged;
        public event Action OnSkipAvailable;
        public event Action OnExpired;

        private readonly int seconds;
        private readonly float skipDelay;
        private readonly float zeroHold;
        private readonly int redFromSecond;

        private float elapsed;
        private bool skipRaised;

        public int Seconds => seconds;
        public int Shown { get; private set; }
        public bool IsRed => Shown <= redFromSecond;
        public bool IsRunning { get; private set; }
        public float Elapsed => elapsed;

        public ReviveCountdown(int seconds = 5, float skipDelay = 2f, float zeroHold = 0.3f, int redFromSecond = 2)
        {
            this.seconds = Mathf.Max(1, seconds);
            this.skipDelay = Mathf.Max(0f, skipDelay);
            this.zeroHold = Mathf.Max(0f, zeroHold);
            this.redFromSecond = redFromSecond;
            Shown = this.seconds;
        }

        /// <summary>
        /// Bắt đầu lại từ đầu và báo ngay số đầu tiên
        /// </summary>
        public void Start()
        {
            elapsed = 0f;
            skipRaised = false;
            Shown = seconds;
            IsRunning = true;
            OnSecondChanged?.Invoke(Shown);
            CheckSkip();
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0f) return;

            elapsed += deltaTime;

            int shown = Mathf.Clamp(seconds - Mathf.FloorToInt(elapsed), 0, seconds);
            while (Shown > shown && IsRunning)
            {
                Shown--;
                OnSecondChanged?.Invoke(Shown);
            }

            CheckSkip();

            if (IsRunning && elapsed >= seconds + zeroHold)
            {
                IsRunning = false;
                OnExpired?.Invoke();
            }
        }

        private void CheckSkip()
        {
            if (skipRaised || !IsRunning || elapsed < skipDelay) return;
            skipRaised = true;
            OnSkipAvailable?.Invoke();
        }
    }
}
