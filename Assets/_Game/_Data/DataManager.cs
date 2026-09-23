using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Managers
{
    using Base;
    using DesignPattern;
    using System;

    [DefaultExecutionOrder(-100)]
    public class DataManager : Singleton<DataManager>, IDataService
    {
        [SerializeField]
        private LevelDataSO levelData;
        [SerializeField]
        private GameplayData gameplayData;
        // [SerializeField]
        // private IAPData iapData;
        //[SerializeField]
        //private AnimAudioData animAudioData;
        [SerializeField]
        private PoolData poolData;
        [SerializeField]
        private GameConfig gameConfig;
        private GameData _gameData;

        public GameData GameData
        {
            get
            {
                if (_gameData == null)
                {
                    Load();
                    bool isFirstInit = _gameData.InitData();
                    _gameData.user.playGameAdsCount = 0;
                    if (isFirstInit)
                    {
                        LoadInitDataValue();
                        Debug.Log($"<color=#fffd74> {"[System]: First Init Data Value"}</color>");
                    }
                }
                return _gameData;
            }
        }
        private void Awake()
        {
            Locator.Data = this;
            DontDestroyOnLoad(this);
        }

        private GameData Load()
        {
            _gameData = Database.Load<GameData>();
            return _gameData;
        }
        public void Save()
        {
            Database.Save(_gameData);
        }
        protected void LoadInitDataValue()
        {
            // HeartSave.SetDefautsMaxHearts(gameplayData.MaxHearts);
            // _gameData.GetItemData(ITEM.HEART).Quantity = HeartSave.MaxHearts;
            Database.Save(_gameData);
        }
        public T GetSOData<T>() where T : ScriptableObject
        {
            switch (typeof(T))
            {
                case Type type when type == typeof(LevelDataSO):
                    return levelData as T;
                case Type type when type == typeof(GameplayData):
                    return gameplayData as T;
                case Type type when type == typeof(GameConfig):
                    return gameConfig as T;
                case Type type when type == typeof(PoolData):
                    return poolData as T;
                // case Type type when type == typeof(IAPData):
                //     return iapData as T;
            }
            return null;
        }

        public T GetUnit<T>(int type) where T : class
        {
            PoolType t = (PoolType)type;
            return poolData.Units[t] as T;
        }

        public T GetData<T>(int index = 0) where T : class
        {
            switch (typeof(T))
            {
                case Type type when type == typeof(GameData):
                    return GameData as T;
                case Type type when type == typeof(LevelData):
                    string json = LoadFromJson(index);
                    LevelData levelData = JsonHelper.ItemFromJson<LevelData>(json);
                    return levelData as T;
                case Type type when type == typeof(PoolData):
                    return poolData as T;

            }
            return null;
        }


        protected string LoadFromJson(int level)
        {
            TextAsset tmp = null;
            tmp = (TextAsset)Resources.Load("LevelData/" + "Lvl_" + level);
            if (tmp != null) return tmp.ToString();
            return null;
        }
    }
}