using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public interface IProgress
    {
        public Action<float> _OnLoadingProgress { get; set; }
    }
    #region GAME
    
    public interface ILevelService
    {
        public T GetLevelData<T>(int level = 0) where T : class;
    }
    public interface IGameplayService
    {
        public void ConstructLevel(int level);
        public void DestructLevel();
        public void UsingBooster(int type);
        public int IsCanUseBooster(int type);
        public void Revive();
    }
    #endregion
}