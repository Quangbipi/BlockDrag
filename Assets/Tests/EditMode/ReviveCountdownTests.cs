using System.Collections.Generic;
using NUnit.Framework;
using UI;

namespace Tests.EditMode
{
    public class ReviveCountdownTests
    {
        [Test]
        public void Tick_StepsShownFromFiveToZeroOnWholeSeconds()
        {
            ReviveCountdown countdown = new ReviveCountdown(5, 2f, 0.3f, 2);
            List<int> shown = new List<int>();
            countdown.OnSecondChanged += shown.Add;

            countdown.Start();
            Advance(countdown, 0.99f);
            Assert.AreEqual(5, countdown.Shown);
            Advance(countdown, 0.02f);
            Assert.AreEqual(4, countdown.Shown);
            Advance(countdown, 4f);

            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1, 0 }, shown);
        }

        [Test]
        public void Tick_LargeDelta_StillReportsEverySecond()
        {
            ReviveCountdown countdown = new ReviveCountdown(5, 2f, 10f, 2);
            List<int> shown = new List<int>();
            countdown.OnSecondChanged += shown.Add;

            countdown.Start();
            countdown.Tick(3.5f);

            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2 }, shown);
        }

        [Test]
        public void IsRed_StartsAtTwo()
        {
            ReviveCountdown countdown = new ReviveCountdown(5, 2f, 0.3f, 2);
            countdown.Start();

            Advance(countdown, 2.5f);
            Assert.AreEqual(3, countdown.Shown);
            Assert.IsFalse(countdown.IsRed);

            Advance(countdown, 1f);
            Assert.AreEqual(2, countdown.Shown);
            Assert.IsTrue(countdown.IsRed);
        }

        [Test]
        public void OnSkipAvailable_FiresOnceAtTwoSeconds()
        {
            ReviveCountdown countdown = new ReviveCountdown(5, 2f, 0.3f, 2);
            int raised = 0;
            countdown.OnSkipAvailable += () => raised++;

            countdown.Start();
            Advance(countdown, 1.95f);
            Assert.AreEqual(0, raised);

            Advance(countdown, 0.1f);
            Assert.AreEqual(1, raised);

            Advance(countdown, 3f);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void OnExpired_FiresOnceAfterZeroHold()
        {
            ReviveCountdown countdown = new ReviveCountdown(5, 2f, 0.3f, 2);
            int expired = 0;
            countdown.OnExpired += () => expired++;

            countdown.Start();
            Advance(countdown, 5.2f);
            Assert.AreEqual(0, countdown.Shown);
            Assert.AreEqual(0, expired);

            Advance(countdown, 0.2f);
            Assert.AreEqual(1, expired);
            Assert.IsFalse(countdown.IsRunning);

            Advance(countdown, 2f);
            Assert.AreEqual(1, expired);
        }

        [Test]
        public void Stop_PreventsExpiry()
        {
            ReviveCountdown countdown = new ReviveCountdown(5, 2f, 0.3f, 2);
            int expired = 0;
            countdown.OnExpired += () => expired++;

            countdown.Start();
            Advance(countdown, 1f);
            countdown.Stop();
            Advance(countdown, 10f);

            Assert.AreEqual(0, expired);
            Assert.AreEqual(4, countdown.Shown);
        }

        private static void Advance(ReviveCountdown countdown, float seconds)
        {
            const float step = 0.05f;
            while (seconds > 1e-4f)
            {
                float dt = seconds < step ? seconds : step;
                countdown.Tick(dt);
                seconds -= dt;
            }
        }
    }
}
