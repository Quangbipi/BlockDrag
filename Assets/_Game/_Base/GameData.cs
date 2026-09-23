using Base;
using DesignPattern;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Database
{
    public static void Save<T>(T data) where T : new()
    {
        string dataString = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(typeof(T).Name, dataString);
        PlayerPrefs.Save();
    }

    public static T Load<T>() where T : new()
    {
        string key = typeof(T).Name;
        if (PlayerPrefs.HasKey(key))
        {
            return JsonConvert.DeserializeObject<T>(PlayerPrefs.GetString(key));
        }
        T data = new();
        Save(data);
        return data;
    }
}

namespace Base
{
    #region GAME DEFINE
    #region GAMEPLAY DATA
    [Serializable]
    public class LevelData
    {
        public int index;
        public Vector2Int size = new Vector2Int(8, 14);
        public LEVEL_DIFFICULTY difficulty;
        public LEVEL_PERFORMANCE_TYPE performanceType;
        public int maxCutPerformanceRequire = -1;
        public int maxKnifeCut;
        public List<int> times = new List<int> { 60, 180, 300 };
        public List<KnifeUnitData> knifeData;
        public List<HolderSlotData> holderSlotData;
        public List<RiceUnitData> riceData;
        public List<StaticRiceUnitData> staticRiceData;
        public List<MovingBeltSlotData> movingBeltSlotData;

        public LevelData() { }
        public LevelData(int index, Vector2Int size, LEVEL_DIFFICULTY difficulty, LEVEL_PERFORMANCE_TYPE performanceType, int maxCutPerformanceRequire, int maxKnifeCut, List<int> times, List<HolderSlotData> holderSlotData, List<RiceUnitData> riceData
            , List<KnifeUnitData> knifeData, List<StaticRiceUnitData> staticRiceData, List<MovingBeltSlotData> movingBeltSlotData)
        {
            this.index = index;
            this.size = size;
            this.difficulty = difficulty;
            this.performanceType = performanceType;
            this.maxCutPerformanceRequire = maxCutPerformanceRequire;
            this.maxKnifeCut = maxKnifeCut;
            this.times = times;
            this.holderSlotData = new List<HolderSlotData>(holderSlotData);
            this.riceData = new List<RiceUnitData>(riceData);
            this.knifeData = new List<KnifeUnitData>(knifeData);
            this.staticRiceData = new List<StaticRiceUnitData>(staticRiceData);
            // this.movingBeltSlotData = movingBeltSlotData;
        }
    }
    [Serializable]
    public class UnitData
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public Vector3 boxColliderSize;

