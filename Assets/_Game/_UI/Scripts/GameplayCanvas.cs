using Quang.UI;
using Quang.Common;
using UnityEngine;

namespace UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Quang.Base;
    using Quang.DesignPattern;
    using DG.Tweening;
    using Gameplay.BlockDrag;
    using Solo.MOST_IN_ONE;
    using TMPro;
    using UnityEngine.UI;
    using Quang.Utilities.Timer;

    public class GameplayCanvas : UISCanvas
    {
        [SerializeField] private UIButton _settingBtn;

        [SerializeField] private TMP_Text HightScoreTxt;

        [SerializeField] private TMP_Text ScoreTxt;

        [Header("Score Count-Up")]
        [Tooltip("Thời gian (giây) Score chạy từ số đang hiển thị lên số cuối; 0 = nhảy số ngay")]
        [SerializeField] private float countUpDuration = 1f;

        [Tooltip("Quản lý chữ điểm bay lên Score (để trống sẽ lấy trên cùng GameObject)")]
        [SerializeField] private ScoreFlyManager scoreFlyManager;

        [Header("Background (màu nền Main Camera)")]
        [SerializeField] private Color[] _bgColors;
        [SerializeField] private float _bgFadeDuration = 0.5f;


        protected ToastCanvas toastCanvas;
        private Tween scorePunchTween;
        private Tween bgColorTween;
        private int bgColorIndex;
        private IScoreService boundScore;
        private IBoardSessionService boundSession;
        private readonly ScoreCountUp scoreCounter = new ScoreCountUp();

        void Awake()
        {
            if (_settingBtn != null)
            {
                _settingBtn._OnClick += OnSettingBtnClick;
            }

            if (scoreFlyManager == null)
            {
                scoreFlyManager = GetComponent<ScoreFlyManager>();
            }
            if (scoreFlyManager != null)
            {
                scoreFlyManager.OnFlyArrived += HandleFlyArrived;
            }
        }

        private void Update()
        {
            if (ScoreTxt != null && scoreCounter.Tick(Time.unscaledDeltaTime))
            {
                ScoreTxt.text = scoreCounter.Displayed.ToString();
            }
        }

        protected override void Start()
        {
            base.Start();
            // Canvas được đặt sẵn trong GameScene nên Open() không chạy lúc vào game
            ResetBgColor();
        }

        private void OnEnable()
        {
            Locator.ScoreChanged -= BindScore;
            Locator.ScoreChanged += BindScore;
            BindScore(Locator.Score);

            Locator.BoardSessionChanged -= BindSession;
            Locator.BoardSessionChanged += BindSession;
            BindSession(Locator.BoardSession);
        }

        private void OnDisable()
        {
            Locator.ScoreChanged -= BindScore;
            Locator.BoardSessionChanged -= BindSession;
            UnbindScore();
            UnbindSession();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_settingBtn != null)
            {
                _settingBtn._OnClick -= OnSettingBtnClick;
            }

            if (scoreFlyManager != null)
            {
                scoreFlyManager.OnFlyArrived -= HandleFlyArrived;
            }

            scorePunchTween?.Kill();
            bgColorTween?.Kill();
            Locator.ScoreChanged -= BindScore;
            Locator.BoardSessionChanged -= BindSession;
            UnbindScore();
            UnbindSession();
        }

        public override void Open(object param = null)
        {
            base.Open(param);
            toastCanvas = UIManager.Ins.OpenUI<ToastCanvas>();

            if (ComboEffectManager.Ins == null && GetComponent<ComboEffectManager>() == null)
            {
                gameObject.AddComponent<ComboEffectManager>();
            }

            BindScore(Locator.Score);
            BindSession(Locator.BoardSession);
            ResetBgColor();
        }

        /// <summary>
        /// Lắng nghe Game Over từ ván chơi hiện tại (Locator.BoardSession) để mở LoseCanvas.
        /// Gọi với null khi service bị huỷ để gỡ đăng ký.
        /// </summary>
        private void BindSession(IBoardSessionService session)
        {
            if (ReferenceEquals(session, boundSession)) return;

            UnbindSession();
            if (session == null) return;

            boundSession = session;
            boundSession.OnGameOver += HandleGameOver;
            boundSession.OnGameRestarted += ResetBgColor;
            boundSession.OnGameRestarted += ResetScoreDisplay;
        }

        private void UnbindSession()
        {
            if (boundSession == null) return;

            boundSession.OnGameOver -= HandleGameOver;
            boundSession.OnGameRestarted -= ResetBgColor;
            boundSession.OnGameRestarted -= ResetScoreDisplay;
            boundSession = null;
        }

        private void HandleGameOver()
        {
            UIManager.Ins.OpenUI<LoseCanvas>(false);
        }

        /// <summary>
        /// Đăng ký sự kiện điểm của Locator.Score (kể cả khi service đăng ký sau canvas
        /// hoặc được thay bằng instance mới khi load lại scene) và hiển thị ngay điểm hiện tại
        /// </summary>
        private void BindScore(IScoreService score)
        {
            if (ReferenceEquals(score, boundScore)) return;

            UnbindScore();
            if (score == null) return;

            boundScore = score;
            boundScore.OnScoreChanged += HandleScoreChanged;
            boundScore.OnHighScoreChanged += UpdateHighScoreText;
            boundScore.OnPerfectClear += HandlePerfectClear;

            ResetScoreDisplay();
            UpdateHighScoreText(boundScore.HighScore);
        }

        private void UnbindScore()
        {
            if (boundScore == null) return;

            boundScore.OnScoreChanged -= HandleScoreChanged;
            boundScore.OnHighScoreChanged -= UpdateHighScoreText;
            boundScore.OnPerfectClear -= HandlePerfectClear;
            boundScore = null;
        }

        /// <summary>
        /// Dọn sạch bàn: chuyển mượt màu nền camera gameplay sang màu kế tiếp trong _bgColors (xoay vòng).
        /// Dùng màu nền camera vì UICamera là Overlay camera (URP stack) nên mọi Image trong UI đều che board.
        /// </summary>
        private void HandlePerfectClear(int bonus, Vector3 worldPos)
        {
            Camera cam = Camera.main;
            if (cam == null || _bgColors == null || _bgColors.Length == 0) return;

            bgColorIndex = (bgColorIndex + 1) % _bgColors.Length;
            bgColorTween?.Kill();
            bgColorTween = cam.DOColor(_bgColors[bgColorIndex], _bgFadeDuration);
        }

        /// <summary>
        /// Ván mới: đưa màu nền camera về màu đầu tiên ngay lập tức
        /// </summary>
        private void ResetBgColor()
        {
            bgColorTween?.Kill();
            bgColorIndex = 0;

            Camera cam = Camera.main;
            if (cam != null && _bgColors != null && _bgColors.Length > 0)
            {
                cam.backgroundColor = _bgColors[0];
            }
        }

        /// <summary>
        /// Điểm giảm (ván mới) thì gán ngay; điểm tăng thì chạy số tới điểm thật trừ phần đang bay
        /// </summary>
        private void HandleScoreChanged(int score)
        {
            if (score < scoreCounter.Displayed)
            {
                ResetScoreDisplay();
                return;
            }

            RefreshScoreTarget();
        }

        private void RefreshScoreTarget()
        {
            if (boundScore == null) return;

            int pending = scoreFlyManager != null ? scoreFlyManager.PendingAmount : 0;
            scoreCounter.Duration = countUpDuration;
            scoreCounter.SetTarget(Mathf.Max(0, boundScore.CurrentScore - pending));
        }

        /// <summary>
        /// Chữ điểm bay chạm Score: chạy số lên và nảy chữ Score
        /// </summary>
        private void HandleFlyArrived(int amount)
        {
            RefreshScoreTarget();

            if (ScoreTxt != null)
            {
                scorePunchTween?.Kill();
                ScoreTxt.transform.localScale = Vector3.one;
                scorePunchTween = ScoreTxt.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
            }
        }

        /// <summary>
        /// Huỷ chữ đang bay và hiển thị ngay điểm hiện tại (bind lần đầu, ván mới)
        /// </summary>
        private void ResetScoreDisplay()
        {
            if (scoreFlyManager != null)
            {
                scoreFlyManager.CancelAll();
            }

            int score = boundScore != null ? boundScore.CurrentScore : 0;
            scoreCounter.Snap(score);

            if (ScoreTxt != null)
            {
                ScoreTxt.text = score.ToString();
            }
        }

        private void UpdateHighScoreText(int highScore)
        {
            if (HightScoreTxt != null)
            {
                HightScoreTxt.text = highScore.ToString();
            }
        }

        protected void OnSettingBtnClick(int index)
        {
            Locator.UI.OpenUI<SettingPopup>(1);
        }

    }
}
