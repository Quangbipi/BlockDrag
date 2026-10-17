using Gameplay.BlockDrag;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Quang.Data;
    using Quang.Base;
    using Quang.UI;
    using Quang.Utilities.Timer;
    using System;
    using System.Data;
    using TMPro;
    using DG.Tweening;

    public class LoseCanvas : UISCanvas
    {
        [SerializeField]
        UIButton tryAgainButton;
        [SerializeField]
        UIButton goHomeButton;
        [SerializeField]
        UIButton nextLevelButton;
        [SerializeField]
        TMP_Text scoreTxt;
        [SerializeField]
        TMP_Text highScoreTxt;
        [SerializeField]
        float scoreCountDuration = 0.5f;
        Tween scoreTween;

        
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
                        GameEventManager.Ins.PostEvent(Quang.DesignPattern.EventID.RECONSTUCT_LEVEL);
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
            KillScoreTween();
            tryAgainButton._OnClick -= OnTryAgain;
            goHomeButton._OnClick -= OnGoHome;
            nextLevelButton._OnClick -= OnNextLevel;
        }
        public override void Open(object param)
        {
            base.Open(param);
            isNextLevel = false;
            isWin = (bool)param;
            //Locator.Audio.PlaySfx(SFX_TYPE.LOSE);
            //Locator.Analytics?.LevelTrackEvent(LEVEL_STATE.FAIL, gameData.user.normalLevelIndex, gameData.level.Star, false, false);
            //topCanvas = UIManager.Ins.OpenUI<TopCanvas>();
            // starList.SetMaxItems(3);
            // starList.ActiveItems(gameData.level.Star, 1);
            //BASE_GOLD = gameplayData.WinLevelGold * gameData.level.DeltaStar;

            if (isWin)
            {
                goHomeButton.gameObject.SetActive(false);
                nextLevelButton.gameObject.SetActive(true);
                nextLevelButton.SetData($"{BASE_GOLD}");
                blockGoldTxt.text = (gameplayData.WinLevelGold
                * (3 - gameData.level.LevelStars[gameData.user.normalLevelIndex])).ToString();
                // titleTxt.text = $"Level Complete!";
                // descriptionTxt.text = $"Earn 3 stars to recieve full reward!";
            }
            else
            {
                goHomeButton.gameObject.SetActive(true);
                nextLevelButton.gameObject.SetActive(false);
                // titleTxt.text = $"No Stars Earn";
                // descriptionTxt.text = "You will not receive any reward!";
            }
            ShowScore();
        }
        protected override void OnClose()
        {
            base.OnClose();
            KillScoreTween();
        }
        /// <summary>
        /// Hiện HighScore ngay và chạy điểm ván vừa chơi tăng dần từ 0
        /// </summary>
        void ShowScore()
        {
            KillScoreTween();
            IScoreService score = Locator.Score;
            int finalScore = score != null ? score.CurrentScore : 0;
            // Max để đúng cả khi ScoreManager chưa kịp SaveHighScore lúc Game Over
            int highScore = score != null ? Mathf.Max(finalScore, score.HighScore) : 0;

            highScoreTxt.text = highScore.ToString();
            scoreTxt.text = "0";
            if (finalScore <= 0) return;

            int shown = 0;
            scoreTween = DOTween.To(() => shown, v =>
                {
                    shown = v;
                    scoreTxt.SetText("{0}", v);
                }, finalScore, scoreCountDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this);
        }
        void KillScoreTween()
        {
            scoreTween?.Kill();
            scoreTween = null;
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            //levelTxt.text = $"Level {gameData.user.normalLevelIndex + 1}";
            
        }
        /// <summary>
        /// Chơi lại ván mới (giống nút Replay của GameOverUI cũ)
        /// </summary>
        protected void OnTryAgain(int index)
        {
            Close();
            UIManager.Ins.CloseUI<TopCanvas>();

            Locator.BoardSession?.RestartGame();
        }

        protected void OnGoHome(int index)
        {
            // Locator.Ads.Inter.Show(Loading, Placement.NONE);
            // UIManager.Ins.CloseUI<TopCanvas>();
            // void Loading()
            // {
            //     UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingAction2s });
            // }
            Close();
            Locator.UI.OpenUI<HomeCanvas>();
        }
        protected void OnNextLevel(int index)
        {
            Locator.Items.ClaimItem(ITEM.GOLD.ToItemId(), BASE_GOLD);
            //topCanvas.FlyGoldReward(startGoldTf.position, BASE_GOLD);
            Database.Save(gameData);
            isNextLevel = true;
            TimerManager.Ins.WaitForTime(1.5f, () =>
                {
                    Close();
                    UIManager.Ins.CloseUI<TopCanvas>();
                    if ((gameData.TotalStar >= gameData.user.normalLevelIndex / gameplayData.LevelPerChapter * 3 * gameplayData.LevelPerChapter) 
                    && Locator.Items.GetItem(ITEM.HEART.ToItemId()).Quantity > 0)
                    {
                        Locator.Ads.Inter.Show(Loading, Placement.NONE);
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
