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


        protected ToastCanvas toastCanvas;
        private Tween scorePunchTween;
        private IScoreService boundScore;
        private IBoardSessionService boundSession;

        void Awake()
        {
            if (_settingBtn != null)
            {
                _settingBtn._OnClick += OnSettingBtnClick;
            }
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

            scorePunchTween?.Kill();
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
        }

        private void UnbindSession()
        {
            if (boundSession == null) return;

            boundSession.OnGameOver -= HandleGameOver;
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
            boundScore.OnScoreChanged += UpdateScoreText;
            boundScore.OnHighScoreChanged += UpdateHighScoreText;

            if (ScoreTxt != null)
            {
                ScoreTxt.text = boundScore.CurrentScore.ToString();
            }
            UpdateHighScoreText(boundScore.HighScore);
        }

        private void UnbindScore()
        {
            if (boundScore == null) return;

            boundScore.OnScoreChanged -= UpdateScoreText;
            boundScore.OnHighScoreChanged -= UpdateHighScoreText;
            boundScore = null;
        }

        private void UpdateScoreText(int score)
        {
            if (ScoreTxt != null)
            {
                ScoreTxt.text = score.ToString();
                scorePunchTween?.Kill();
                ScoreTxt.transform.localScale = Vector3.one;
                scorePunchTween = ScoreTxt.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
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
            //UIManager.Ins.OpenUI<SettingPopup>();
        }

    }
}
