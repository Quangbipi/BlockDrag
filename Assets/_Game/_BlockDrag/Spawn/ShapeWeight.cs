using System;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Trọng số spawn của một shape: earlyWeight khi độ khó = 0, lateWeight khi độ khó = 1
    /// </summary>
    [Serializable]
    public class ShapeWeight
    {
        public BlockShapeData shape;

        [Min(0f)] public float earlyWeight = 1f;
        [Min(0f)] public float lateWeight = 1f;

        public float Evaluate(float difficulty)
        {
            return Mathf.Max(0f, Mathf.Lerp(earlyWeight, lateWeight, difficulty));
        }
    }
}
