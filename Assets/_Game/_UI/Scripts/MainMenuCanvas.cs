using Base;
using Base.Init;
using Base.UI;
using Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class MainMenuCanvas : UICanvas
    {       
        [SerializeField]
        UIButton[] tabBtns;

        UICanvas[] tabs = new UICanvas[3];
        int currentTab = -1;
        GameData gameData;
        public override void Open(object param = null)
        {
            base.Open(param);
            UIManager.Ins.UpdateBannerSpace(!gameData.IsRemoveAds());
            if (currentTab > 0)
            {
                tabs[currentTab]?.Close();
                tabBtns[currentTab]?.SetState(UIButton.STATE.OPENING);
            }
            currentTab = 0;
            tabs[0].Open();
            tabBtns[0].SetState(UIButton.STATE.SELECTING);

            if (DebugManager.Ins != null)
            {
               UIManager.Ins.OpenUI<Debug_HomeCanvas>();
            }
        }

        public override void Close()
        {
            base.Close();
            if (DebugManager.Ins != null)
            {
               UIManager.Ins.CloseUI<Debug_HomeCanvas>();
            }
        }
        public int CurrentTab
        {
            get => currentTab;
            set
            {
                if (currentTab == value) return;
                if (currentTab >= 0)
                {
                    tabs[currentTab]?.Close();
                    tabBtns[currentTab]?.SetState(UIButton.STATE.OPENING);
                }
                currentTab = value;
                tabBtns[currentTab].SetState(UIButton.STATE.SELECTING);
                if (!tabs[currentTab])
                {
                    tabs[currentTab] = UIManager.Ins.GetUI<HomeCanvas>();
                }
                tabs[currentTab].Open();
            }
        }
        protected void Awake ()
        {
            currentTab = -1;
            for(int i = 0; i < tabBtns.Length; i++)
            {
                tabBtns[i]._OnClick += OnTabBtnClick;
            }

            tabs[0] = UIManager.Ins.GetUI<HomeCanvas>();
            tabs[1] = UIManager.Ins.GetUI<ShopCanvas>();
            // tabs[2] = UIManager.Ins.GetUI<CollectionCanvas>();

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                tabBtns[i].SetState(UIButton.STATE.OPENING);
                tabs[i].gameObject.SetActive(false);
            }
            CurrentTab = 0;
            
            // 
            PreLoadPopup();
            gameData = Locator.Data.GetData<GameData>();
        }

        public override void UpdateUI()
        {
            base.UpdateUI();
            for(int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null && tabs[i].isActiveAndEnabled)
                {
                    tabs[i].UpdateUI();
                }
            }
        }
        protected void OnDestroy()
        {
            for (int i = 0; i < tabBtns.Length; i++)
            {
                tabBtns[i]._OnClick -= OnTabBtnClick;
            }
        }
        protected void OnTabBtnClick(int index)
        {
            CurrentTab = index;
        }

        private static void PreLoadPopup()
        {
            // UIManager.Ins.PreloadUI<RemoveAdsPopup>();
            // UIManager.Ins.PreloadUI<DailyRewardPopup>();
            // UIManager.Ins.PreloadUI<SpinWheelPopup>();
            // UIManager.Ins.PreloadUI<DailyGiftPopup>();
            // UIManager.Ins.PreloadUI<PiggyBankPopup>();
            UIManager.Ins.PreloadUI<SettingPopup>();
        }
    }
}