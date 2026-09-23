using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base;
    using Base.UI;
    using Common;

    public class ShopCanvas : UISCanvas
    {
        // [SerializeField]
        // IAPData iapData;
        // [SerializeField]
        // List<UIPack> packs;

        // GameData gameData;
        // protected void Awake()
        // {
        //     for (int i = 0; i < packs.Count; i++)
        //     {
        //         packs[i]._OnItemPurchased += OnItemPurchased;
        //     }
        // }
        // protected override void OnDestroy()
        // {
        //     base.OnDestroy();
        //     for (int i = 0; i < packs.Count; i++)
        //     {
        //         packs[i]._OnItemPurchased -= OnItemPurchased;
        //     }
        // }
        // public override void Open(object param)
        // {
        //     gameData ??= Locator.Data.GetData<GameData>();
        //     base.Open(param);
        //     for (int i = 0; i < packs.Count; i++)
        //     {
        //         packs[i].OnInit(iapData.PurchaseProducts[packs[i].Item]);
        //     }
        //     //GameEventManager.Ins.PostEvent(DesignPattern.EventID.OnActiveHomeScene, new bool[2] {false, true});
        // }

        // public override void UpdateUI()
        // {
        //     base.UpdateUI();
        //     for (int i = 0; i < packs.Count; i++)
        //     {
        //         if (packs[i].Item == IAP_ITEM.STARTER_PACK || packs[i].Item == IAP_ITEM.PREMIUM_PACK)
        //         {
        //             if (gameData.user.PurchasedItems.Contains(packs[i].Item))
        //                 packs[i].gameObject.SetActive(false);
        //             continue;
        //         }
        //         switch (iapData.PurchaseProducts[packs[i].Item].Type)
        //         {
        //             case IAP_PRODUCT_TYPE.NON_CONSUMABLE:
        //                 if (gameData.user.PurchasedItems.Contains(packs[i].Item))
        //                     packs[i].gameObject.SetActive(false);
        //                 break;
        //         }
        //     }
        // }

        // protected void OnItemPurchased(IAP_ITEM item)
        // {
        //     Time.timeScale = 0;
        //     Locator.IAPService.Purchase(item, () =>
        //     {
        //         Time.timeScale = 1;
        //         if (iapData.PurchaseProducts[item].Type == IAP_PRODUCT_TYPE.NON_CONSUMABLE)
        //             gameData.user.PurchasedItems.Add(item);
        //         else if (item == IAP_ITEM.STARTER_PACK || item == IAP_ITEM.PREMIUM_PACK)
        //         {
        //             gameData.user.PurchasedItems.Add(item);
        //         }

        //         List<ITEM> items = new List<ITEM>();
        //         List<int> quantitys = new List<int>();

        //         for (int i = 0; i < iapData.PurchaseProducts[item].rewards.Count; i++)
        //         {
        //             items.Add(iapData.PurchaseProducts[item].rewards[i].Item);
        //             quantitys.Add(iapData.PurchaseProducts[item].rewards[i].Quantity);
        //             Locator.Reward.ClaimItem(items[i], quantitys[i]);
        //         }
                
        //         Locator.Reward.SaveItemData();              
        //         Locator.Reward.ShowReward(items, quantitys);
        //         UIManager.Ins.UpdateAllUI();
        //     }, () => UIManager.Ins.OpenUI<ToastCanvas>().Show("Buy Fail!"));
        // }

    }
}
