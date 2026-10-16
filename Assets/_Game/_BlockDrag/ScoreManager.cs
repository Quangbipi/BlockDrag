using System;
using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Đăng ký vào <see cref="Locator.Score"/> trong Awake để UI truy cập qua IScoreService
    /// </summary>
    public class ScoreManager : MonoBehaviour, IScoreService
    {
        public const string ComboCountdownId = "BLOCK_BLAST_COMBO_TIMER";

        [Header("Configuration")]
        [Tooltip("Asset dữ liệu ScriptableObject chứa thiết lập điểm và combo")]
        [SerializeField] private ScoreConfigSO scoreConfig;

        [Header("References")]
        [Tooltip("Tham chiếu BlockGrid (tự tìm nếu để trống)")]
        [SerializeField] private BlockGrid blockGrid;

        [Tooltip("Tham chiếu BlockSpawner (tự tìm nếu để trống)")]
        [SerializeField] private BlockSpawner blockSpawner;

        // Runtime state
        private int currentScore = 0;
        private int highScore = 0;
        private int currentCombo = 1;
        private float comboTimer = 0f;
        private bool isComboActive = false;
        private bool isHighScoreDirty = false;
        private IHighScoreStore highScoreStore;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<ScoreGain> OnScoreGained; // phát TRƯỚC OnScoreChanged
        public event Action<int> OnHighScoreChanged;
        public event Action<int, int> OnComboChanged; // (comboCount, bonusScoreAwarded)
        public event Action<int, Vector3> OnComboTriggered; // (comboCount, worldPos) khi combo >= 2
        public event Action<int, Vector3> OnPerfectClear;   // (bonus, worldPos) khi dọn sạch bàn
        public event Action<float> OnComboTimerUpdated; // (remainingTime)
        public event Action OnComboExpired;

        public ScoreConfigSO Config
        {
            get
            {
                if (scoreConfig == null)
                {
                    scoreConfig = ScoreConfigSO.CreateDefault();
                }
                return scoreConfig;
            }
            set => scoreConfig = value;
        }

        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public int CurrentCombo => currentCombo;
        public float ComboTimer => comboTimer;
        public bool IsComboActive => isComboActive;
        public BlockGrid Grid
        {
            get => blockGrid;
            set => blockGrid = value;
        }
        public BlockSpawner Spawner
        {
            get => blockSpawner;
            set => blockSpawner = value;
        }
        private IHighScoreStore HighScoreStore => highScoreStore ??= new GameDataHighScoreStore();

        /// <summary>
        /// Thay nơi lưu HighScore (dùng cho tests) và load lại HighScore từ đó
        /// </summary>
        public void SetHighScoreStore(IHighScoreStore store)
        {
            highScoreStore = store;
            LoadHighScore();
        }

        private void Awake()
        {
            if (Locator.Score is ScoreManager other && other != null && other != this)
            {
                Destroy(gameObject);
                return;
            }

            if (scoreConfig == null)
            {
                scoreConfig = ScoreConfigSO.CreateDefault();
            }

            if (blockGrid == null)
            {
                blockGrid = FindObjectOfType<BlockGrid>();
            }

            if (blockSpawner == null)
            {
                blockSpawner = FindObjectOfType<BlockSpawner>();
            }

            LoadHighScore();

            Locator.Score = this;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopComboTimer();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveHighScore();
            }
        }

        private void OnApplicationQuit()
        {
            SaveHighScore();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Locator.Score, this))
            {
                Locator.Score = null;
            }
        }

        private void Start()
        {
            // Đảm bảo CountdownManager tồn tại trong scene nếu chạy PlayMode
            if (CountdownManager.Instance == null && Application.isPlaying)
            {
                GameObject cdObj = new GameObject("CountdownManager");
                cdObj.AddComponent<CountdownManager>();
            }

            OnScoreChanged?.Invoke(currentScore);
            OnHighScoreChanged?.Invoke(highScore);
            OnComboChanged?.Invoke(currentCombo, 0);
        }

        private void Update()
        {
            // Nếu không có CountdownManager hoặc trong chế độ test không chạy coroutine, tự đếm bằng deltaTime
            if (CountdownManager.Instance == null && isComboActive)
            {
                UpdateComboTimer(Time.deltaTime);
            }
        }

        public void SubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnShapePlacedWithCount -= HandleShapePlacedWithCount;
                blockGrid.OnShapePlacedWithCount += HandleShapePlacedWithCount;

                blockGrid.OnLinesClearedWithPos -= HandleLinesClearedWithPos;
                blockGrid.OnLinesClearedWithPos += HandleLinesClearedWithPos;

                blockGrid.OnBoardCleared -= HandlePerfectClear;
                blockGrid.OnBoardCleared += HandlePerfectClear;
            }

            if (blockSpawner != null)
            {
                blockSpawner.OnGameOver -= HandleGameOver;
                blockSpawner.OnGameOver += HandleGameOver;

                blockSpawner.OnGameRestarted -= HandleGameRestarted;
                blockSpawner.OnGameRestarted += HandleGameRestarted;
            }
        }

        public void UnsubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnShapePlacedWithCount -= HandleShapePlacedWithCount;
                blockGrid.OnLinesClearedWithPos -= HandleLinesClearedWithPos;
                blockGrid.OnBoardCleared -= HandlePerfectClear;
            }

            if (blockSpawner != null)
            {
                blockSpawner.OnGameOver -= HandleGameOver;
                blockSpawner.OnGameRestarted -= HandleGameRestarted;
            }
        }

        /// <summary>
        /// Xử lý khi đặt một khối shape thành công xuống Grid:
        /// Cộng điểm bằng số SingleBlock * PointsPerSingleBlock
        /// </summary>
        public void HandleShapePlacedWithCount(int singleBlockCount)
        {
            if (singleBlockCount <= 0) return;

            int points = singleBlockCount * Config.PointsPerSingleBlock;
            AddScore(new ScoreGain(points, ScoreSource.Placement));
        }

        /// <summary>
        /// Xử lý khi có hàng hoặc cột bị phá hủy:
        /// - Mỗi hàng/cột: +25 điểm cơ bản
        /// - Trong 5s nếu phá tiếp -> lên combo và cộng thêm 3s vào đồng hồ đang đếm
        /// - Điểm combo thưởng: +25 * (combo - 1)
        /// </summary>
        public void HandleLinesCleared(int rowsCleared, int colsCleared, int totalCellsCleared)
        {
            ApplyLinesCleared(rowsCleared, colsCleared, null);
        }

        private void ApplyLinesCleared(int rowsCleared, int colsCleared, Vector3? placedPos)
        {
            int linesCleared = rowsCleared + colsCleared;
            if (linesCleared <= 0) return;

            int basePoints = linesCleared * Config.PointsPerLine;
            int comboBonusPoints = 0;

            if (isComboActive && comboTimer > 0f)
            {
                // Đang trong thời gian đếm ngược: tăng cấp combo bằng số hàng/cột vừa phá hủy
                currentCombo += linesCleared;
                comboBonusPoints = (currentCombo - 1) * Config.ComboBonusMultiplier;

                comboTimer += Config.ComboBonusDuration;

                if (CountdownManager.Instance != null && CountdownManager.Instance.IsRunning(ComboCountdownId))
                {
                    CountdownManager.Instance.AddTime(ComboCountdownId, Config.ComboBonusDuration);
                }
            }
            else
            {
                // Bắt đầu chuỗi combo mới: 1 dòng là combo 1, nhiều dòng là combo tương ứng (2 dòng = combo 2, ...)
                currentCombo = linesCleared;
                comboBonusPoints = (currentCombo - 1) * Config.ComboBonusMultiplier;
                comboTimer = Config.InitialComboDuration;
                isComboActive = true;

                StartComboTimer(comboTimer);
            }

            int totalEarned = basePoints + comboBonusPoints;
            AddScore(placedPos.HasValue
                ? new ScoreGain(totalEarned, ScoreSource.LineClear, placedPos.Value)
                : new ScoreGain(totalEarned, ScoreSource.LineClear));

            OnComboChanged?.Invoke(currentCombo, comboBonusPoints);
            OnComboTimerUpdated?.Invoke(comboTimer);
        }

        /// <summary>
        /// Xử lý phá hủy hàng/cột kèm toạ độ tâm của khối shape vừa đặt:
        /// Gọi HandleLinesCleared và nếu combo >= 2 thì phát OnComboTriggered.
        /// </summary>
        public void HandleLinesClearedWithPos(int rowsCleared, int colsCleared, int totalCellsCleared, Vector3 placedPos)
        {
            ApplyLinesCleared(rowsCleared, colsCleared, placedPos);

            if (currentCombo >= 2)
            {
                OnComboTriggered?.Invoke(currentCombo, placedPos);
            }
        }

        /// <summary>
        /// Phá hàng xong bàn trống hoàn toàn: cộng perfectClearBonus (× combo hiện tại nếu bật).
        /// BlockGrid bắn OnBoardCleared sau OnLinesClearedWithPos nên currentCombo đã gồm lần phá này.
        /// </summary>
        public void HandlePerfectClear(Vector3 boardCenter)
        {
            int multiplier = Config.PerfectClearMultiplyByCombo ? Mathf.Max(1, currentCombo) : 1;
            int bonus = Config.PerfectClearBonus * multiplier;
            if (bonus <= 0) return;

            AddScore(new ScoreGain(bonus, ScoreSource.PerfectClear, boardCenter));
            OnPerfectClear?.Invoke(bonus, boardCenter);
        }

        /// <summary>
        /// Bắt đầu đếm ngược thời gian combo bằng CountdownManager
        /// </summary>
        private void StartComboTimer(float duration)
        {
            if (CountdownManager.Instance != null)
            {
                CountdownManager.Instance.RestartCountdown(
                    ComboCountdownId,
                    duration,
                    onComplete: HandleComboExpired,
                    onTick: null,
                    onUpdate: HandleComboTimerTick
                );
            }
        }

        /// <summary>
        /// Dừng đếm ngược combo
        /// </summary>
        private void StopComboTimer()
        {
            if (CountdownManager.Instance != null && CountdownManager.Instance.IsRunning(ComboCountdownId))
            {
                CountdownManager.Instance.StopCountdown(ComboCountdownId);
            }
        }

        private void HandleComboTimerTick(float remainingTime)
        {
            comboTimer = remainingTime;
            OnComboTimerUpdated?.Invoke(remainingTime);
        }

        /// <summary>
        /// Khi hết thời gian đếm ngược combo -> reset cấp combo về 1
        /// </summary>
        public void HandleComboExpired()
        {
            comboTimer = 0f;
            isComboActive = false;
            currentCombo = 1;

            OnComboExpired?.Invoke();
            OnComboChanged?.Invoke(currentCombo, 0);
        }

        /// <summary>
        /// Hàm cập nhật thời gian combo thủ công (dùng cho EditMode tests hoặc fallback Update)
        /// </summary>
        public void UpdateComboTimer(float deltaTime)
        {
            if (!isComboActive) return;

            comboTimer -= deltaTime;
            if (comboTimer <= 0f)
            {
                HandleComboExpired();
            }
            else
            {
                OnComboTimerUpdated?.Invoke(comboTimer);
            }
        }

        /// <summary>
        /// Cộng điểm vào tổng điểm hiện tại và kiểm tra kỷ lục HighScore
        /// </summary>
        public void AddScore(int amount)
        {
            AddScore(new ScoreGain(amount, ScoreSource.Placement));
        }

        /// <summary>
        /// Cộng điểm kèm nguồn và toạ độ để UI hiển thị chữ điểm bay
        /// </summary>
        public void AddScore(ScoreGain gain)
        {
            if (gain.Amount <= 0) return;

            currentScore += gain.Amount;
            OnScoreGained?.Invoke(gain);
            OnScoreChanged?.Invoke(currentScore);

            if (currentScore > highScore)
            {
                highScore = currentScore;
                isHighScoreDirty = true;
                OnHighScoreChanged?.Invoke(highScore);
            }
        }

        /// <summary>
        /// Reset điểm hiện tại về 0 và combo về 1 (khi bắt đầu ván mới)
        /// </summary>
        public void ResetScoreAndCombo()
        {
            currentScore = 0;
            currentCombo = 1;
            comboTimer = 0f;
            isComboActive = false;

            StopComboTimer();

            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(currentCombo, 0);
        }

        private void HandleGameOver()
        {
            SaveHighScore();
            StopComboTimer();
        }

        private void HandleGameRestarted()
        {
            SaveHighScore();
            ResetScoreAndCombo();
        }

        /// <summary>
        /// Ghi HighScore vào GameData (qua DataManager) nếu có kỷ lục mới chưa được lưu
        /// </summary>
        public void SaveHighScore()
        {
            if (currentScore > highScore)
            {
                highScore = currentScore;
                isHighScoreDirty = true;
                OnHighScoreChanged?.Invoke(highScore);
            }

            if (!isHighScoreDirty) return;

            HighScoreStore.Save(highScore);
            isHighScoreDirty = false;
        }

        private void LoadHighScore()
        {
            highScore = HighScoreStore.Load();
            isHighScoreDirty = false;
            OnHighScoreChanged?.Invoke(highScore);
        }
    }
}
