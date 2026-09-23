using System.Collections.Generic;
using Base;
using Base.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI
{
    public class RemoveAdsPopup : BasePopup
    {
    //     [SerializeField] 
    //     protected UIButton removeAdsBtn;
    //     [SerializeField]
    //     protected Image frameImg;
    //     [SerializeField]
    //     protected List<IAP_ITEM> items;
    //     [SerializeField]
    //     protected List<GameObject> itemLists;
    //     [SerializeField]
    //     protected List<Sprite> frames;
    //     [SerializeField]
    //     TMP_Text priceTxt;
    //     protected IAPData iapData;
    //     protected IAP_ITEM currentItem;
    //     protected int itemIndex;
    //     private void Awake()
    //     {
    //         removeAdsBtn._OnClick += OnRemoveAds;
            
    //     }

    //     protected override void OnDestroy()
    //     {
    //         removeAdsBtn._OnClick -= OnRemoveAds;
    //         base.OnDestroy();
    //     }

    //     public override void Open(object param)
    //     {
    //         itemIndex = (int)param;
    //         iapData ??= Locator.Data.GetSOData<IAPData>();
    //         currentItem = items[itemIndex];
    //         base.Open(param);       
    //     }

    //     public override void UpdateUI()
    //     {
    //         base.UpdateUI();
    //         priceTxt.text = iapData?.PurchaseProducts[currentItem].Price;
    //         itemLists.ForEach(x => x.gameObject.SetActive(false));
    //         itemLists[itemIndex].gameObject.SetActive(true);
    //         frameImg.sprite = frames[itemIndex];
    //     }
    //     private void OnRemoveAds(int index)
    //     {
    //         // check if ads is bought
    //         var gameData = Locator.Data.GetData<GameData>();
    //         if (gameData.user.PurchasedItems.Contains(currentItem)) return;
    //         // bought it, the close
    //         // TODO: Temporary, change to SO Data for bonus item when buy
    //         Locator.IAPService.Purchase(currentItem, () =>
    //         {
    //             DevLog.Log(DevId.Hung, "Packing Success");
    //             gameData.user.PurchasedItems.Add(currentItem);
    //             // bonus item
    //             List<ITEM> items = new List<ITEM>();
    //             List<int> quantitys = new List<int>();

    //             for (int i = 0; i < iapData.PurchaseProducts[currentItem].rewards.Count; i++)
    //             {
    //                 items.Add(iapData.PurchaseProducts[currentItem].rewards[i].Item);
    //                 quantitys.Add(iapData.PurchaseProducts[currentItem].rewards[i].Quantity);
    //                 Locator.Reward.ClaimItem(items[i], quantitys[i]);
    //             }
                
    //             Locator.Reward.SaveItemData();              
    //             Locator.Reward.ShowReward(items, quantitys);
    //             UIManager.Ins.UpdateAllUI();
    //             Close();
    //         });
    //     }

    //     private readonly List<GameData.ItemData> _listRwItem = new()
    //     {
    //         new GameData.ItemData()
    //         {
    //             Item = ITEM.GOLD,
    //             Quantity = 300,
    //         },
    //         new GameData.ItemData()
    //         {
    //         Item = ITEM.RESET_BOOSTER,
    //         Quantity = 1,
    //         },
    //         new GameData.ItemData()
    //         {
    //             Item = ITEM.KNIFE_BOOSTER,
    //             Quantity = 1,
    //         },
    //         new GameData.ItemData()
    //         {
    //             Item = ITEM.HINT_BOOSTER,
    //             Quantity = 1,
    //         },
    //     };
    }
}