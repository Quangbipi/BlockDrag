using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;
using Solo.MOST_IN_ONE;

namespace Tests.EditMode
{
    public class LineClearImpactEffectTests
    {
        private GameObject cameraObject;
        private Camera camera;
        private GameObject shakeObject;
        private ScreenShake shake;
        private Matrix4x4 defaultView;
        private Matrix4x4 defaultProjection;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("TestCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 16f;
            camera.ResetWorldToCameraMatrix();
            defaultView = camera.worldToCameraMatrix;
            defaultProjection = camera.projectionMatrix;

            shakeObject = new GameObject("TestScreenShake");
            shake = shakeObject.AddComponent<ScreenShake>();
            shake.Cameras = new[] { camera };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(shakeObject);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void ScreenShake_WhileShaking_OffsetsViewOnly()
        {
            Vector3 position = cameraObject.transform.position;

            shake.Shake();
            shake.Tick(0.02f);

            Assert.IsTrue(shake.IsShaking);
            Assert.AreNotEqual(defaultView, camera.worldToCameraMatrix);
            Assert.AreEqual(defaultProjection, camera.projectionMatrix);
            Assert.AreEqual(position, cameraObject.transform.position);
        }

        [Test]
        public void ScreenShake_AfterDuration_RestoresDefaultView()
        {
            shake.Shake();
            for (int i = 0; i < 10; i++)
            {
                shake.Tick(shake.Duration / 4f);
            }

            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(defaultView, camera.worldToCameraMatrix);
        }

        [Test]
        public void ScreenShake_StopAndReset_MidShake_RestoresDefaultView()
        {
            shake.Shake();
            shake.Tick(0.03f);

            shake.StopAndReset();

            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(defaultView, camera.worldToCameraMatrix);
        }

        [Test]
        public void ScreenShake_ShakeTwice_DoesNotDrift()
        {
            shake.Shake();
            shake.Tick(0.05f);
            shake.Shake();
            shake.Tick(0.05f);
            shake.Tick(shake.Duration);

            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(defaultView, camera.worldToCameraMatrix);
        }

        [Test]
        public void ScreenShake_OffsetStaysWithinAmplitudeOfViewWidth()
        {
            float maxWorld = 0.009f * ScreenShake.GetViewWidth(camera);

            shake.Shake();
            for (int i = 0; i < 6; i++)
            {
                Vector3 delta = camera.worldToCameraMatrix.GetColumn(3) - defaultView.GetColumn(3);
                Assert.LessOrEqual(new Vector2(delta.x, delta.y).magnitude, maxWorld + 1e-5f);
                Assert.AreEqual(0f, delta.z, 1e-6f);
                shake.Tick(0.02f);
            }
        }

        [Test]
        public void GetFadeDuration_ScalesWithLinesAndClamps()
        {
            var range = new Vector2(0.25f, 0.4f);

            Assert.AreEqual(0.25f, LineClearImpactEffect.GetFadeDuration(1, range, 3), 1e-5f);
            Assert.AreEqual(0.325f, LineClearImpactEffect.GetFadeDuration(2, range, 3), 1e-5f);
            Assert.AreEqual(0.4f, LineClearImpactEffect.GetFadeDuration(3, range, 3), 1e-5f);
            Assert.AreEqual(0.4f, LineClearImpactEffect.GetFadeDuration(8, range, 3), 1e-5f);
            Assert.AreEqual(0.25f, LineClearImpactEffect.GetFadeDuration(0, range, 3), 1e-5f);
        }

        [Test]
        public void GetHapticType_WhenSingleLine_ReturnsMediumImpact()
        {
            Assert.AreEqual(MOST_HapticFeedback.HapticTypes.MediumImpact, LineClearImpactEffect.GetHapticType(1, 2));
        }

        [Test]
        public void GetHapticType_WhenLinesReachHeavyThreshold_ReturnsHeavyImpact()
        {
            Assert.AreEqual(MOST_HapticFeedback.HapticTypes.HeavyImpact, LineClearImpactEffect.GetHapticType(2, 2));
            Assert.AreEqual(MOST_HapticFeedback.HapticTypes.HeavyImpact, LineClearImpactEffect.GetHapticType(4, 2));
        }

        [Test]
        public void GetGlowLocalSize_IsFramePlusHaloOnEachSide()
        {
            Vector2 frame = BlockGrid.ComputeFrameSize(8f, 8f, 1f, 0.12f);
            Vector2 size = LineClearImpactEffect.GetGlowLocalSize(8f, 8f, 1f, 0.12f, 0.3f);

            Assert.AreEqual(frame.x + 0.6f, size.x, 1e-5f);
            Assert.AreEqual(frame.y + 0.6f, size.y, 1e-5f);
        }
    }
}
