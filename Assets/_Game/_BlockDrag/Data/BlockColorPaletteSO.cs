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

        private static readonly List<Color> Candidates = new List<Color>();

        public IReadOnlyList<Color> Colors => colors;

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
    }
}
