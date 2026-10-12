using Gameplay.BlockDrag;
using UnityEngine;

namespace Quang.Data
{
    /// <summary>
    /// ScriptableObject riêng của BlockDrag, đăng ký vào DataManager qua hook TryGetGameSOData.
    /// Tên field gameplayData khớp dữ liệu serialize cũ trong DataManager.prefab.
    /// </summary>
    public partial class DataManager
    {
        [SerializeField]
        private GameplayData gameplayData;
        [SerializeField]
        private BlockColorPaletteSO blockColorPalette;

        partial void TryGetGameSOData<T>(ref T result) where T : ScriptableObject
        {
            if (typeof(T) == typeof(GameplayData))
            {
                result = gameplayData as T;
            }
            else if (typeof(T) == typeof(BlockColorPaletteSO))
            {
                result = blockColorPalette as T;
            }
        }
    }
}
