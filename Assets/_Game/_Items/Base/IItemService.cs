using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public interface IItemService
    {
        public void ClaimItem(List<GameData.ItemData> listRwItem, bool showDrop = false, Action callback = null);
        public void ClaimItem(ITEM item, int quantity, object data = null);
        public void SpendItem(ITEM item, int quantity);
        public void SetLock(ITEM item, int quantity);
        public GameData.ItemData GetItem(ITEM item);
        public void SaveItemData();
        public void ShowUI(bool value);
        public void ShowReward(List<ITEM> items, List<int> quantitys);
        public void SetSpawnPosition(Transform transform);
    }
}
