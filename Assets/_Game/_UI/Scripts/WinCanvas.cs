using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base;
    using Base.UI;
    using Common;
    using DG.Tweening;
    using Spine;
    using System;
    using TMPro;
    using UnityEngine.UI;
    using Utilities.Timer;

    public class WinCanvas : UISCanvas
    {
        public readonly List<int> REWARD_BOUND = new List<int>() { -330, -200, -65, 65, 200, 330 };
        public readonly List<float> REWARD_AMPLI_RATE = new List<float> { 2f, 3f, 5f, 3f, 2f };
        [SerializeField]
        List<UIButton> nextLevelBtns;
        [SerializeField]
        TMP_Text levelText;
        [SerializeField]
        TMP_Text goldAmpliText;
        [SerializeField]
        List<Transform> startFlyGoldTfs;
        [SerializeField]
        ListItemShow starList;
        [SerializeField]
        Image fxImage;
        [SerializeField]
        List<UIPack> iapPacks;
        GameData.ItemData goldData;
        GameData gameData;
        GameplayData gameplayData;
        //IAPData iapData;
        Tweener tween;

        int currentReceiveGold;
        int BASE_GOLD;
        int levelShow;
        Transform goldFlyTf;

        List<float> loadingTimes;
        List<Action> loadingActions;
        List<Action> loadingAction2s;
        bool isClaimGold = false;
        TopCanvas topCanvas;
        STimer timer;
        List<Action> animActions;
        List<float> animTimes;
        bool isShowPack = false;
        protected void Awake()
        {
            gameData = Locator.Data.GetData<GameData>();
            gameplayData = Locator.Data.GetSOData<GameplayData>();
            //iapData = Locator.Data.GetSOData<IAPData>();
            goldData = gameData.GetItemData(ITEM.GOLD);
            for (int i = 0; i < nextLevelBtns.Count; i++)
            {
                nextLevelBtns[i]._OnClick += OnNextLevelClick;
            }
            loadingTimes = new List<float>() { 1.2f, 1.5f };
            loadingActions = new List<Action>()
            {
                () =>
                {
                    Locator.Gameplay.DestructLevel();
                    Locator.Gameplay.ConstructLevel(gameData.user.normalLevelIndex);
                    UIManager.Ins.OpenUI<GameplayCanvas>();
                },
                () =>
                {
                    UIManager.Ins.CloseUI<WinCanvas>();
                    UIManager.Ins.GetUI<LoadingCanvas>().Close();
                }
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
            timer = TimerManager.Ins.PopSTimer();
            animActions = new List<Action>();
            animTimes = new List<float>();

            float currentTime = 0.5f;
            for (int i = 0; i < 3; i++)
            {
                int sound = (int)SFX_TYPE.STAR_POP_1 + i;
                animTimes.Add(currentTime);
                animActions.Add(() => ShowStar(sound));
                currentTime += 0.3f;
            }
            for(int i = 0; i < iapPacks.Count; i++)
            {
                iapPacks[i]._OnItemPurchased += OnBuyIapPack;
            }

            void ShowStar(int sound)
            {
                starList.Add();
                Locator.Audio.PlaySfx((SFX_TYPE)sound);
            }
        }

        protected void OnDisable()
        {
            tween.Kill();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < nextLevelBtns.Count; i++)
            {
                nextLevelBtns[i]._OnClick -= OnNextLevelClick;
            }
             for(int i = 0; i < iapPacks.Count; i++)
            {
                iapPacks[i]._OnItemPurchased -= OnBuyIapPack;
            }
        }
        public override void Open(object param)
        {
            levelShow = gameData.user.normalLevelIndex + 1;
            BASE_GOLD = gameplayData.WinLevelGold * gameData.level.DeltaStar;
            currentReceiveGold = BASE_GOLD;
            base.Open(param);
            Locator.Audio.PlaySfx(SFX_TYPE.WIN);
            Locator.Ads.Analytic.LevelTrackEvent(LEVEL_STATE.COMPLETE, gameData.user.normalLevelIndex - 1);
            // StartAmpliGold();
            Locator.PiggyBank.IncreasePiggyProgress();
            Database.Save(gameData);
            isClaimGold = false;
            topCanvas = UIManager.Ins.OpenUI<TopCanvas>();
            tween = fxImage.transform.DOLocalRotate(Vector3.forward * 360f, 5f, DG.Tweening.RotateMode.LocalAxisAdd).SetLoops(-1).SetEase(Ease.Linear);
            starList.ResetAll();
            starList.SetMaxItems(3);
            timer.Start(animTimes, animActions);
            isShowPack = false;
            for(int i = 0; i < iapPacks.Count; i++)
            {
                if (!isShowPack && !gameData.user.PurchasedItems.Contains(iapPacks[i].Item))
                {
                    iapPacks[i].gameObject.SetActive(true);
                    //iapPacks[i].OnInit(iapData.PurchaseProducts[iapPacks[i].Item]);
                    iapPacks[i].Play(UIAnim.ANIM.SHOW);
                    isShowPack = true;
                }
                else
                {
                    iapPacks[i].gameObject.SetActive(false);
                }
            }
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            nextLevelBtns[0].SetData($"{BASE_GOLD}");
            nextLevelBtns[1].SetData($"{BASE_GOLD * 2}");
            levelText.text = $"LEVEL {levelShow}";
            for (int i = 0; i < nextLevelBtns.Count; i++)
            {
                nextLevelBtns[i].SetInteractable(true);
            }

            if (currentReceiveGold == 0)
            {
                nextLevelBtns[0].gameObject.SetActive(true);
                nextLevelBtns[1].gameObject.SetActive(false);
            }
            else
            {
                nextLevelBtns[0].gameObject.SetActive(true);
                nextLevelBtns[1].gameObject.SetActive(true);
            }

            for(int i = 0; i < iapPacks.Count; i++)
            {
                if (gameData.user.PurchasedItems.Contains(iapPacks[i].Item))
                {
                    iapPacks[i].gameObject.SetActive(false);
                }
            }
            
        }
        protected void OnNextLevelClick(int index)
        {
            if (isClaimGold) return;
            goldFlyTf = startFlyGoldTfs[index];

            switch (index)
            {
                case 0: // Next
                    currentReceiveGold = BASE_GOLD;
                    ClaimGold(currentReceiveGold);
                    break;
                case 1: //NOTE: Video
                    currentReceiveGold = BASE_GOLD * 2;
                    Locator.Ads.Reward.Show(() => ClaimGold(currentReceiveGold), OnVideoHidden, Placement.X2_COIN);
                    break;
            }
            for (int i = 0; i < nextLevelBtns.Count; i++)
            {
                nextLevelBtns[i].SetInteractable(false);
            }
        }
        protected void OnBuyIapPack(IAP_ITEM item)
        {
            // List<ITEM> items = new List<ITEM>();
            // List<int> quantitys = new List<int>();
            // for(int i = 0; i < iapData.PurchaseProducts[item].rewards.Count; i++)
            // {
            //     items.Add(iapData.PurchaseProducts[item].rewards[i].Item);
            //     quantitys.Add(iapData.PurchaseProducts[item].rewards[i].Quantity);
            // }
            // Locator.IAPService.Purchase(item, () => {
            //     Locator.Reward.ShowReward(items, quantitys);
            //     gameData.user.PurchasedItems.Add(item);
            //     UIManager.Ins.UpdateAllUI();
            //     }, () => UIManager.Ins.OpenUI<ToastCanvas>().Show("Buy Fail!"));
        }
        protected void ClaimGold(int addGold)
        {
            if (addGold > 0)
            {
                isClaimGold = true;
                Locator.Reward.ClaimItem(ITEM.GOLD, addGold);
                Database.Save(gameData);
            }
            UpdateUI();
            Vector3 startPos = goldFlyTf.transform.position;
            topCanvas.FlyGoldReward(startPos, addGold > 0 ? addGold : BASE_GOLD);
            TimerManager.Ins.WaitForTime(1.5f, Next);

            void Next()
            {
                Locator.Ads.Inter.Show(NextLevel);
            }
        }
        protected void OnVideoHidden()
        {
            Time.timeScale = 1;
            UpdateUI();
        }
        protected void NextLevel()
        {
            UIManager.Ins.CloseUI<TopCanvas>();
            if (gameData.TotalStar >= gameData.user.normalLevelIndex / gameplayData.LevelPerChapter * 3 * gameplayData.LevelPerChapter
            && gameData.user.normalLevelIndex < CONSTANTS.MAX_LEVEL)
            {
                UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingActions });
            }
            else
            {
                UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingAction2s });
            }
        }
    }
}
