using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Bộ màu random cho khối khi spawn. Nằm trong Quang.Data (qua asmref) để DataManager serialize được.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockColorPalette", menuName = "Game/Block Color Palette")]
    public class BlockColorPaletteSO : ScriptableObject
    {
        [Tooltip("Bộ màu dùng để random cho khối khi spawn")]
        [SerializeField] private List<Color> colors = new List<Color>();

        [Tooltip("Bộ sprite cho khối. Có ít nhất 1 sprite => dùng chế độ sprite, trống => dùng bộ màu ở trên")]
        [SerializeField] private List<BlockSkin> sprites = new List<BlockSkin>();

        private static readonly List<Color> Candidates = new List<Color>();
        private static readonly List<BlockSkin> SkinCandidates = new List<BlockSkin>();

        public IReadOnlyList<Color> Colors => colors;
        public IReadOnlyList<BlockSkin> Sprites => sprites;

        public bool UseSprites
        {
            get
            {
                if (sprites == null) return false;
                for (int i = 0; i < sprites.Count; i++)
                {
                    if (sprites[i].HasSprite) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Chọn ngẫu nhiên một skin, ưu tiên skin chưa có trong exclude.
        /// Có sprite => random trong list sprites (bỏ entry thiếu sprite), không thì random trong list colors.
        /// Alpha luôn = 1.
        /// </summary>
        public bool TryGetRandomSkin(IReadOnlyList<BlockSkin> exclude, out BlockSkin skin)
        {
            skin = BlockSkin.FromColor(Color.white);

            SkinCandidates.Clear();
            if (UseSprites)
            {
                for (int i = 0; i < sprites.Count; i++)
                {
                    if (sprites[i].HasSprite)
                    {
                        SkinCandidates.Add(sprites[i]);
                    }
                }
            }
            else if (colors != null)
            {
                for (int i = 0; i < colors.Count; i++)
                {
                    SkinCandidates.Add(BlockSkin.FromColor(colors[i]));
                }
            }

            if (SkinCandidates.Count == 0) return false;

            int unusedCount = 0;
            for (int i = 0; i < SkinCandidates.Count; i++)
            {
                if (!ContainsSkin(exclude, SkinCandidates[i]))
                {
                    // Dồn skin chưa dùng lên đầu list
                    SkinCandidates[unusedCount] = SkinCandidates[i];
                    unusedCount++;
                }
            }

            skin = unusedCount > 0
                ? SkinCandidates[Random.Range(0, unusedCount)]
                : SkinCandidates[Random.Range(0, SkinCandidates.Count)];
            skin.color.a = 1f;
            SkinCandidates.Clear();
            return true;
        }

        /// <summary>
        /// Chọn ngẫu nhiên một màu, ưu tiên màu chưa có trong exclude.
        /// Nếu mọi màu đều đã bị exclude thì random trên toàn bộ list. Alpha luôn = 1.
        /// </summary>
        public bool TryGetRandomColor(IReadOnlyList<Color> exclude, out Color color)
        {
            color = Color.white;
            if (colors == null || colors.Count == 0) return false;

            Candidates.Clear();
            for (int i = 0; i < colors.Count; i++)
            {
                if (!Contains(exclude, colors[i]))
                {
                    Candidates.Add(colors[i]);
                }
            }

            color = Candidates.Count > 0
                ? Candidates[Random.Range(0, Candidates.Count)]
                : colors[Random.Range(0, colors.Count)];
            color.a = 1f;
            Candidates.Clear();
            return true;
        }

        private static bool Contains(IReadOnlyList<Color> list, Color target)
        {
            if (list == null) return false;

            // So sánh RGB vì màu trả ra đã bị ép alpha = 1
            for (int i = 0; i < list.Count; i++)
            {
                Color c = list[i];
                if (Mathf.Approximately(c.r, target.r) && Mathf.Approximately(c.g, target.g) && Mathf.Approximately(c.b, target.b))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ContainsSkin(IReadOnlyList<BlockSkin> list, BlockSkin target)
        {
            if (list == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].SameAs(target))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
