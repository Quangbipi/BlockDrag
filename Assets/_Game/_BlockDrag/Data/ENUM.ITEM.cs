using Quang.Base;
using System;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Legacy item enum kept for BlockDrag code and GameplayData.asset keys.
    /// The framework identifies items by <see cref="ItemId"/>; convert with <see cref="ItemExtensions.ToItemId"/>.
    /// </summary>
    public enum ITEM
    {
        HINT_BOOSTER = 0,
        KNIFE_BOOSTER = 1,
        RESET_BOOSTER = 2,
        GOLD = 3,
        HEART = 4,
        REMOVE_ADS = 5,
        REVIVE = 6,

        // TEMPORARY, Change the data model to handle with list item later
        ALL_BOOSTER = 7,
        REFRESH_CLEAR_BOOSTER = 8,
        UNLIMITED_HEART = 9,
    }

    public static class ItemExtensions
    {
        public static ItemId ToItemId(this ITEM item)
        {
            return item switch
            {
                ITEM.HINT_BOOSTER => BaseItemIds.HintBooster,
                ITEM.KNIFE_BOOSTER => BaseItemIds.KnifeBooster,
                ITEM.RESET_BOOSTER => BaseItemIds.ResetBooster,
                ITEM.GOLD => BaseItemIds.Gold,
                ITEM.HEART => BaseItemIds.Heart,
                ITEM.REMOVE_ADS => BaseItemIds.RemoveAds,
                ITEM.REVIVE => BaseItemIds.Revive,
                ITEM.ALL_BOOSTER => BaseItemIds.AllBooster,
                ITEM.REFRESH_CLEAR_BOOSTER => BaseItemIds.RefreshClearBooster,
                ITEM.UNLIMITED_HEART => BaseItemIds.UnlimitedHeart,
                _ => throw new ArgumentOutOfRangeException(nameof(item), item, null),
            };
        }

        public static bool TryToItem(this ItemId id, out ITEM item)
        {
            foreach (ITEM value in Enum.GetValues(typeof(ITEM)))
            {
                if (value.ToItemId() == id)
                {
                    item = value;
                    return true;
                }
            }
            item = default;
            return false;
        }

        /// <summary>Legacy enum of an ItemData from GameplayData.Items (old ItemData.Type).</summary>
        public static ITEM GetItemType(this ItemData data)
        {
            return data.Id.TryToItem(out ITEM item) ? item : default;
        }
    }
}
