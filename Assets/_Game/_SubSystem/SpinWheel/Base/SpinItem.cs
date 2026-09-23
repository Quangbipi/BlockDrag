using System;
using UnityEngine;

namespace Base
{
    [Serializable]
    public class SpinItem
    {
        public ITEM type; // change this if detach module
        public int value;
        public float rate;

        // TEMP FOR THIS GAME, converting to RewardItemDbModel
    }
}
