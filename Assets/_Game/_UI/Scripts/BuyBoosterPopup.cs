using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using System;
    using Base;
    using Base.UI;
    using Common;
    using TMPro;
    using UnityEngine.UI;
    using Utilities.Timer;

    public class BuyBoosterPopup : BasePopup
    {
        [SerializeField]
        TMP_Text titleTxt;
        [SerializeField]
        Image icon;
        [SerializeField]
        TMP_Text description;
        [SerializeField]
        TMP_Text costTxt;
        [SerializeField]
        TMP_Text heartTxt;
        [SerializeField]
        TMP_Text heartTimeTxt;
        [SerializeField]
        GameObject heartIcon;
        [SerializeField]
        GameObject timeClock;
        [SerializeField]
        UIButton[] funcBtns;
        ItemData boosterData;
        GameData.ItemData goldItem;
        GameData.ItemData boosterItem;
        STimer heartTimer;
        GameplayData gameplayData;
        Placement place;
        int cost = 0;
        public bool IsRevive = false;
        private void Awake()
        {
            gameplayData = Locator.Data.GetSOData<GameplayData>();
            goldItem = Locator.Reward.GetItem(ITEM.GOLD);
            heartTimer = TimerManager.Ins.PopSTimer();
            for (int i = 0; i < funcBtns.Length; i++)
            {
                funcBtns[i]._OnClick += OnUsingFunction;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < funcBtns.Length; i++)
            {
                funcBtns[i]._OnClick -= OnUsingFunction;
            }
        }
        public override void Open(object param)
        {
            boosterData = (ItemData)param;
            IsRevive = false;
            base.Open(param);
            UIManager.Ins.OpenUI<TopCanvas>();
            switch (boosterData.Type)
            {
                case ITEM.HINT_BOOSTER:
                    place = Placement.HINT_BOOSTER;
                    break;
                case ITEM.KNIFE_BOOSTER:
                    place = Placement.KNIFE_BOOSTER;
                    break;
                case ITEM.HEART:
                    place = Placement.REFILL_HEART;
                    break;
            }
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            cost = 0;
            switch (boosterData.Type)
            {
                case ITEM.HEART:
                    cost = boosterData.Cost * gameplayData.MaxHearts;
                    heartIcon.SetActive(true);
                    break;
                default:
                    cost = boosterData.Cost;
                    heartIcon.SetActive(false);
                    break;
            }

            if (cost <= goldItem.Quantity)
            {
                funcBtns[0].SetInteractable(true);
                funcBtns[0].SetState(UIButton.STATE.OPENING);
            }
            else
            {
                funcBtns[0].SetInteractable(false);
                funcBtns[0].SetState(UIButton.STATE.DISABLE);
            }

            if (boosterData.WatchVideoCount > 0 || true)
            {
                funcBtns[1].SetInteractable(true);
                funcBtns[1].SetState(UIButton.STATE.OPENING);
            }
            else
            {
                funcBtns[1].SetInteractable(false);
                funcBtns[1].SetState(UIButton.STATE.DISABLE);
            }

            if (boosterData != null)
            {
                switch (boosterData.Type)
                {
                    case ITEM.HEART:
                        description.gameObject.SetActive(false);
                        heartTxt.gameObject.SetActive(true);
                        timeClock.SetActive(true);
                        titleTxt.text = $"Refill {boosterData.Name}?";
                        icon.sprite = boosterData.Icon;
                        costTxt.text = $"{boosterData.Cost * gameplayData.MaxHearts}";
                        boosterItem = Locator.Reward.GetItem(boosterData.Type);
                        OnTimeTik();
                        heartTimer.Start(1f, OnTimeTik, true);
                        break;
                    default:
                        description.gameObject.SetActive(true);
                        heartTxt.gameObject.SetActive(false);
                        timeClock.SetActive(false);
                        titleTxt.text = $"More {boosterData.Name}?";
                        icon.sprite = boosterData.Icon;
                        description.text = boosterData.Description;
                        costTxt.text = boosterData.Cost.ToString();
                        boosterItem = Locator.Reward.GetItem(boosterData.Type);
                        break;
                }
            }
        }

        private void OnTimeTik()
        {
            heartTimeTxt.text = Locator.Heart.GetFormattedTimeRemaining();
        }

        protected void OnUsingFunction(int code)
        {
            switch (code)
            {
                case 0:
                    //Using gold
                    if (goldItem.Quantity < cost)
                    {
                        // UIManager.Ins.OpenUI<ShopCanvas>();
                        // Close();
                    }
                    else
                    {
                        Locator.Reward.SpendItem(ITEM.GOLD, cost);
                        OnUsingBooster();
                    }
                    break;
                case 1:
                    //Using video
                    Time.timeScale = 0;
                    Locator.Ads.Reward.Show(OnVideoUsingBooster, OnVideoHidden, place);
                    break;
            }
            for (int i = 0; i < funcBtns.Length; i++)
            {
                funcBtns[i].SetInteractable(false);
            }
        }
        protected void OnUsingBooster()
        {
            switch (boosterData.Type)
            {
                case ITEM.HEART:
                    Locator.Heart.RefillHearts();
                    if (IsRevive)
                    {
                        GameEventManager.Ins.PostEvent(DesignPattern.EventID.REVIVE);
                        Locator.Gameplay.Revive();
                    }
                    break;
                default:
                    Locator.Reward.ClaimItem(boosterData.Type, 1);
                    UIManager.Ins.GetUI<GameplayCanvas>().OnBoosterBtnClick((int)boosterData.Type);
                    break;
            }
            Time.timeScale = 1;
            Locator.Reward.SaveItemData();
            UIManager.Ins.UpdateAllUI();
            Close();
        }

        protected void OnVideoUsingBooster()
        {
            boosterData.WatchVideoCount--;
            switch (boosterData.Type)
            {
                case ITEM.HEART:
                    Locator.Reward.ClaimItem(ITEM.HEART, 1);
                    if (IsRevive)
                    {
                        GameEventManager.Ins.PostEvent(DesignPattern.EventID.REVIVE);
                        Locator.Gameplay.Revive();
                    }
                    break;
                default:
                    Locator.Reward.ClaimItem(boosterData.Type, 1);
                    UIManager.Ins.GetUI<GameplayCanvas>().OnBoosterBtnClick((int)boosterData.Type);
                    break;
            }
            Time.timeScale = 1;
            Locator.Reward.SaveItemData();
            UIManager.Ins.UpdateAllUI();
            Close();
        }
        protected void OnVideoHidden()
        {
            Time.timeScale = 1;
            UpdateUI();
        }
        public override void Close()
        {
            base.Close();
            UIManager.Ins.CloseUI<TopCanvas>();
            heartTimer.Stop();
        }
    }
}
