using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class ShakeOffsetTests
    {
        private const float Duration = 0.15f;
        private const float Amplitude = 0.009f;
        private const float Frequency = 24f;

        [Test]
        public void Evaluate_AtOrAfterDuration_ReturnsZero()
        {
            Assert.AreEqual(Vector2.zero, ShakeOffset.Evaluate(Duration, Duration, Amplitude, Frequency, 1));
            Assert.AreEqual(Vector2.zero, ShakeOffset.Evaluate(Duration + 1f, Duration, Amplitude, Frequency, 1));
            Assert.AreEqual(Vector2.zero, ShakeOffset.Evaluate(-0.01f, Duration, Amplitude, Frequency, 1));
            Assert.AreEqual(Vector2.zero, ShakeOffset.Evaluate(0f, 0f, Amplitude, Frequency, 1));
        }

        [Test]
        public void Evaluate_WithinDuration_MagnitudeNeverExceedsAmplitude()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                for (float t = 0f; t < Duration; t += 0.005f)
                {
                    float magnitude = ShakeOffset.Evaluate(t, Duration, Amplitude, Frequency, seed).magnitude;
                    Assert.LessOrEqual(magnitude, Amplitude + 1e-6f);
                }
            }
        }

        [Test]
        public void Evaluate_MagnitudeDecreasesOverTime()
        {
            float early = ShakeOffset.Evaluate(0.01f, Duration, Amplitude, Frequency, 3).magnitude;
            float late = ShakeOffset.Evaluate(Duration - 0.01f, Duration, Amplitude, Frequency, 3).magnitude;

            Assert.Greater(early, late);
            Assert.Greater(early, Amplitude * 0.5f);
        }

        [Test]
        public void Evaluate_SameSeedAndTime_IsDeterministic()
        {
            Vector2 a = ShakeOffset.Evaluate(0.07f, Duration, Amplitude, Frequency, 42);
            Vector2 b = ShakeOffset.Evaluate(0.07f, Duration, Amplitude, Frequency, 42);

            Assert.AreEqual(a, b);
        }

        [Test]
        public void Evaluate_NewStep_ChangesDirectionByAtLeast60Degrees()
        {
            float stepDuration = 1f / Frequency;
            for (int seed = 0; seed < 20; seed++)
            {
                for (int step = 0; step < 3; step++)
                {
                    Vector2 a = ShakeOffset.Evaluate(step * stepDuration + 0.001f, Duration, Amplitude, Frequency, seed);
                    Vector2 b = ShakeOffset.Evaluate((step + 1) * stepDuration + 0.001f, Duration, Amplitude, Frequency, seed);

                    Assert.GreaterOrEqual(Vector2.Angle(a, b), 59.9f);
                }
            }
        }
    }
}
