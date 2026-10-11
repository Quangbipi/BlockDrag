using Gameplay.BlockDrag;
using Quang.Base;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

// GameplayData.asset was serialized by Odin with the pre-migration type names (Hung.Base assembly).
[assembly: BindTypeNameToType("Base.ITEM, Hung.Base", typeof(ITEM))]
[assembly: BindTypeNameToType("Base.ITEM", typeof(ITEM))]
[assembly: BindTypeNameToType("Base.ITEM_RARITY, Hung.Base", typeof(ITEM_RARITY))]
[assembly: BindTypeNameToType("Base.ITEM_RARITY", typeof(ITEM_RARITY))]
[assembly: BindTypeNameToType("Base.ItemData, Hung.Base", typeof(ItemData))]
[assembly: BindTypeNameToType("Base.ItemData", typeof(ItemData))]

/// <summary>
/// BlockDrag tunables, compiled into Quang.Data through Quang.Data.asmref.
/// Field names match the pre-migration GameplayData so the existing asset keeps its values.
/// </summary>
public partial class GameplayData
{
    [SerializeField]
    private Dictionary<ITEM, ItemData> items;
    public Dictionary<ITEM, ItemData> Items
    {
        get
        {
            EnsureItemIds();
            return items;
        }
    }
    [SerializeField]
    private Dictionary<ITEM_RARITY, Sprite> frames;
    public Dictionary<ITEM_RARITY, Sprite> Frames => frames;
    [SerializeField]
    private int winLevelGold;
    public int WinLevelGold => winLevelGold;
    [SerializeField]
    private int levelPerChapter;
    public int LevelPerChapter => levelPerChapter;

    /// <summary>
    /// The old ItemData stored its enum in "Type"; the framework ItemData uses "Id".
    /// Fill missing ids from the dictionary key.
    /// </summary>
    private void EnsureItemIds()
    {
        if (items == null) return;
        foreach (KeyValuePair<ITEM, ItemData> pair in items)
        {
            if (pair.Value != null && !pair.Value.Id.IsValid)
            {
                pair.Value.Id = pair.Key.ToItemId();
            }
        }
    }
}
