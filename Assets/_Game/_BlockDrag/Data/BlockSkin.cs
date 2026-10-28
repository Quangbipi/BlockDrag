using System;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Skin của khối: có sprite → dùng sprite (tint trắng), không có → tint sprite mặc định bằng color.
    /// Ở chế độ sprite, color là màu đại diện của sprite.
    /// </summary>
    [Serializable]
    public struct BlockSkin
    {
        public Sprite sprite;
        public Color color;

        public bool HasSprite => sprite != null;

        public static BlockSkin FromColor(Color color)
        {
            return new BlockSkin { sprite = null, color = color };
        }

        /// <summary>
        /// Có sprite thì so sprite, không thì so RGB (bỏ qua alpha)
        /// </summary>
        public bool SameAs(BlockSkin other)
        {
            if (HasSprite || other.HasSprite)
            {
                return sprite == other.sprite;
            }

            return Mathf.Approximately(color.r, other.color.r)
                && Mathf.Approximately(color.g, other.color.g)
                && Mathf.Approximately(color.b, other.color.b);
        }
    }
}
