using UnityEngine;

namespace Base
{
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
    public enum ITEM_RARITY
    {
        NONE = -1,
        NORMAL = 0,
        RARE = 1,
    }
}
