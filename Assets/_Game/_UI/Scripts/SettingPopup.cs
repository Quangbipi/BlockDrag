using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base;
    using Base.UI;
    using Common;
    using System;

    public class SettingPopup : BasePopup
    {
        int state;
        [SerializeField]
        UIButton resumeButton;
        [SerializeField]
        UIButton homeButton;
        [SerializeField]
        GameObject bottomGroup;
        [SerializeField]
        UIButton[] functionButtons;

        GameData.SettingData settingData;

        List<float> loadingTimes;
        List<Action> loadingActions;
        List<Action> confirmActions;

        private void Awake()
        {
            settingData = Locator.Data.GetData<GameData>().setting;

            resumeButton._OnClick += OnResumeButtonClick;
            homeButton._OnClick += OnHomeButtonClick;
            for (int i = 0; i < functionButtons.Length; i++)
            {
                functionButtons[i]._OnClick += OnFunctionButtonClick;
            }

            loadingTimes = new List<float>() { 1.2f, 1.5f };
            loadingActions = new List<Action>()
            {
                () =>
                {
                    Locator.Gameplay.DestructLevel();
                    Locator.Gameplay.ConstructLevel(-1);
                    UIManager.Ins.CloseAll();
                    UIManager.Ins.OpenUI<MainMenuCanvas>();
                },
                () => UIManager.Ins.GetUI<LoadingCanvas>().Close()
            };
            confirmActions = new List<Action>()
            {
                OnGoHome
            };
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            resumeButton._OnClick -= OnResumeButtonClick;
            homeButton._OnClick -= OnHomeButtonClick;
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
            switch (state)
            {
                case 0:
                    homeButton.gameObject.SetActive(false);
                    resumeButton.gameObject.SetActive(false);
                    bottomGroup.SetActive(false);
                    break;
                case 1:
                    homeButton.gameObject.SetActive(true);
                    resumeButton.gameObject.SetActive(true);
                    bottomGroup.SetActive(true);
                    break;
            }
        }

        protected void OnFunctionButtonClick(int index)
        {
            switch (index)
            {
                case 0:
                    settingData.isSfxMute = !settingData.isSfxMute;
                    Locator.Audio.ToggleSfxVolume(settingData.isSfxMute);
                    break;
                case 1:
                    settingData.isBgmMute = !settingData.isBgmMute;
                    Locator.Audio.ToggleBgmVolume(settingData.isBgmMute);
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

        protected void OnHomeButtonClick(int index)
        {
            UIManager.Ins.OpenUI<ConfirmPopup>(confirmActions);
            Close();
        }

        protected void OnGoHome()
        {
            Locator.Ads.Inter.Show(Loading);
            void Loading()
            {
                UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingActions });
            }

        }
    }
}
