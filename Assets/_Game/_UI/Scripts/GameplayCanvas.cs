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
        private ScoreManager boundScoreManager;
        private BlockSpawner boundBlockSpawner;

        void Awake()
        {
            if (_settingBtn != null)
            {
                _settingBtn._OnClick += OnSettingBtnClick;
            }
        }

        private void OnEnable()
        {
            ScoreManager.OnInstanceReady -= BindScoreManager;
            ScoreManager.OnInstanceReady += BindScoreManager;
            BindScoreManager(ScoreManager.Ins);

            BlockSpawner.OnInstanceReady -= BindBlockSpawner;
            BlockSpawner.OnInstanceReady += BindBlockSpawner;
            BindBlockSpawner(BlockSpawner.Ins);
        }

        private void OnDisable()
        {
            ScoreManager.OnInstanceReady -= BindScoreManager;
            BlockSpawner.OnInstanceReady -= BindBlockSpawner;
            UnbindScoreManager();
            UnbindBlockSpawner();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_settingBtn != null)
            {
                _settingBtn._OnClick -= OnSettingBtnClick;
            }

            scorePunchTween?.Kill();
            ScoreManager.OnInstanceReady -= BindScoreManager;
            BlockSpawner.OnInstanceReady -= BindBlockSpawner;
            UnbindScoreManager();
            UnbindBlockSpawner();
        }

        public override void Open(object param = null)
        {
            base.Open(param);
            toastCanvas = UIManager.Ins.OpenUI<ToastCanvas>();

            if (ComboEffectManager.Ins == null && GetComponent<ComboEffectManager>() == null)
            {
                gameObject.AddComponent<ComboEffectManager>();
            }

            BindScoreManager(ScoreManager.Ins);
            BindBlockSpawner(BlockSpawner.Ins);
        }

        /// <summary>
        /// Lắng nghe Game Over từ BlockSpawner để mở LoseCanvas
        /// </summary>
        private void BindBlockSpawner(BlockSpawner blockSpawner)
        {
            if (blockSpawner == null || blockSpawner == boundBlockSpawner) return;

            UnbindBlockSpawner();
            boundBlockSpawner = blockSpawner;
            boundBlockSpawner.OnGameOver += HandleGameOver;
        }

        private void UnbindBlockSpawner()
        {
            if (boundBlockSpawner == null) return;

            boundBlockSpawner.OnGameOver -= HandleGameOver;
            boundBlockSpawner = null;
        }

        private void HandleGameOver()
        {
            UIManager.Ins.OpenUI<LoseCanvas>(false);
        }

        /// <summary>
        /// Đăng ký sự kiện điểm của ScoreManager (kể cả khi ScoreManager được tạo sau canvas
        /// hoặc được thay bằng instance mới khi load lại scene) và hiển thị ngay điểm hiện tại
        /// </summary>
        private void BindScoreManager(ScoreManager scoreManager)
        {
            if (scoreManager == null || scoreManager == boundScoreManager) return;

            UnbindScoreManager();
            boundScoreManager = scoreManager;
            boundScoreManager.OnScoreChanged += UpdateScoreText;
            boundScoreManager.OnHighScoreChanged += UpdateHighScoreText;

            if (ScoreTxt != null)
            {
                ScoreTxt.text = boundScoreManager.CurrentScore.ToString();
            }
            UpdateHighScoreText(boundScoreManager.HighScore);
        }

        private void UnbindScoreManager()
        {
            if (boundScoreManager == null) return;

            boundScoreManager.OnScoreChanged -= UpdateScoreText;
            boundScoreManager.OnHighScoreChanged -= UpdateHighScoreText;
            boundScoreManager = null;
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
