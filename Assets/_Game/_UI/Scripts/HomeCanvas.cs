using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Common;
    using TMPro;

    using Base.UI;
    using Base;
    using Base.Init;
    using System;
    using PolyAndCode.UI;
    using DG.Tweening;
    using Utilities.Timer;
    using Utilities;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Spine.Unity;

    public class HomeCanvas : UISCanvas, IRecyclableScrollRectDataSource
    {
        [SerializeField]
        List<SkeletonGraphic> bannerUISkeletons;
        [SerializeField]
        UIButton playBtn;
        [SerializeField]
        UIButton settingBtn;
        [SerializeField]
        UIButton shopBtn;
        [SerializeField]
        UISButton addStarBtn;
        [SerializeField]
        TMP_Text goldTxt;
        [SerializeField]
        TMP_Text heartTxt;
        [SerializeField]
        TMP_Text heartTimeTxt;

        [SerializeField]
        private UIFlyReward flyReward;
        [SerializeField]
        private UIButton piggyBankBtn;
        [SerializeField]
        private UIButton spinWheelBtn;
        [SerializeField]
        private UIButton dailyRewardBtn;
        [SerializeField]
        private UIButton removeAdsBtn;
        [SerializeField]
        private UIButton dailyGiftBtn;
        [SerializeField]
        private UIButton levelBtn;
        [SerializeField]
        protected DetectMouse levelScrollRectDetect;
        [SerializeField]
        protected HorizontalLayoutGroup levelHorizontalLayout;
        [SerializeField]
        private RecyclableScrollRect levelScrollRect;
        [SerializeField]
        protected LevelDataSO levelData;
        protected GameData gameData;
        protected GameplayData gameplayData;
        // Start is called before the first frame update
        protected List<Action> loadingActions;
        protected List<float> loadingTimes;
        protected List<UIHomeItemData> levelItemDatas;
        protected STimer regenerateHeartTime;
        public Transform PiggyBankBtnTransform => piggyBankBtn.transform;
        protected bool isSnapping;
        int currentTotalStar = 0;
        int currentLevel = 0;
        int minCellIndex = 0;
        int maxCellIndex = 0;
        float containWidth;
        float cellNormalWidth;
        Vector2 currentPosition;
        bool firstInit = true;
        bool isFirstSnap = true;
        protected void Awake()
        {
            firstInit = true;
            playBtn._OnClick += OnPlayBtnClick;
            settingBtn._OnClick += OnSettingBtnClick;
            dailyGiftBtn._OnClick += OnDailyGiftBtnClick;
            piggyBankBtn._OnClick += OnPiggyBankBtnClick;
            spinWheelBtn._OnClick += OnSpinWheelBtnClick;
            dailyRewardBtn._OnClick += OnDailyRewardBtnClick;
            levelBtn._OnClick += OnLevelBtnClick;
            removeAdsBtn._OnClick += OnRemoveAdsBtnClick;
            shopBtn._OnClick += OnShopBtnClick;
            addStarBtn._OnClick += OnAddStarBtnClick;
            levelScrollRectDetect._OnBeginDrag += OnStartScrollLevel;
            levelScrollRectDetect._OnDrag += OnDragScrollLevel;
            levelScrollRectDetect._OnPointerUp += OnEndScrollLevel;


            gameData ??= Locator.Data.GetData<GameData>();
            gameplayData ??= Locator.Data.GetSOData<GameplayData>();
            loadingActions = new List<Action>() { () =>
            {
                UIManager.Ins.OpenUI<GameplayCanvas>();
                Locator.Gameplay.ConstructLevel(gameData.user.normalLevelIndex);
            }
            , () => UIManager.Ins.GetUI<LoadingCanvas>().Close()};
            loadingTimes = new List<float>() { 1.2f, 1.5f };
            levelItemDatas = new List<UIHomeItemData>();
            for (int i = 0; i <= CONSTANTS.MAX_LEVEL; i++)
            {
                int star = 0;
                if (i < gameData.level.LevelStars.Count)
                {
                    star = gameData.level.LevelStars[i];
                }
                levelItemDatas.Add(new UIHomeItemData
                {
                    Level = i,
                    CurrentStar = star,
                    Type = (int)levelData.DetailData[i].LevelData.performanceType,
                    RequireStar = i / gameplayData.LevelPerChapter * 3 * gameplayData.LevelPerChapter
                });
            }
            regenerateHeartTime = TimerManager.Ins.PopSTimer();
            levelScrollRect.DataSource = this;
            Locator.Heart.OnHeartsChanged += OnHeartChanged;
        }
        // Update is called once per frame
        protected override void OnDestroy()
        {
            base.OnDestroy();
            playBtn._OnClick -= OnPlayBtnClick;
            settingBtn._OnClick -= OnSettingBtnClick;
            dailyGiftBtn._OnClick -= OnDailyGiftBtnClick;
            piggyBankBtn._OnClick -= OnPiggyBankBtnClick;
            spinWheelBtn._OnClick -= OnSpinWheelBtnClick;
            dailyRewardBtn._OnClick -= OnDailyRewardBtnClick;
            levelBtn._OnClick -= OnLevelBtnClick;
            removeAdsBtn._OnClick -= OnRemoveAdsBtnClick;
            shopBtn._OnClick -= OnShopBtnClick;
            addStarBtn._OnClick -= OnAddStarBtnClick;
            Locator.Heart.OnHeartsChanged -= OnHeartChanged;
            levelScrollRectDetect._OnBeginDrag -= OnStartScrollLevel;
            levelScrollRectDetect._OnDrag -= OnDragScrollLevel;
            levelScrollRectDetect._OnPointerUp -= OnEndScrollLevel;
        }
        public override void Open(object param = null)
        {
            isFirstSnap = true;
            base.Open(param);

            if (DebugManager.Ins != null)
            {
                UIManager.Ins.OpenUI<Debug_HomeCanvas>();
            }
            PlayAppearAnim();
            GameEventManager.Ins.PostEvent(DesignPattern.EventID.ON_ACTIVE_HOME_SCENE, new bool[2] { true, false });
            currentLevel = gameData.user.normalLevelIndex;

            for (int i = 0; i < gameData.level.LevelStars.Count; i++)
            {
                levelItemDatas[i].CurrentStar = gameData.level.LevelStars[i];
            }
            isSnapping = false;
            levelScrollRect.horizontal = false;
            levelHorizontalLayout.padding.left = (int)((UIManager.Ins.ParentCanvasTf.rect.width - levelScrollRect.PrototypeCell.rect.width) / 2);
            levelHorizontalLayout.padding.right = (int)((UIManager.Ins.ParentCanvasTf.rect.width - levelScrollRect.PrototypeCell.rect.width) / 2);
            TimerManager.Ins.WaitForFrame(6, () =>
            {
                if (firstInit)
                {
                    minCellIndex = 0;
                    maxCellIndex = levelScrollRect.MinPoolSize - 1;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(levelHorizontalLayout.GetComponent<RectTransform>());
                    firstInit = false;
                }

                containWidth = levelHorizontalLayout.padding.left + levelHorizontalLayout.padding.right
                + levelScrollRect.PrototypeCell.rect.width * levelScrollRect.MinPoolSize
                - UIManager.Ins.ParentCanvasTf.rect.width;
                cellNormalWidth = levelScrollRect.PrototypeCell.rect.width / containWidth;
                levelScrollRect.UpdateData();
            });

            TimerManager.Ins.WaitForTime(0.8f, () =>
            {
                SnapToLevel(gameData.user.normalLevelIndex);
            });
            Locator.Audio.PlayBgm(BGM_TYPE.HOME);
            //SetUpFeatureBtn(FeatureButtonType.All);
        }

        public override void Close()
        {
            base.Close();
            if (DebugManager.Ins != null)
            {
                UIManager.Ins.CloseUI<Debug_HomeCanvas>();
            }
        }

        public void PlayAppearAnim()
        {
            for (int i = 0; i < bannerUISkeletons.Count; i++)
            {
                bannerUISkeletons[i].AnimationState.SetAnimation(0, ANIM_NAME.APPEAR, false);
                bannerUISkeletons[i].AnimationState.AddAnimation(0, ANIM_NAME.IDLE, true, 0);
            }

        }
        public override void UpdateUI()
        {
            gameData ??= Locator.Data.GetData<GameData>();
            gameplayData ??= Locator.Data.GetSOData<GameplayData>();
            currentTotalStar = gameData.TotalStar;

            goldTxt.text = gameData.GetItemData(ITEM.GOLD).Quantity.ToString();
            heartTxt.text = gameData.GetItemData(ITEM.HEART).Quantity.ToString();

            // if (gameData.GetItemData(ITEM.HEART).Quantity >= HeartSave.MaxHearts)
            // {
            //     heartTimeTxt.text = "FULL";
            //     regenerateHeartTime.Stop();
            // }
            // else
            // {
            //     if (!regenerateHeartTime.IsStart)
            //     {
            //         OnTimeTik();
            //         regenerateHeartTime.Start(1f, OnTimeTik, true);
            //     }
            // }

            if (gameData.user.PurchasedItems.Contains(IAP_ITEM.PREMIUM_PACK))
            {
                removeAdsBtn.gameObject.SetActive(false);
            }
            else
            {
                removeAdsBtn.gameObject.SetActive(true);
            }
            // DevLog.Log(DevId.Hung, $"CUR HEART = {gameData.GetItemData(ITEM.HEART).Quantity} MAX HEART = {HeartSave.MaxHearts}");
            //SetUpFeatureBtn(FeatureButtonType.All);
            // TimerManager.Ins.WaitForFrame(5, () =>
            // {
            //     for (int i = 0; i < gameData.level.LevelStars.Count; i++)
            //     {
            //         levelItemDatas[i].CurrentStar = gameData.level.LevelStars[i];
            //     }
            //     levelScrollRect.UpdateData();
            // });
        }

        protected void UpdateAddStarBtn()
        {
            int requireStar = gameData.user.normalLevelIndex / gameplayData.LevelPerChapter * 3 * gameplayData.LevelPerChapter;
            if (gameData.TotalStar < requireStar)
            {
                addStarBtn.Show();
                addStarBtn.SetData($"+{requireStar - gameData.TotalStar}");
            }
            else
            {
                addStarBtn.Hide();
            }
        }

        protected void OnAddStarBtnClick(int index)
        {
            int level = -1;
            for (int i = 0; i < gameData.level.LevelStars.Count; i++)
            {
                if (gameData.level.LevelStars[i] < 3)
                {
                    level = i;
                    break;
                }
            }
            if (level < 0)
            {
                level = gameData.level.LevelStars.Count;
            }
            SnapToLevel(level);
            addStarBtn.Hide();
        }
        protected void OnShopBtnClick(int obj)
        {
            UIManager.Ins.OpenUI<ShopCanvas>();
        }
        protected void OnTimeTik()
        {
            heartTimeTxt.text = Locator.Heart.GetFormattedTimeRemaining();
        }

        protected void OnHeartChanged(int count)
        {
            UpdateUI();
        }

        public void SnapToLevel(int level)
        {
            if (isSnapping) return;
            isSnapping = true;
            levelScrollRect.horizontal = false;
            // DevLog.Log(DevId.Hung, "Start Snap Level");
            float distance = level * cellNormalWidth;
            float startPoint = currentPosition.x / containWidth;
            float oldValue = startPoint;

            DOVirtual.Float(startPoint, distance, Mathf.Clamp((distance - startPoint) / levelScrollRect.MinPoolSize, 0.1f, 2.5f), x =>
            {
                float addValue = x - oldValue;
                levelScrollRect.horizontalNormalizedPosition += addValue;
                oldValue = x;
                currentPosition += new Vector2(addValue * containWidth, 0);
            }).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                isSnapping = false;
                levelScrollRect.horizontal = true;
                int centerLevel = Mathf.RoundToInt(Mathf.Lerp(minCellIndex, maxCellIndex, levelScrollRect.normalizedPosition.x));
                if (isFirstSnap)
                {
                    isFirstSnap = false;
                    GetCell(centerLevel)?.Select(true);
                    gameData.user.normalLevelIndex = currentLevel;
                }
                else
                {
                    if (centerLevel != currentLevel)
                    {
                        GetCell(currentLevel)?.Select(false);
                        currentLevel = Mathf.RoundToInt(Mathf.Lerp(minCellIndex, maxCellIndex, levelScrollRect.normalizedPosition.x));
                        GetCell(currentLevel)?.Select(true);
                        gameData.user.normalLevelIndex = currentLevel;
                        // DevLog.Log(DevId.Hung, $"Change level {currentLevel} -> {centerLevel}");
                    }
                }

                // DevLog.Log(DevId.Hung, $"End Snap Level");
                UpdateAddStarBtn();
            });
        }

        bool isScrolling = false;
        bool isStartSnap = false;
        float direction = 0;
        protected void OnStartScrollLevel(PointerEventData data)
        {
            if (isSnapping) return;
            isScrolling = true;
            isStartSnap = false;
            // DevLog.Log(DevId.Hung, "Start Scroll Level");
        }
        private void OnEndScrollLevel(PointerEventData data)
        {
            if (isSnapping || !isScrolling) return;
            isStartSnap = true;
            // DevLog.Log(DevId.Hung, "End Scroll Level");
        }
        private void OnDragScrollLevel(PointerEventData data)
        {
            if (isSnapping || !isScrolling) return;

        }

        void Update()
        {
            if(isFirstSnap) return;
            if (Mathf.Sign(-levelScrollRect.velocity.x) != 0)
                direction = Mathf.Sign(-levelScrollRect.velocity.x);
            int centerLevel = Mathf.RoundToInt(Mathf.Lerp(minCellIndex, maxCellIndex, levelScrollRect.normalizedPosition.x));
            if (centerLevel != currentLevel)
            {
                // DevLog.Log(DevId.Hung, $"Change level {currentLevel} -> {centerLevel}");
                GetCell(currentLevel)?.Select(false);
                currentLevel = centerLevel;
                GetCell(currentLevel)?.Select(true);
                gameData.user.normalLevelIndex = currentLevel;
            }

            if (isSnapping) return;
            if (isStartSnap && levelScrollRect.velocity.sqrMagnitude < Mathf.Pow(200, 2) && levelScrollRect.velocity.sqrMagnitude > 1)
            {
                // DevLog.Log(DevId.Hung, $"Auto Snap Level - {levelScrollRect.velocity.sqrMagnitude}");
                currentPosition = new Vector2(minCellIndex * levelScrollRect.PrototypeCell.rect.width
            + levelScrollRect.normalizedPosition.x * containWidth, 0);

                isScrolling = false;
                isStartSnap = false;
                SnapToLevel(currentLevel);
            }
            // DevLog.Log(DevId.Hung, $"NORMAL: {levelScrollRect.normalizedPosition.x} - MIN: {minCellIndex} - MAX: {maxCellIndex}\n ");
        }

        protected UIHomeLevelItem GetCell(int index)
        {
            ICell cell = levelScrollRect.CachedCells?.Find(x => x.CellIndex == index);
            return cell as UIHomeLevelItem;
        }
        protected void OnPlayBtnClick(int index)
        {
            //NOTE: Test
            if (gameData.user.normalLevelIndex >= CONSTANTS.MAX_LEVEL)
            {
                UIManager.Ins.OpenUI<ToastCanvas>().Show($"Comming Soon!");
            }
            else
            {
                if (gameData.GetItemData(ITEM.HEART).Quantity < gameplayData.ReviveHeartCost)
                {
                    UIManager.Ins.OpenUI<BuyBoosterPopup>(gameplayData.Items[ITEM.HEART]);
                }
                else if (gameData.TotalStar < levelItemDatas[gameData.user.normalLevelIndex].RequireStar)
                {
                    UIManager.Ins.OpenUI<ToastCanvas>().Show($"Need More {levelItemDatas[gameData.user.normalLevelIndex].RequireStar - gameData.TotalStar} stars to play!");
                }
                else
                {
                    UIManager.Ins.CloseAll();
                    UIManager.Ins.OpenUI<LoadingCanvas>(new object[] { loadingTimes, loadingActions });
                }
            }
        }

        protected void OnSettingBtnClick(int index)
        {
            UIManager.Ins.OpenUI<SettingPopup>(0);
        }

        protected void OnDailyGiftBtnClick(int index)
        {
            UIManager.Ins.OpenUI<ToastCanvas>().Show("Coming Soon!");
            //UIManager.Ins.OpenUI<DailyGiftPopup>();
        }

        private void OnPiggyBankBtnClick(int index)
        {
            // UIManager.Ins.OpenUI<PiggyBankPopup>();
        }

        private void OnSpinWheelBtnClick(int index)
        {
            // UIManager.Ins.OpenUI<SpinWheelPopup>();
        }

        private void OnDailyRewardBtnClick(int index)
        {
            UIManager.Ins.OpenUI<ToastCanvas>().Show("Coming Soon!");
            // UIManager.Ins.OpenUI<DailyRewardPopup>();
        }

        private void OnLevelBtnClick(int index)
        {
            UIManager.Ins.OpenUI<ToastCanvas>().Show("Coming Soon!");
            // UIManager.Ins.OpenUI<ShopCanvas>();
        }

        private void OnRemoveAdsBtnClick(int index)
        {
            UIManager.Ins.OpenUI<RemoveAdsPopup>(1);
        }

        public void FlyGoldReward(Vector3 fromPos, int amount)
        {
            int currentGold = gameData.GetItemData(ITEM.GOLD).Quantity;
            int prevGold = currentGold - amount;
            flyReward.InitReward(prevGold, currentGold, fromPos);
        }
        #region LEVEL DATA
        public int GetItemCount()
        {
            return CONSTANTS.MAX_LEVEL + 1;
        }

        public void SetCell(ICell cell, int index)
        {
            var item = cell as UIHomeLevelItem;
            item.ConfigureCell(levelItemDatas[index], currentTotalStar, gameplayData.LevelPerChapter, index);
            if (index > maxCellIndex)
            {
                maxCellIndex++;
                minCellIndex++;
            }

            if (index < minCellIndex)
            {
                maxCellIndex--;
                minCellIndex--;
            }
        }
        #endregion
        //public void SetUpFeatureBtn(FeatureButtonType buttonType, bool updateCoin = false)
        //{
        //    switch (buttonType)
        //    {
        //        case FeatureButtonType.PiggyBank:
        //            piggyBankBtn.SetUp();
        //            break;
        //        case FeatureButtonType.SpinWheel:
        //            spinWheelBtn.SetUp();
        //            break;
        //        case FeatureButtonType.DailyReward:
        //            dailyRewardBtn.SetUp();
        //            break;
        //        case FeatureButtonType.DailyGift:
        //            dailyGiftBtn.Setup();
        //            break;
        //        case FeatureButtonType.RemoveAds:
        //            removeAdsBtn.gameObject.SetActive(!gameData.user.PurchasedItems.Contains(IAP_ITEM.REMOVE_ADS));
        //            break;
        //        case FeatureButtonType.All:
        //            piggyBankBtn.SetUp();
        //            spinWheelBtn.SetUp();
        //            dailyRewardBtn.SetUp();
        //            dailyGiftBtn.Setup();
        //            removeAdsBtn.gameObject.SetActive(!gameData.user.PurchasedItems.Contains(IAP_ITEM.REMOVE_ADS));
        //            break;
        //    }
        //    if (updateCoin)
        //    {
        //        goldTxt.text = gameData.GetItemData(ITEM.GOLD).Quantity.ToString();
        //    }
        //}
    }

    public enum FeatureButtonType
    {
        All = -1,
        PiggyBank = 0,
        SpinWheel = 1,
        DailyReward = 2,
        DailyGift = 3,
        RemoveAds = 4
    }
}
