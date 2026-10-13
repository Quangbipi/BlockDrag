using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Nhóm các variant cùng hình (mọi góc xoay, kể cả từ nhiều asset trùng hình) và trọng số spawn của nhóm
    /// </summary>
    public sealed class ShapeFamily
    {
        public string key;
        public readonly List<ShapeVariant> variants = new List<ShapeVariant>();
        public float earlyWeight;
        public float lateWeight;
        public int minCells = int.MaxValue;

        public float Weight(float difficulty) => Mathf.Max(0f, Mathf.Lerp(earlyWeight, lateWeight, difficulty));
    }
}
