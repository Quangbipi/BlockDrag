using Gameplay.BlockDrag;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Quang.Common;
    using TMPro;

    using Quang.UI;
    using Quang.Base;
    using Quang.Base.Init;
    using System;
    using PolyAndCode.UI;
    using DG.Tweening;
    using Quang.Utilities.Timer;
    using Quang.Utilities;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Spine.Unity;

    public class HomeCanvas : UISCanvas
    {
        [SerializeField]
        UIButton startButton;
        [SerializeField]
        UIButton settingButton;
        [SerializeField]
        TMP_Text highScoreTxt;
        [Tooltip("Thời gian (giây) HighScore chạy từ 0 lên số cuối mỗi lần Home mở")]
        [SerializeField]
        float highScoreCountDuration = 0.5f;

        readonly ScoreCountUp highScoreCounter = new ScoreCountUp();

        void Awake()
        {
            startButton._OnClick += OnStartGame;
            settingButton._OnClick += OnSetting;
        }

        void OnEnable()
        {
            // Canvas được bật mỗi lần Open/Show → chạy lại HighScore từ 0
            highScoreCounter.Duration = highScoreCountDuration;
            highScoreCounter.Snap(0);
            highScoreTxt.text = "0";
            highScoreCounter.SetTarget(Locator.Score != null ? Locator.Score.HighScore : 0);
        }

        void Update()
        {
            if (highScoreCounter.Tick(Time.unscaledDeltaTime))
            {
                highScoreTxt.text = highScoreCounter.Displayed.ToString();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            startButton._OnClick -= OnStartGame;
            settingButton._OnClick -= OnSetting;
        }

        private void OnSetting(int obj)
        {
            // TODO: mở SettingPopup khi có
        }

        private void OnStartGame(int obj)
        {
            Close();
        }
    }
}