        public UnitData(Transform tf, Vector3 boxColliderSize)
        {
            this.position = tf.position;
            this.rotation = tf.rotation.eulerAngles;
            this.scale = tf.localScale;
            this.boxColliderSize = boxColliderSize;
        }
    }
    [Serializable]
    public class RiceUnitData : UnitData
    {
        public List<DIRECTION> linkedDirections; // LEFT, UP, RIGHT, DOWN
        public List<DIRECTION> blockDirections;
        public PoolType poolType;
        public PLACE_TYPE placeType;
        public int capacity;

        public RiceUnitData(Transform tf, Vector3 boxColliderSize, PoolType poolType
        , List<DIRECTION> linkedDirections = null, List<DIRECTION> blockDirections = null, PLACE_TYPE placeType = PLACE_TYPE.NONE) : base(tf, boxColliderSize)
        {
            this.position = tf.position;
            this.rotation = tf.rotation.eulerAngles;
            this.scale = tf.localScale;
            this.boxColliderSize = boxColliderSize;
            this.poolType = poolType;
            this.linkedDirections = linkedDirections;
            this.blockDirections = blockDirections;
            this.placeType = placeType;
        }
    }
    [Serializable]
    public class KnifeUnitData : UnitData
    {
        public DIRECTION cutDirection;
        public List<DIRECTION> linkedDirections; // LEFT, UP, RIGHT, DOWN

        public KnifeUnitData(Transform tf, Vector3 boxColliderSize, DIRECTION cutDirection
        , List<DIRECTION> linkedDirections = null) : base(tf, boxColliderSize)
        {
            this.position = tf.position;
            this.rotation = tf.rotation.eulerAngles;
            this.scale = tf.localScale;
            this.boxColliderSize = boxColliderSize;
            this.cutDirection = cutDirection;
            this.linkedDirections = linkedDirections;
        }
    }
    [Serializable]
    public class StaticRiceUnitData : UnitData
    {
        public StaticRiceUnitData(Transform tf, Vector3 boxCollideSize) : base(tf, boxCollideSize)
        { 
            
        }
    }   

    [Serializable]
    public class HolderSlotData : UnitData
    {
        public int hintIndex;
        public PLACE_TYPE placeType;
        public List<DIRECTION> blockDirections; // LEFT, UP, RIGHT, DOWN
        public HolderSlotData(Transform tf, Vector3 boxCollideSize, int hintIndex, List<DIRECTION> blockDirections, PLACE_TYPE placeType) : base(tf, boxCollideSize)
        {
            this.blockDirections = blockDirections;
            this.placeType = placeType;
            this.hintIndex = hintIndex;
        }
    }



    [Serializable]
    public class MovingBeltSlotData : UnitData
    {
        public List<RiceUnitData> vehicleData;
        public int vehicleShowCount;
        public MovingBeltSlotData(List<RiceUnitData> vehicleData, Transform tf, Vector3 boxCollideSize, int showVehicleCount) : base(tf, boxCollideSize)
        {
            this.vehicleData = vehicleData;
            this.vehicleShowCount = showVehicleCount;
        }
    }
    
    [Serializable]
    public class PlaceTypeData
    {
        public Sprite ToppingSprite;
        public SkeletonDataAsset ToppingAnimData;
        public Sprite SlotIndicatorSprite;
    }
    
    #endregion

    #region IAP DATA
    [Serializable]
    public class IAPItem
    {
        public string Name;
        public IAP_PRODUCT_TYPE Type;
        public string Id;
        public string Description;
        public string Price;
        public List<GameData.ItemData> rewards;
        public int TimeDuration;
    }
    #endregion
    [Serializable]
    public class ItemData
    {
#if UNITY_EDITOR
#endif
        public ITEM Type;
        [PreviewField(75)]
        public Sprite Icon;
        [PreviewField(75)]
        public Sprite ShowIcon;
        public string Name;
        public ITEM_RARITY Rarity;
        public string Description;
        public int Cost;
        public int WatchVideoCount;
    }
    #endregion
    public class GameData
    {
        public SettingData setting = new();
        public UserData user = new();
        public LevelData level = new();
        public bool IsFirstTimeUser = true;

        public bool InitData()
        {
            IsFirstTimeUser = false;
            List<ITEM> items = Enum.GetValues(typeof(ITEM)).Cast<ITEM>().ToList();
            if (user.PurchasedItems == null)
            {
                user.PurchasedItems = new List<IAP_ITEM>();
            }
            if (user.ItemDatas == null)
            {
                user.ItemDatas = new ItemData[items.Count];
                IsFirstTimeUser = true;
            }
            else
            {
                if (items.Count > user.ItemDatas.Length)
                {
                    ItemData[] newData = new ItemData[items.Count];
                    for (int i = 0; i < user.ItemDatas.Length; i++)
                    {
                        newData[i] = user.ItemDatas[i];
                    }
                    user.ItemDatas = newData;
                }
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (user.ItemDatas[i] != null && user.ItemDatas[i].Item == items[i])
                    continue;
                else
                {
                    ItemData newData = new ItemData();
                    newData.Item = items[i];
                    newData.Quantity = 0;
                    user.ItemDatas[i] = newData;
                }
            }
            return IsFirstTimeUser;
        }
        public int TotalStar
        {
            get
            {
                int value = 0;
                for (int i = 0; i < level.LevelStars.Count; i++)
                {
                    value += level.LevelStars[i];
                }
                return value;
            }
        }

        public void SetStar(int level, int star)
        {
            if (level >= this.level.LevelStars.Count)
            {
                int count = level - this.level.LevelStars.Count;
                for (int i = 0; i <= count; i++)
                {
                    this.level.LevelStars.Add(0);
                }
                count = level - this.level.PassLevels.Count;
                for (int i = 0; i <= count; i++)
                {
                    this.level.PassLevels.Add(false);
                }
            }
            this.level.DeltaStar = Mathf.Clamp(star - this.level.LevelStars[level], 0, 3);
            if(star > this.level.LevelStars[level])
            {
                this.level.LevelStars[level] = star;               
            }
        }
        public int ClaimItem(ITEM item, int value)
        {
            Locator.Ads.Analytic.EarnVirtualCurrency(item.ToString(), value, "");
            ItemData data = user.ItemDatas.First(x => x.Item == item);
            data.Quantity += value;
            return data.Quantity;
        }
        public int SpendItem(ITEM item, int value)
        {
            Locator.Ads.Analytic.SpendVirtualCurrency(item.ToString(), value, "");
            ItemData data = user.ItemDatas.First(x => x.Item == item);
            data.Quantity -= value;
            return data.Quantity;
        }
        public int SetLock(ITEM item, int value)
        {
            ItemData data = user.ItemDatas.First(x => x.Item == item);
            data.LockQuantity = value;
            return data.LockQuantity;
        }
        public ItemData GetItemData(ITEM item)
        {
            return user.ItemDatas.First(x => x.Item == item);
        }
        public bool IsRemoveAds()
        {
            return GetItemData(ITEM.REMOVE_ADS).Quantity > 0;
        }
        [Serializable]
        public class UserData
        {
            // Level Progress Data
            public int normalLevelIndex;
            public int currentLevelIndex;
            public int maxHearts;
            public int watchingAdsCount = 0;
            public int playGameAdsCount = 0;
            public string lastHeartSaveTime;
            // Item Data
            public ItemData[] ItemDatas;
            public List<IAP_ITEM> PurchasedItems;
        }

        [Serializable]
        public class SettingData
        {
            public bool hapticOff;
            public bool isBgmMute;
            public bool isSfxMute;
        }
        [Serializable]
        public class ItemData
        {
            public ITEM Item;
            public int Quantity;
            public int LockQuantity;
        }
        [Serializable]
        public class LevelData
        {
            public int Action;
            public int Knife;
            public int Star;
            public int DeltaStar;
            public List<int> LevelStars = new List<int>();
            public List<bool> PassLevels = new List<bool>();
        }
    }
}