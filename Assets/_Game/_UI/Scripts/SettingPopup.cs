using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    using Quang.Base;
    using Quang.UI;
    using Quang.Common;
    using System;

    public class SettingPopup : BasePopup
    {
        int state;
        [SerializeField]
        UIButton resumeButton;
        [SerializeField]
        UIButton homeButton;
        [SerializeField]
        UIButton restartButton;
        [SerializeField]
        GameObject bottomGroup;
        [SerializeField]
        UIButton[] functionButtons;
        [Tooltip("Panel co giãn theo nội dung (ContentSizeFitter), rebuild layout khi ẩn/hiện bottomGroup")]
        [SerializeField]
        RectTransform panel;

        GameData.SettingData settingData;

        private void Awake()
        {
            settingData = Locator.Data.GetData<GameData>().setting;

            resumeButton._OnClick += OnResumeButtonClick;
            homeButton._OnClick += OnHomeButtonClick;
            restartButton._OnClick += OnRestartButtonClick;
            for (int i = 0; i < functionButtons.Length; i++)
            {
                functionButtons[i]._OnClick += OnFunctionButtonClick;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            resumeButton._OnClick -= OnResumeButtonClick;
            homeButton._OnClick -= OnHomeButtonClick;
            restartButton._OnClick -= OnRestartButtonClick;
            for (int i = 0; i < functionButtons.Length; i++)
            {
                functionButtons[i]._OnClick -= OnFunctionButtonClick;
            }
        }
        public override void Open(object param)
        {
            base.Open(param);
            state = (int)param;
            SetupState(state);
        }

        public override void UpdateUI()
        {
            base.UpdateUI();
            functionButtons[0].SetState(BoolToState(settingData.isSfxMute));
            functionButtons[1].SetState(BoolToState(settingData.isBgmMute));
            functionButtons[2].SetState(BoolToState(settingData.hapticOff));

            UIButton.STATE BoolToState(bool value)
            {
                switch (value)
                {
                    case true:
                        return UIButton.STATE.OPENING;
                    case false:
                        return UIButton.STATE.SELECTING;
                }
            }

        }
        protected void SetupState(int state)
        {
            // 0: mở từ Home (chỉ toggle), 1: pause trong Gameplay (Resume / Restart / Home)
            bottomGroup.SetActive(state == 1);
            if (panel != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            }
        }

        protected void OnFunctionButtonClick(int index)
        {
            switch (index)
            {
                case 0:
                    settingData.isSfxMute = !settingData.isSfxMute;
                    Locator.Audio?.ToggleSfxVolume(settingData.isSfxMute);
                    break;
                case 1:
                    settingData.isBgmMute = !settingData.isBgmMute;
                    Locator.Audio?.ToggleBgmVolume(settingData.isBgmMute);
                    break;
                case 2:
                    settingData.hapticOff = !settingData.hapticOff;
                    break;
            }
            UpdateUI();
        }
        protected void OnResumeButtonClick(int index)
        {
            Close();
        }

        protected void OnRestartButtonClick(int index)
        {
            Close();
            Locator.BoardSession?.RestartGame();
        }

        protected void OnHomeButtonClick(int index)
        {
            Close();
            Locator.UI.OpenUI<HomeCanvas>();
        }
    }
}
