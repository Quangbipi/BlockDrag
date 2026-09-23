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
    using Solo.MOST_IN_ONE;
    using TMPro;
    using UnityEngine.UI;
    using Utilities.Timer;

    public class GameplayCanvas : UISCanvas
    {
        [SerializeField]
        protected UIButton settingBtn;
        [SerializeField]
        protected UIButton removeAdsBtn;
        [SerializeField]
        protected UIBoosterButton[] boosterBtns;
        [SerializeField]
        protected UISButton[] functionBtns;
        [SerializeField]
        protected TMP_Text levelTxt;
        [SerializeField]
        protected TMP_Text timeTxt;
        [SerializeField]
        ListItemShow starList;
        [SerializeField]
        ListItemShow knifeList;
        [SerializeField]
        GameObject timeChallengeLabel;
        [SerializeField]
        GameObject hardLabel;
        [SerializeField]
        Image cutBannerImage;
        [SerializeField]
        List<UIAnim> cutBannerAnims;
        [SerializeField]
        protected GameObject cutInfoParent;
        [SerializeField]
        protected List<Sprite> cutInfoSprites;

        protected ToastCanvas toastCanvas;
        protected GameData gameData;
        protected LevelData levelData;
        protected GameplayData gameplayData;
        protected UIAnim.ANIM starAnim;
        protected int currentStar = 0;
        protected int currentKnife = 0;
        protected void Awake()
        {
            gameData = Locator.Data.GetData<GameData>();
            gameplayData = Locator.Data.GetSOData<GameplayData>();
            settingBtn._OnClick += OnSettingBtnClick;
            removeAdsBtn._OnClick += OnRemoveAdsBtnClick;
            for (int i = 0; i < boosterBtns.Length; i++)
            {
                boosterBtns[i]._OnClick += OnBoosterBtnClick;
            }
            for (int i = 0; i < functionBtns.Length; i++)
            {
                functionBtns[i]._OnClick += OnBoosterBtnClick;
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            settingBtn._OnClick -= OnSettingBtnClick;
            removeAdsBtn._OnClick -= OnRemoveAdsBtnClick;
            for (int i = 0; i < boosterBtns.Length; i++)
            {
                boosterBtns[i]._OnClick -= OnBoosterBtnClick;
            }
            for (int i = 0; i < functionBtns.Length; i++)
            {
                functionBtns[i]._OnClick -= OnBoosterBtnClick;
            }
        }
        public override void Open(object param = null)
        {
            levelData = Locator.Data.GetSOData<LevelDataSO>().DetailData[gameData.user.normalLevelIndex].LevelData;
            UIManager.Ins.UpdateBannerSpace(!gameData.IsRemoveAds());
            toastCanvas = UIManager.Ins.OpenUI<ToastCanvas>();
            GameEventManager.Ins.PostEvent(DesignPattern.EventID.ON_ACTIVE_HOME_SCENE, new bool[2] { false, true });
            starAnim = UIAnim.ANIM.HIDE;
            base.Open(param);
            knifeList.SetMaxItems(levelData.maxKnifeCut);
            starList.SetMaxItems(3);
            starList.ResetAll();
            knifeList.ResetAll();
            starList.ActiveItems(3, 1);
            knifeList.ActiveItems(levelData.maxKnifeCut, 1);
            currentStar = 3;
            currentKnife = levelData.maxKnifeCut;

            for (int i = 0; i < boosterBtns.Length; i++)
            {
                boosterBtns[i].gameObject.SetActive(true);
            }
            switch (levelData.difficulty)
            {
                case LEVEL_DIFFICULTY.NONE:
                case LEVEL_DIFFICULTY.EASY:
                case LEVEL_DIFFICULTY.MEDIUM:
                    hardLabel.SetActive(false);
                    break;
                case LEVEL_DIFFICULTY.HARD:
                    hardLabel.SetActive(true);
                    break;
            }

            switch (levelData.performanceType)
            {
                case LEVEL_PERFORMANCE_TYPE.TIME:
                    timeChallengeLabel.SetActive(true);
                    break;
                default:
                    timeChallengeLabel.SetActive(false);
                    break;
            }
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            levelTxt.text = $"LEVEL {gameData.user.normalLevelIndex + 1}";

            int starDelta = gameData.level.Star - currentStar;
            int knifeDelta = gameData.level.Knife - currentKnife;
            currentStar = gameData.level.Star;
            currentKnife = gameData.level.Knife;
            if (starDelta > 0)
            {
                starList.Add();
            }
            else if (starDelta < 0)
            {
                ParticleSystem starTrail = ParticlePool.Play(Locator.Data.GetSOData<PoolData>().VFXs[VFX.STAR_TRAIL], functionBtns[0].transform.position);
                starTrail.transform.position = functionBtns[0].transform.position;
                starTrail.gameObject.SetActive(true);
                starTrail.transform.DOMove(starList.BreakingImages[gameData.level.Star].transform.position, 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    starList.Remove();
                });
            }


            if (knifeDelta < 0)
            {
                knifeList.Remove();
            }
            // pointTxt.text = gameData.level.Star.ToString();
            // actionTxt.text = $"CUT - {gameData.level.Action}";
            for (int i = 0; i < boosterBtns.Length; i++)
            {
                ITEM item = boosterBtns[i].ItemType;
                GameData.ItemData itemData = gameData.GetItemData(item);
                boosterBtns[i].SetQuantity(itemData.Quantity);
            }

            if (gameData.user.PurchasedItems.Contains(IAP_ITEM.STARTER_PACK))
            {
                removeAdsBtn.gameObject.SetActive(false);
            }
            else
            {
                removeAdsBtn.gameObject.SetActive(true);
            }
        }
        public void UpdateTime(int totalSecond)
        {
            int minutes = totalSecond / 60;
            int second = totalSecond % 60;
            timeTxt.text = $"{minutes:D2}:{second:D2}";
        }
        protected void OnSettingBtnClick(int index)
        {
            UIManager.Ins.OpenUI<SettingPopup>(1);
        }
        protected void OnRemoveAdsBtnClick(int index)
        {
            UIManager.Ins.OpenUI<RemoveAdsPopup>(0);
        }
        public void OnBoosterBtnClick(int index)
        {
            switch (Locator.Gameplay.IsCanUseBooster(index))
            {
                case 0:
                    UIManager.Ins.OpenUI<BuyBoosterPopup>(gameplayData.Items[(ITEM)index]);
                    break;
                case 1:
                    UIBoosterButton btn = Array.Find(boosterBtns, x => x.IndexID == index);
                    switch ((ITEM)index)
                    {
                        case ITEM.KNIFE_BOOSTER:
                            ParticleSystem starTrail = ParticlePool.Play(Locator.Data.GetSOData<PoolData>().VFXs[VFX.STAR_TRAIL], btn.transform.position);
                            starTrail.transform.position = btn.transform.position;
                            starTrail.gameObject.SetActive(true);
                            starTrail.transform.DOMove(knifeList.Images[gameData.level.Knife].transform.position, 0.5f)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() =>
                            {
                                knifeList.Add();
                                Locator.Audio.PlaySfx(SFX_TYPE.POP);
                                MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.Success, 0.1f);
                            });
                            break;
                    }

                    Locator.Reward.SpendItem((ITEM)index, 1);
                    Locator.Gameplay.UsingBooster(index);
                    UpdateUI();
                    Database.Save(gameData);
                    break;
                case 2:
                    Locator.Gameplay.UsingBooster(index);
                    UpdateUI();
                    break;
                case -1:
                    toastCanvas.Show("Max Hint!!");
                    break;
                case -2:
                    UIManager.Ins.OpenUI<RevivePopup>(gameplayData.Items[ITEM.REVIVE]);
                    break;
                case -3:
                    toastCanvas.Show("Max Knife!!");
                    break;
                case -4:
                    toastCanvas.Show("No boats in the dock to sort pasenger");
                    break;
                case -5:
                    toastCanvas.Show("Can not refresh boat color!");
                    break;
            }
            //UIManager.Ins.OpenUI<BuyBoosterPopup>();
        }
        public void SetActiveCuttingStar(bool value)
        {
            UIAnim.ANIM anim = UIAnim.ANIM.NONE;
            if (value)
            {
                anim = UIAnim.ANIM.SHOW;
            }
            else
            {
                anim = UIAnim.ANIM.HIDE;
            }
            if (starAnim == anim) return;
            starAnim = anim;
            for (int i = 0; i < cutBannerAnims.Count; i++)
            {
                cutBannerAnims[i].Play(anim);
            }
        }
        public void ChangeCutInfoSprite(bool value, int index = -1)
        {
            cutInfoParent.SetActive(value);
            if (index == 1)
            {
                cutInfoParent.SetActive(false);
            }
            UIButton.STATE state = value ? UIButton.STATE.SELECTING : UIButton.STATE.OPENING;
            if (functionBtns[0].State != state)
            {
                functionBtns[0].SetState(state);
                functionBtns[0].PlayAnim(UIAnim.ANIM.SHOW);
            }

            if (index < 0) return;
            cutBannerImage.sprite = cutInfoSprites[index];
        }
        public void ShowOneSpaceLeftBanner()
        {
            toastCanvas.ShowSpecial();
        }
        public void SetActiveBoosterButton(int index, bool value)
        {
            if (index < 0 || index >= boosterBtns.Length) return;
            boosterBtns[index].gameObject.SetActive(value);
        }

        public RectTransform GetBoosterButtonRect(int index)
        {
            if (index < 0 || index >= boosterBtns.Length) return null;
            return boosterBtns[index].GetComponent<RectTransform>();
        }
        public Image GetBoosterButtonFrameSprite(int index)
        {
            if (index < 0 || index >= boosterBtns.Length) return null;
            return boosterBtns[index].FrameImage;
        }
    }
}
