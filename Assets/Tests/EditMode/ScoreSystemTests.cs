using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class ScoreSystemTests
    {
        private GameObject scoreManagerObject;
        private ScoreManager scoreManager;
        private ScoreConfigSO config;

        [SetUp]
        public void SetUp()
        {
            scoreManagerObject = new GameObject("TestScoreManager");
            scoreManager = scoreManagerObject.AddComponent<ScoreManager>();

            config = ScriptableObject.CreateInstance<ScoreConfigSO>();
            // Sử dụng các thông số mặc định theo đúng yêu cầu người dùng
            // pointsPerSingleBlock = 1, pointsPerLine = 25, initialComboDuration = 5, comboBonusDuration = 3, comboBonusMultiplier = 25
            scoreManager.Config = config;
            scoreManager.ResetScoreAndCombo();
        }

        [TearDown]
        public void TearDown()
        {
            if (scoreManagerObject != null)
            {
                Object.DestroyImmediate(scoreManagerObject);
            }
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void AddPlacementScore_WhenShapePlaced_AwardsScoreEqualToSingleBlockCount()
        {
            // Đặt khối gồm 4 SingleBlock con
            scoreManager.HandleShapePlacedWithCount(4);

            Assert.AreEqual(4, scoreManager.CurrentScore);

            // Đặt thêm khối gồm 5 SingleBlock con
            scoreManager.HandleShapePlacedWithCount(5);

            Assert.AreEqual(9, scoreManager.CurrentScore);
        }

        [Test]
        public void ClearSingleLine_NoActiveCombo_Awards25PointsAndStarts5sTimerAtCombo1()
        {
            int receivedCombo = 0;
            int receivedBonus = -1;
            scoreManager.OnComboChanged += (combo, bonus) =>
            {
                receivedCombo = combo;
                receivedBonus = bonus;
            };

            // Phá 1 hàng (rows = 1, cols = 0)
            scoreManager.HandleLinesCleared(1, 0, 8);

            Assert.AreEqual(25, scoreManager.CurrentScore);
            Assert.AreEqual(1, scoreManager.CurrentCombo);
            Assert.IsTrue(scoreManager.IsComboActive);
            Assert.AreEqual(5f, scoreManager.ComboTimer, 0.001f);
            Assert.AreEqual(1, receivedCombo);
            Assert.AreEqual(0, receivedBonus); // Combo 1 chưa có điểm thưởng combo
        }

        [Test]
        public void ClearMultipleLines_Simultaneous_StartsComboByLinesClearedAndAwardsBonus()
        {
            // Phá đồng thời 1 hàng và 1 cột = 2 lines ngay từ đầu
            scoreManager.HandleLinesCleared(1, 1, 15);

            // 2 lines * 25 = 50 cơ bản + (2 - 1) * 25 = 25 thưởng combo = 75 điểm
            Assert.AreEqual(75, scoreManager.CurrentScore);
            Assert.AreEqual(2, scoreManager.CurrentCombo);
            Assert.AreEqual(5f, scoreManager.ComboTimer, 0.001f);
        }

        [Test]
        public void ClearTripleLines_Simultaneous_StartsCombo3AndAwardsBonus()
        {
            // Phá đồng thời 3 dòng = 3 lines ngay từ đầu
            scoreManager.HandleLinesCleared(2, 1, 20);

            // 3 lines * 25 = 75 cơ bản + (3 - 1) * 25 = 50 thưởng combo = 125 điểm
            Assert.AreEqual(125, scoreManager.CurrentScore);
            Assert.AreEqual(3, scoreManager.CurrentCombo);
            Assert.AreEqual(5f, scoreManager.ComboTimer, 0.001f);
        }

        [Test]
        public void ClearMultipleLines_DuringComboStreak_AccumulatesLinesCleared()
        {
            // Bước 1: Phá 1 dòng -> Combo 1, 25 điểm
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.AreEqual(1, scoreManager.CurrentCombo);
            Assert.AreEqual(25, scoreManager.CurrentScore);

            // Bước 2: Trong thời gian combo, phá tiếp 2 dòng cùng lúc
            // Combo tăng thêm 2: 1 + 2 = Combo 3!
            // Điểm dòng = 2 * 25 = 50
            // Điểm combo = (3 - 1) * 25 = 50
            // Tổng cộng thêm = 100 -> Điểm mới: 25 + 100 = 125
            scoreManager.HandleLinesCleared(1, 1, 15);
            Assert.AreEqual(3, scoreManager.CurrentCombo);
            Assert.AreEqual(125, scoreManager.CurrentScore);
        }

        [Test]
        public void HandleLinesClearedWithPos_MultiLineInitial_TriggersComboEvent()
        {
            int triggeredCombo = 0;
            Vector3 triggeredPos = Vector3.zero;
            scoreManager.OnComboTriggered += (combo, pos) =>
            {
                triggeredCombo = combo;
                triggeredPos = pos;
            };

            Vector3 spawnPos = new Vector3(2.5f, 3.5f, 0f);
            // Phá 2 dòng cùng lúc ngay từ đầu -> Đạt Combo 2 -> Kích hoạt event
            scoreManager.HandleLinesClearedWithPos(1, 1, 15, spawnPos);

            Assert.AreEqual(2, triggeredCombo);
            Assert.AreEqual(spawnPos, triggeredPos);
        }

        [Test]
        public void ClearLines_Within5sWindow_IncrementsComboAdds3sAndAwardsBonusMultiplierTimesComboMinusOne()
        {
            // Bước 1: Phá 1 dòng lần đầu -> Combo 1, 25 điểm, timer 5s
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.AreEqual(25, scoreManager.CurrentScore);
            Assert.AreEqual(1, scoreManager.CurrentCombo);
            Assert.AreEqual(5f, scoreManager.ComboTimer, 0.001f);

            // Trôi qua 2s (còn lại 3s)
            scoreManager.UpdateComboTimer(2f);
            Assert.AreEqual(3f, scoreManager.ComboTimer, 0.001f);

            // Bước 2: Trong 3s còn lại, tiếp tục phá 1 dòng -> lên Combo 2
            // Điểm dòng = 25, Điểm combo = 25 * (2 - 1) = 25 -> Tổng cộng thêm = 50
            // Timer cộng thêm 3s -> 3s + 3s = 6s
            scoreManager.HandleLinesCleared(0, 1, 8);
            Assert.AreEqual(2, scoreManager.CurrentCombo);
            Assert.AreEqual(75, scoreManager.CurrentScore); // 25 + 50 = 75
            Assert.AreEqual(6f, scoreManager.ComboTimer, 0.001f);

            // Trôi qua 1.5s (còn lại 4.5s)
            scoreManager.UpdateComboTimer(1.5f);
            Assert.AreEqual(4.5f, scoreManager.ComboTimer, 0.001f);

            // Bước 3: Tiếp tục phá 1 dòng -> lên Combo 3
            // Điểm dòng = 25, Điểm combo = 25 * (3 - 1) = 50 -> Tổng cộng thêm = 75
            // Timer cộng thêm 3s -> 4.5s + 3s = 7.5s
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.AreEqual(3, scoreManager.CurrentCombo);
            Assert.AreEqual(150, scoreManager.CurrentScore); // 75 + 75 = 150
            Assert.AreEqual(7.5f, scoreManager.ComboTimer, 0.001f);

            // Bước 4: Tiếp tục phá 1 dòng -> lên Combo 4
            // Điểm dòng = 25, Điểm combo = 25 * (4 - 1) = 75 -> Tổng cộng thêm = 100
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.AreEqual(4, scoreManager.CurrentCombo);
            Assert.AreEqual(250, scoreManager.CurrentScore); // 150 + 100 = 250
            Assert.AreEqual(10.5f, scoreManager.ComboTimer, 0.001f);
        }

        [Test]
        public void ComboTimer_WhenExpired_ResetsComboToOneAndIsInactive()
        {
            bool expiredCalled = false;
            scoreManager.OnComboExpired += () => expiredCalled = true;

            // Bắt đầu combo 1 (timer 5s)
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.AreEqual(1, scoreManager.CurrentCombo);
            Assert.IsTrue(scoreManager.IsComboActive);

            // Nối tiếp lên combo 2 (timer 5s + 3s = 8s)
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.AreEqual(2, scoreManager.CurrentCombo);

            // Thời gian trôi qua hết (8.5 giây)
            scoreManager.UpdateComboTimer(8.5f);

            Assert.IsTrue(expiredCalled);
            Assert.IsFalse(scoreManager.IsComboActive);
            Assert.AreEqual(1, scoreManager.CurrentCombo);
            Assert.AreEqual(0f, scoreManager.ComboTimer);
        }

        [Test]
        public void ResetScoreAndCombo_ResetsCurrentScoreToZeroAndComboToOne()
        {
            scoreManager.HandleShapePlacedWithCount(5);
            scoreManager.HandleLinesCleared(1, 0, 8);
            Assert.Greater(scoreManager.CurrentScore, 0);

            scoreManager.ResetScoreAndCombo();

            Assert.AreEqual(0, scoreManager.CurrentScore);
            Assert.AreEqual(1, scoreManager.CurrentCombo);
            Assert.IsFalse(scoreManager.IsComboActive);
            Assert.AreEqual(0f, scoreManager.ComboTimer);
        }

        [Test]
        public void HighScore_UpdatesWhenCurrentScoreExceedsIt()
        {
            int initialHigh = scoreManager.HighScore;
            scoreManager.AddScore(500);

            Assert.GreaterOrEqual(scoreManager.HighScore, 500);
            Assert.AreEqual(500, scoreManager.CurrentScore);
        }

        [Test]
        public void HandleLinesClearedWithPos_Combo1_DoesNotTriggerOnComboTriggered()
        {
            int comboTriggered = 0;
            scoreManager.OnComboTriggered += (c, pos) => comboTriggered = c;

            scoreManager.HandleLinesClearedWithPos(1, 0, 8, new Vector3(1f, 2f, 0f));

            Assert.AreEqual(0, comboTriggered, "Combo 1 should not trigger visual combo effect");
        }

        [Test]
        public void HandleLinesClearedWithPos_Combo2_TriggersOnComboTriggeredWithPosition()
        {
            int comboTriggered = 0;
            Vector3 triggeredPos = Vector3.zero;
            scoreManager.OnComboTriggered += (c, pos) =>
            {
                comboTriggered = c;
                triggeredPos = pos;
            };

            // Lần 1: kích hoạt combo 1
            scoreManager.HandleLinesClearedWithPos(1, 0, 8, new Vector3(1f, 1f, 0f));
            // Lần 2: trong cửa sổ combo -> lên combo 2
            Vector3 targetPos = new Vector3(3f, 4f, 0f);
            scoreManager.HandleLinesClearedWithPos(1, 0, 8, targetPos);

            Assert.AreEqual(2, comboTriggered);
            Assert.AreEqual(targetPos, triggeredPos);
        }
    }
}
