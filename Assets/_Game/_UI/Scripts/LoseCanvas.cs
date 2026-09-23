using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using _Game.Managers;
    using Base;
    using Base.UI;
    using Common;
    using System;
    using System.Data;
    using TMPro;
    using Utilities.Timer;

    public class LoseCanvas : UISCanvas
    {
        [SerializeField]
        UIButton tryAgainButton;
        [SerializeField]
        UIButton goHomeButton;
        [SerializeField]
        UIButton nextLevelButton;
        [SerializeField]
        TMP_Text levelTxt;
        [SerializeField]
        ListItemShow starList;
        [SerializeField]
        RectTransform startGoldTf;
        [SerializeField]
        TMP_Text titleTxt;
        [SerializeField]
        TMP_Text descriptionTxt;
        [SerializeField]
        TMP_Text blockGoldTxt;
        List<float> loadingTimes;
        List<Action> loadingActions;
        List<Action> loadingAction2s;
        GameData gameData;
        GameplayData gameplayData;
        bool isWin = false;
        TopCanvas topCanvas;
        int BASE_GOLD = 0;
        bool isNextLevel = false;
        private void Awake()
        {
            tryAgainButton._OnClick += OnTryAgain;
            goHomeButton._OnClick += OnGoHome;
            nextLevelButton._OnClick += OnNextLevel;

            loadingTimes = new List<float>() { 1.2f, 1.5f };
            loadingActions = new List<Action>()
            {
                () =>
                {
                    if (isNextLevel)
                    {
                        Locator.Gameplay.DestructLevel();
                        Locator.Gameplay.ConstructLevel(gameData.user.normalLevelIndex);
                    }
                    else
                    {
                        GameEventManager.Ins.PostEvent(DesignPattern.EventID.RECONSTUCT_LEVEL);
                    }
                    UIManager.Ins.OpenUI<GameplayCanvas>();
                },
                () => UIManager.Ins.GetUI<LoadingCanvas>().Close(),
            };
            loadingAction2s = new List<Action>()
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
            gameData = Locator.Data.GetData<GameData>();
            gameplayData = Locator.Data.GetSOData<GameplayData>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            tryAgainButton._OnClick -= OnTryAgain;
            goHomeButton._OnClick -= OnGoHome;
            nextLevelButton._OnClick -= OnNextLevel;
        }
        public override void Open(object param)
        {
            base.Open(param);
            isNextLevel = false;
            isWin = (bool)param;
            Locator.Audio.PlaySfx(SFX_TYPE.LOSE);
            Locator.Ads.Analytic.LevelTrackEvent(LEVEL_STATE.FAIL, gameData.user.normalLevelIndex);
            topCanvas = UIManager.Ins.OpenUI<TopCanvas>();
            starList.SetMaxItems(3);
            starList.ActiveItems(gameData.level.Star, 1);
            BASE_GOLD = gameplayData.WinLevelGold * gameData.level.DeltaStar;

            if (isWin)
            {
                goHomeButton.gameObject.SetActive(false);
                nextLevelButton.gameObject.SetActive(true);
                nextLevelButton.SetData($"{BASE_GOLD}");
                blockGoldTxt.text = (gameplayData.WinLevelGold
                * (3 - gameData.level.LevelStars[gameData.user.normalLevelIndex])).ToString();
                titleTxt.text = $"Level Complete!";
                descriptionTxt.text = $"Earn 3 stars to recieve full reward!";
            }
            else
            {
                goHomeButton.gameObject.SetActive(true);
                nextLevelButton.gameObject.SetActive(false);
                titleTxt.text = $"No Stars Earn";
                descriptionTxt.text = "You will not receive any reward!";
            }
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            levelTxt.text = $"Level {gameData.user.normalLevelIndex + 1}";
        }
        protected void OnTryAgain(int index)
        {
            if (Locator.Reward.GetItem(ITEM.HEART).Quantity > 0)
            {
                Close();
                isNextLevel = true;
                if (isWin)
                {
                    gameData.user.normalLevelIndex -= 1;
                }
                UIManager.Ins.CloseUI<TopCanvas>();
                Locator.Ads.Inter.Show(Loading);
            }
            else
            {
                UIManager.Ins.OpenUI<BuyBoosterPopup>(gameplayData.Items[ITEM.HEART]);
            }

            void Loading()
            {
                UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingActions });
            }
        }

        protected void OnGoHome(int index)
        {
            Locator.Ads.Inter.Show(Loading);
            UIManager.Ins.CloseUI<TopCanvas>();
            void Loading()
            {
                UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingAction2s });
            }
        }
        protected void OnNextLevel(int index)
        {
            Locator.Reward.ClaimItem(ITEM.GOLD, BASE_GOLD);
            topCanvas.FlyGoldReward(startGoldTf.position, BASE_GOLD);
            Database.Save(gameData);
            isNextLevel = true;
            TimerManager.Ins.WaitForTime(1.5f, () =>
                {
                    Close();
                    UIManager.Ins.CloseUI<TopCanvas>();
                    if ((gameData.TotalStar >= gameData.user.normalLevelIndex / gameplayData.LevelPerChapter * 3 * gameplayData.LevelPerChapter) 
                    && Locator.Reward.GetItem(ITEM.HEART).Quantity > 0)
                    {
                        Locator.Ads.Inter.Show(Loading);
                        void Loading()
                        {
                            UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingActions });
                        }
                    }
                    else
                    {
                        OnGoHome(index);
                    }

                });

        }
    }
}
