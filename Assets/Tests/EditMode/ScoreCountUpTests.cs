using NUnit.Framework;
using UI;

namespace Tests.EditMode
{
    public class ScoreCountUpTests
    {
        private ScoreCountUp counter;

        [SetUp]
        public void SetUp()
        {
            counter = new ScoreCountUp { Duration = 1f };
            counter.Snap(100);
        }

        [Test]
        public void Snap_SetsDisplayedAndTarget()
        {
            counter.Snap(250);

            Assert.AreEqual(250, counter.Displayed);
            Assert.AreEqual(250, counter.Target);
            Assert.IsFalse(counter.IsRunning);
        }

        [Test]
        public void Tick_AfterDuration_DisplaysExactTarget()
        {
            counter.SetTarget(206);

            for (int i = 0; i < 10; i++)
            {
                counter.Tick(0.1f);
            }
            counter.Tick(0.01f);

            Assert.AreEqual(206, counter.Displayed);
            Assert.IsFalse(counter.IsRunning);
        }

        [Test]
        public void Tick_MidWay_DisplaysValueBetweenStartAndTarget()
        {
            counter.SetTarget(200);

            bool changed = counter.Tick(0.5f);

            Assert.IsTrue(changed);
            Assert.Greater(counter.Displayed, 100);
            Assert.Less(counter.Displayed, 200);
        }

        [Test]
        public void Tick_NeverExceedsTarget()
        {
            counter.SetTarget(103);

            int previous = counter.Displayed;
            for (int i = 0; i < 30; i++)
            {
                counter.Tick(0.05f);
                Assert.LessOrEqual(counter.Displayed, 103);
                Assert.GreaterOrEqual(counter.Displayed, previous);
                previous = counter.Displayed;
            }
            Assert.AreEqual(103, counter.Displayed);
        }

        [Test]
        public void SetTarget_WhileRunning_ContinuesFromDisplayedValue()
        {
            counter.SetTarget(200);
            counter.Tick(0.5f);
            int midway = counter.Displayed;

            counter.SetTarget(300);
            counter.Tick(0.001f);

            Assert.GreaterOrEqual(counter.Displayed, midway);
            Assert.Less(counter.Displayed, 300);

            counter.Tick(1f);
            Assert.AreEqual(300, counter.Displayed);
        }

        [Test]
        public void SetTarget_WithZeroDuration_JumpsImmediately()
        {
            counter.Duration = 0f;

            counter.SetTarget(500);

            Assert.AreEqual(500, counter.Displayed);
            Assert.IsFalse(counter.IsRunning);
        }

        [Test]
        public void SetTarget_LowerThanDisplayed_Snaps()
        {
            counter.SetTarget(0);

            Assert.AreEqual(0, counter.Displayed);
            Assert.IsFalse(counter.IsRunning);
        }
    }
}
