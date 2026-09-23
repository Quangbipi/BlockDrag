using Base;
using Base.UI;
using Common;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Debug_HomeCanvas : UICanvas
    {
        public readonly Color ENABLE_COLOR = Color.white;
        public readonly Color DISABLE_COLOR = new Color(1, 1, 1, 0);

        [SerializeField]
        Button activeBtn;
        [SerializeField]
        Image activeBtnImage;
        [SerializeField]
        Button addGoldBtn;
        [SerializeField]
        Button fullStarBtn;
        [SerializeField]
        Button startLevelBtn;
        [SerializeField]
        TMP_InputField levelInputField;
        [SerializeField]
        GameObject contentRegion;
        [SerializeField]
        Button maxAdsBtn;
        [SerializeField]
        Button ironSourceAdsBtn;

        bool isActive;
        GameData gameData;
        protected GameData GameData => gameData ??= Locator.Data.GetData<GameData>();
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                contentRegion.SetActive(value);
                if (isActive)
                {
                    activeBtnImage.color = ENABLE_COLOR;
                }
                else
                {
                    activeBtnImage.color = DISABLE_COLOR;
                }
            }
        }
        protected void Awake()
        {
            IsActive = contentRegion.activeInHierarchy;
            activeBtn.onClick.AddListener(OnActiveBtnClick);
            addGoldBtn.onClick.AddListener(OnAddGold);
            startLevelBtn.onClick.AddListener(OnStartLevelClick);
            fullStarBtn.onClick.AddListener(OnFullStarLevel);
            maxAdsBtn.onClick.AddListener(OnMaxAdsClick);
            ironSourceAdsBtn.onClick.AddListener(OnIronSourceAdsClick);
        }

        protected void OnDestroy()
        {
            activeBtn.onClick.RemoveListener(OnActiveBtnClick);
            addGoldBtn.onClick.RemoveListener(OnAddGold);
            startLevelBtn.onClick.RemoveListener(OnStartLevelClick);
            fullStarBtn.onClick.RemoveListener(OnFullStarLevel);
            maxAdsBtn.onClick.RemoveListener(OnMaxAdsClick);
            ironSourceAdsBtn.onClick.RemoveListener(OnIronSourceAdsClick);
        }

        protected void OnActiveBtnClick()
        {
            IsActive = !IsActive;
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            levelInputField.text = GameData.user.normalLevelIndex.ToString();
        }
        protected void OnAddGold()
        {
            Locator.Reward.ClaimItem(ITEM.GOLD, 200);
            UIManager.Ins.UpdateAllUI();
        }
        protected void OnFullStarLevel()
        {
            bool isFullStar = false;
            for (int i = 0; i < GameData.level.LevelStars.Count; i++)
            {
                if (GameData.level.LevelStars[i] < 3)
                {
                    isFullStar = true;
                    GameData.level.LevelStars[i] = 3;
                    break;
                }
            }
            if (!isFullStar)
            {
                GameData.level.LevelStars.Add(3);
            }
            UIManager.Ins.UpdateAllUI();
        }
        protected void OnStartLevelClick()
        {
            GameData.user.normalLevelIndex = int.Parse(levelInputField.text);
            UIManager.Ins.GetUI<HomeCanvas>().SnapToLevel(GameData.user.normalLevelIndex);
            // UIManager.Ins.UpdateAllUI();
        }
        protected void OnMaxAdsClick()
        {
            Locator.Ads.Type = ADS_TYPE.MAX;
        }
        protected void OnIronSourceAdsClick()
        {
            Locator.Ads.Type = ADS_TYPE.IRON_SOURCE;
        }
    }
}
