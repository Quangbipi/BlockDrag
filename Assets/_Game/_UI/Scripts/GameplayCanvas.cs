using Base.UI;
using Common;
using UnityEngine;

namespace UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Base;
    using DesignPattern;
    using DG.Tweening;
    using Gameplay.BlockDrag;
    using Solo.MOST_IN_ONE;
    using TMPro;
    using UnityEngine.UI;
    using Utilities.Timer;

    public class GameplayCanvas : UISCanvas
    {
        [SerializeField] private UIButton _settingBtn;

        [SerializeField] private TMP_Text HightScoreTxt;

        [SerializeField] private TMP_Text ScoreTxt;


        protected ToastCanvas toastCanvas;
        private Tween scorePunchTween;

        void Awake()
        {
            if (_settingBtn != null)
            {
                _settingBtn._OnClick += OnSettingBtnClick;
            }
        }

        private void OnEnable()
        {
            SubscribeScoreEvents();
        }

        private void OnDisable()
        {
            UnsubscribeScoreEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_settingBtn != null)
            {
                _settingBtn._OnClick -= OnSettingBtnClick;
            }

            scorePunchTween?.Kill();
            UnsubscribeScoreEvents();
        }

        public override void Open(object param = null)
        {
            base.Open(param);
            toastCanvas = UIManager.Ins.OpenUI<ToastCanvas>();

            if (ComboEffectManager.Ins == null && GetComponent<ComboEffectManager>() == null)
            {
                gameObject.AddComponent<ComboEffectManager>();
            }

            SubscribeScoreEvents();

            if (ScoreManager.Ins != null)
            {
                UpdateScoreText(ScoreManager.Ins.CurrentScore);
                UpdateHighScoreText(ScoreManager.Ins.HighScore);
            }
        }

        private void SubscribeScoreEvents()
        {
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnScoreChanged -= UpdateScoreText;
                ScoreManager.Ins.OnScoreChanged += UpdateScoreText;

                ScoreManager.Ins.OnHighScoreChanged -= UpdateHighScoreText;
                ScoreManager.Ins.OnHighScoreChanged += UpdateHighScoreText;
            }
        }

        private void UnsubscribeScoreEvents()
        {
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnScoreChanged -= UpdateScoreText;
                ScoreManager.Ins.OnHighScoreChanged -= UpdateHighScoreText;
            }
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
