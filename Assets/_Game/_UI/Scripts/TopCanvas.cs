using Gameplay.BlockDrag;
using Quang.Base;
using Quang.UI;
using TMPro;
using UnityEngine;
using Quang.Utilities.Timer;

namespace UI
{
    public class TopCanvas : UISCanvas
    {
        [SerializeField]
        TMP_Text goldTxt;
        [SerializeField]
        TMP_Text heartTxt;
        [SerializeField]
        TMP_Text heartTimeTxt;
         [SerializeField] 
        private UIFlyReward flyReward;

        GameData gameData;
        STimer regenerateHeartTime;

        protected void Awake()
        {
            gameData = Locator.Data.GetData<GameData>();
            regenerateHeartTime = TimerManager.Ins.PopSTimer();
        }
        public override void UpdateUI()
        {
            base.UpdateUI();
            goldTxt.text = gameData.GetItemData(ITEM.GOLD.ToItemId()).Quantity.ToString();
            heartTxt.text = gameData.GetItemData(ITEM.HEART.ToItemId()).Quantity.ToString();
            // if (gameData.GetItemData(ITEM.HEART.ToItemId()).Quantity >= HeartSave.MaxHearts)
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

        }

        protected void OnTimeTik()
        {
            heartTimeTxt.text = Locator.Heart.GetHeartFormattedTimeRemaining();
            heartTxt.text = gameData.GetItemData(ITEM.HEART.ToItemId()).Quantity.ToString();
        }
        public override void Close()
        {
            base.Close();
            regenerateHeartTime.Stop();
        }

        public void FlyGoldReward(Vector3 fromPos, int amount)
        {
            int currentGold = gameData.GetItemData(ITEM.GOLD.ToItemId()).Quantity;
            int prevGold = currentGold - amount;
            flyReward.InitReward(prevGold, currentGold, fromPos);
        }
    }
}
