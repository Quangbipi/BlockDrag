using System.Collections.Generic;
using Gameplay.BlockDrag;
using NUnit.Framework;
using Quang.Base;
using TMPro;
using UI;
using UnityEngine;

namespace Tests.EditMode
{
    public class ScoreFlyTests
    {
        private class FakeHighScoreStore : IHighScoreStore
        {
            public int Load() => 0;
            public void Save(int highScore) { }
        }

        private GameObject scoreManagerObject;
        private ScoreManager scoreManager;
        private ScoreConfigSO config;
        private readonly List<ScoreGain> gains = new List<ScoreGain>();

        private GameObject canvasGo;
        private GameObject prefabGo;
        private ScoreFlyManager flyManager;

        [SetUp]
        public void SetUp()
        {
            scoreManagerObject = new GameObject("TestScoreManager");
            scoreManager = scoreManagerObject.AddComponent<ScoreManager>();
            config = ScriptableObject.CreateInstance<ScoreConfigSO>();
            scoreManager.Config = config;
            scoreManager.SetHighScoreStore(new FakeHighScoreStore());
            scoreManager.ResetScoreAndCombo();

            gains.Clear();
            scoreManager.OnScoreGained += gains.Add;
        }

        [TearDown]
        public void TearDown()
        {
            if (scoreManagerObject != null) Object.DestroyImmediate(scoreManagerObject);
            if (config != null) Object.DestroyImmediate(config);
            if (canvasGo != null) Object.DestroyImmediate(canvasGo);
            if (prefabGo != null) Object.DestroyImmediate(prefabGo);
        }

        private void CreateFlyManager()
        {
            prefabGo = new GameObject("TextScoreFly_Prefab", typeof(RectTransform), typeof(CanvasGroup));
            var fly = prefabGo.AddComponent<TextScoreFly>();
            var labelGo = new GameObject("Amount", typeof(RectTransform));
            labelGo.transform.SetParent(prefabGo.transform, false);
            fly.InitReferences(labelGo.AddComponent<TextMeshProUGUI>(), prefabGo.GetComponent<CanvasGroup>());

            canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var target = new GameObject("Score", typeof(RectTransform));
            target.transform.SetParent(canvasGo.transform, false);

            flyManager = canvasGo.AddComponent<ScoreFlyManager>();
            flyManager.FlyPrefab = fly;
            flyManager.FlyTarget = (RectTransform)target.transform;
        }

        [Test]
        public void ShapePlaced_RaisesScoreGained_WithPlacementSourceAndNoWorldPos()
        {
            scoreManager.HandleShapePlacedWithCount(4);

            Assert.AreEqual(1, gains.Count);
            Assert.AreEqual(4, gains[0].Amount);
            Assert.AreEqual(ScoreSource.Placement, gains[0].Source);
            Assert.IsFalse(gains[0].HasWorldPos);
        }

        [Test]
        public void LinesClearedWithPos_RaisesSingleLineClearGain_WithBasePlusComboAndPlacedPos()
        {
            Vector3 placedPos = new Vector3(1f, 2f, 0f);

            // 2 hàng cùng lúc: 2 * 25 cơ bản + (2 - 1) * 25 thưởng combo
            scoreManager.HandleLinesClearedWithPos(1, 1, 15, placedPos);

            Assert.AreEqual(1, gains.Count);
            Assert.AreEqual(75, gains[0].Amount);
            Assert.AreEqual(ScoreSource.LineClear, gains[0].Source);
            Assert.IsTrue(gains[0].HasWorldPos);
            Assert.AreEqual(placedPos, gains[0].WorldPos);
        }

        [Test]
        public void PerfectClear_RaisesPerfectClearGain_WithBoardCenter()
        {
            Vector3 center = new Vector3(0f, -1f, 0f);

            scoreManager.HandlePerfectClear(center);

            Assert.AreEqual(1, gains.Count);
            Assert.AreEqual(ScoreSource.PerfectClear, gains[0].Source);
            Assert.AreEqual(scoreManager.CurrentScore, gains[0].Amount);
            Assert.AreEqual(center, gains[0].WorldPos);
        }

        [Test]
        public void ScoreGained_IsRaisedBeforeScoreChanged()
        {
            var order = new List<string>();
            scoreManager.OnScoreGained += _ => order.Add("gained");
            scoreManager.OnScoreChanged += _ => order.Add("changed");

            scoreManager.HandleShapePlacedWithCount(3);

            CollectionAssert.AreEqual(new[] { "gained", "changed" }, order);
        }

        [Test]
        public void ScoreGainedAmounts_SumToCurrentScore()
        {
            scoreManager.HandleShapePlacedWithCount(4);
            scoreManager.HandleLinesClearedWithPos(1, 0, 8, Vector3.zero);
            scoreManager.HandleShapePlacedWithCount(3);
            scoreManager.HandleLinesClearedWithPos(0, 1, 8, Vector3.one);
            scoreManager.HandlePerfectClear(Vector3.zero);

            int sum = 0;
            foreach (ScoreGain gain in gains) sum += gain.Amount;

            Assert.AreEqual(scoreManager.CurrentScore, sum);
        }

        [Test]
        public void ScoreFlyManager_TrySpawn_PlacementSource_ReturnsFalseAndNoPending()
        {
            CreateFlyManager();

            bool spawned = flyManager.TrySpawn(new ScoreGain(4, ScoreSource.Placement, Vector3.zero));

            Assert.IsFalse(spawned);
            Assert.AreEqual(0, flyManager.PendingAmount);
            Assert.AreEqual(0, flyManager.FlyingCount);
        }

        [Test]
        public void ScoreFlyManager_TrySpawn_WithoutWorldPos_ReturnsFalse()
        {
            CreateFlyManager();

            bool spawned = flyManager.TrySpawn(new ScoreGain(50, ScoreSource.LineClear));

            Assert.IsFalse(spawned);
            Assert.AreEqual(0, flyManager.PendingAmount);
        }

        [Test]
        public void ScoreFlyManager_TrySpawn_LineClear_AddsPendingAmount()
        {
            CreateFlyManager();

            Assert.IsTrue(flyManager.TrySpawn(new ScoreGain(106, ScoreSource.LineClear, Vector3.zero)));
            Assert.IsTrue(flyManager.TrySpawn(new ScoreGain(300, ScoreSource.PerfectClear, Vector3.zero)));

            Assert.AreEqual(406, flyManager.PendingAmount);
            Assert.AreEqual(2, flyManager.FlyingCount);
        }

        [Test]
        public void ScoreFlyManager_CancelAll_ClearsPendingAndDeactivatesTexts()
        {
            CreateFlyManager();
            flyManager.TrySpawn(new ScoreGain(106, ScoreSource.LineClear, Vector3.zero));

            flyManager.CancelAll();

            Assert.AreEqual(0, flyManager.PendingAmount);
            Assert.AreEqual(0, flyManager.FlyingCount);
            foreach (TextScoreFly fly in canvasGo.GetComponentsInChildren<TextScoreFly>(true))
            {
                Assert.IsFalse(fly.gameObject.activeSelf);
            }
        }
    }
}
