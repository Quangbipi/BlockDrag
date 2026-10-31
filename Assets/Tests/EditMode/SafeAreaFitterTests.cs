using NUnit.Framework;
using UI;
using UnityEngine;

namespace Tests.EditMode
{
    public class SafeAreaFitterTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ComputeAnchors_FullScreen_ReturnsUnitAnchors()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0f, 0f, 1080f, 1920f), new Vector2(1080f, 1920f), out Vector2 min, out Vector2 max);

            Assert.AreEqual(0f, min.x, Tolerance);
            Assert.AreEqual(0f, min.y, Tolerance);
            Assert.AreEqual(1f, max.x, Tolerance);
            Assert.AreEqual(1f, max.y, Tolerance);
        }

        [Test]
        public void ComputeAnchors_TopNotchAndBottomInset_ShrinksVerticalAnchors()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0f, 102f, 1170f, 2383f), new Vector2(1170f, 2532f), out Vector2 min, out Vector2 max);

            Assert.AreEqual(0f, min.x, Tolerance);
            Assert.AreEqual(102f / 2532f, min.y, Tolerance);
            Assert.AreEqual(1f, max.x, Tolerance);
            Assert.AreEqual(2485f / 2532f, max.y, Tolerance);
        }

        [Test]
        public void ComputeAnchors_ZeroScreen_ReturnsUnitAnchors()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0f, 0f, 0f, 0f), Vector2.zero, out Vector2 min, out Vector2 max);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
