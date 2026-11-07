using System.Collections.Generic;
using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Event gameplay gửi qua Locator.Analytics: thua ván (game_lose) và mốc combo chia hết cho 5 (combo_milestone).
    /// </summary>
    [AddComponentMenu("Gameplay/Block Drag Analytics")]
    public class BlockDragAnalytics : MonoBehaviour
    {
        public const string GameLoseEvent = "game_lose";
        public const string ComboMilestoneEvent = "combo_milestone";
        public const string ParamScore = "score";
        public const string ParamHighScore = "high_score";
        public const string ParamCombo = "combo";

        [Tooltip("Tham chiếu ScoreManager (tự tìm nếu để trống)")]
        [SerializeField] private ScoreManager scoreManager;

        [Tooltip("Tham chiếu BlockSpawner (tự tìm nếu để trống)")]
        [SerializeField] private BlockSpawner blockSpawner;

        private readonly ComboMilestoneTracker comboTracker = new ComboMilestoneTracker();
        private readonly List<int> milestoneBuffer = new List<int>();

        private void Awake()
        {
            if (scoreManager == null)
            {
                scoreManager = FindObjectOfType<ScoreManager>();
            }
            if (blockSpawner == null)
            {
                blockSpawner = FindObjectOfType<BlockSpawner>();
            }
        }

        private void OnEnable()
        {
            if (scoreManager != null)
            {
                scoreManager.OnComboChanged += HandleComboChanged;
            }
            if (blockSpawner != null)
            {
                blockSpawner.OnGameOver += HandleGameOver;
            }
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.OnComboChanged -= HandleComboChanged;
            }
            if (blockSpawner != null)
            {
                blockSpawner.OnGameOver -= HandleGameOver;
            }
        }

        private void HandleComboChanged(int combo, int bonus)
        {
            comboTracker.Update(combo, milestoneBuffer);
            for (int i = 0; i < milestoneBuffer.Count; i++)
            {
                LogComboMilestone(Locator.Analytics, milestoneBuffer[i], scoreManager.CurrentScore);
            }
        }

        private void HandleGameOver()
        {
            if (scoreManager == null) return;
            int score = scoreManager.CurrentScore;
            LogGameLose(Locator.Analytics, score, Mathf.Max(scoreManager.HighScore, score));
        }

        public static void LogGameLose(IAnalyticsService analytics, int score, int highScore)
        {
            analytics?.LogEvent(GameLoseEvent, AnalyticsCategory.Design, new Dictionary<string, object>
            {
                { ParamScore, score },
                { ParamHighScore, highScore }
            });
        }

        public static void LogComboMilestone(IAnalyticsService analytics, int combo, int score)
        {
            analytics?.LogEvent(ComboMilestoneEvent, AnalyticsCategory.Design, new Dictionary<string, object>
            {
                { ParamCombo, combo },
                { ParamScore, score }
            });
        }
    }
}
