using System;
using UnityEngine;
using Utilities.Timer;

namespace Gameplay.BlockDrag
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Ins { get; private set; }

        /// <summary>
        /// Phát khi một ScoreManager vừa sẵn sàng (cuối Awake), cho UI mở trước đó đăng ký lại
        /// </summary>
        public static event Action<ScoreManager> OnInstanceReady;

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
        public event Action<int> OnHighScoreChanged;
        public event Action<int, int> OnComboChanged; // (comboCount, bonusScoreAwarded)
        public event Action<int, Vector3> OnComboTriggered; // (comboCount, worldPos) khi combo >= 2
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
            if (Ins != null && Ins != this)
            {
                Destroy(gameObject);
                return;
            }
            Ins = this;

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

            OnInstanceReady?.Invoke(this);
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
            if (Ins == this)
            {
                Ins = null;
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
            AddScore(points);
        }

        /// <summary>
        /// Xử lý khi có hàng hoặc cột bị phá hủy:
        /// - Mỗi hàng/cột: +25 điểm cơ bản
        /// - Trong 5s nếu phá tiếp -> lên combo và cộng thêm 3s vào đồng hồ đang đếm
        /// - Điểm combo thưởng: +25 * (combo - 1)
        /// </summary>
        public void HandleLinesCleared(int rowsCleared, int colsCleared, int totalCellsCleared)
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
            AddScore(totalEarned);

            OnComboChanged?.Invoke(currentCombo, comboBonusPoints);
            OnComboTimerUpdated?.Invoke(comboTimer);
        }

        /// <summary>
        /// Xử lý phá hủy hàng/cột kèm toạ độ tâm của khối shape vừa đặt:
        /// Gọi HandleLinesCleared và nếu combo >= 2 thì phát OnComboTriggered.
        /// </summary>
        public void HandleLinesClearedWithPos(int rowsCleared, int colsCleared, int totalCellsCleared, Vector3 placedPos)
        {
            HandleLinesCleared(rowsCleared, colsCleared, totalCellsCleared);

            if (currentCombo >= 2)
            {
                OnComboTriggered?.Invoke(currentCombo, placedPos);
            }
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
            if (amount <= 0) return;

            currentScore += amount;
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
