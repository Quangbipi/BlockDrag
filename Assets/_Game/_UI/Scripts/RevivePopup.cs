using UnityEngine;

namespace UI
{
    using Base;
    using Base.UI;
    using Common;
    using TMPro;

    public class RevivePopup : BasePopup
    {
        [SerializeField]
        UIButton goldReviveButton;
        [SerializeField]
        UIButton videoReviveButton;
        [SerializeField]
        TMP_Text costTxt;
        [SerializeField]
        TMP_Text levelTxt;

        GameData.ItemData heartItem;
        GameData gameData;
        GameplayData gameplayData;
        bool isRevive = false;
        ItemData reviveItem;
        Placement place;

        private void Awake()
        {
            goldReviveButton._OnClick += OnRevive;
            videoReviveButton._OnClick += OnRevive;
            gameData = Locator.Data.GetData<GameData>();
            heartItem = gameData.GetItemData(ITEM.HEART);
            gameplayData = Locator.Data.GetSOData<GameplayData>();
        }
        public override void Open(object param)
        {
            reviveItem = (ItemData)param;
            base.Open(param);
            UIManager.Ins.OpenUI<TopCanvas>();
            place = Placement.RESET_LEVEL;
        }
        protected void OnRevive(int index)
        {
            switch (index)
            {
                case 0: //NOTE: Heart
                    if (gameplayData.ReviveHeartCost <= heartItem.Quantity)
                    {
                        Locator.Ads.Inter.Show(OnRevive);
                        UIManager.Ins.UpdateAllUI();
                    }
                    else
                    {
                        UIManager.Ins.OpenUI<BuyBoosterPopup>(gameplayData.Items[ITEM.HEART]).IsRevive = true;
                        Close();
                    }
                    break;
                case 1: // NOTE: Video
                    Time.timeScale = 0;
                    Locator.Ads.Reward.Show(OnVideoRevive, OnVideoHidden, place);
                    UIManager.Ins.UpdateAllUI();          
                    break;
            }
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            isRevive = false;
            costTxt.text = $"-{gameplayData.ReviveHeartCost}";
            levelTxt.text = $"Level {gameData.user.normalLevelIndex}";
            // if (gameplayData.ReviveHeartCost <= heartItem.Quantity)
            // {
            //     goldReviveButton.SetInteractable(true);
            //     goldReviveButton.SetState(UIButton.STATE.OPENING);
            // }
            // else
            // {
            //     goldReviveButton.SetInteractable(false);
            //     goldReviveButton.SetState(UIButton.STATE.DISABLE);
            // }

            if (reviveItem.WatchVideoCount > 0 || true)
            {
                videoReviveButton.SetInteractable(true);
                videoReviveButton.SetState(UIButton.STATE.OPENING);
            }
            else
            {
                videoReviveButton.SetInteractable(false);
                videoReviveButton.SetState(UIButton.STATE.DISABLE);
            }
        }
        protected void OnRevive()
        {
            isRevive = true;
            GameEventManager.Ins.PostEvent(DesignPattern.EventID.REVIVE);
            Locator.Gameplay.Revive();
            Close();
        }
        protected void OnVideoRevive()
        {
            reviveItem.WatchVideoCount--;
            OnRevive();
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
        }
    }
}