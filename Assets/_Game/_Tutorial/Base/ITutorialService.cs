using System;
using UnityEngine;

namespace Base
{
     public class HandData
    {
        public Vector3 Pos1;
        public Vector3 Pos2;
        public POSITION_TYPE PosType;
    }
    public interface ITutorialService
    {
        public Func<int, HandData> GetHandInfo
        {
            get;
            set;
        }
        public bool IsHaveTutorial(int level);
        public void NextStep();
        public void CustomActions(CUSTOM_ACTION action);
        public void AddCustomCondition(CUSTOM_CONDITION condition, Func<bool> func);
        public void RemoveCustomCondition(CUSTOM_CONDITION condition);
        public bool CustomConditions(CUSTOM_CONDITION condition);

    }
}
