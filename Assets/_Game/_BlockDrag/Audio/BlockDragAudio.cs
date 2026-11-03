using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Âm thanh gameplay qua Locator.Audio: tiếng đặt khối, tiếng phá hàng (1 lần mỗi lượt), nhạc nền khi vào game.
    /// Trạng thái mute đã lưu do AudioManager tự đọc từ GameData.
    /// </summary>
    [AddComponentMenu("Gameplay/Block Drag Audio")]
    public class BlockDragAudio : MonoBehaviour
    {
        [Tooltip("Tham chiếu BlockGrid (tự tìm nếu để trống)")]
        [SerializeField] private BlockGrid blockGrid;
        [SerializeField] private BGM_TYPE gameplayBgm = BGM_TYPE.BACK_GROUND_1;

        private void Awake()
        {
            if (blockGrid == null)
            {
                blockGrid = FindObjectOfType<BlockGrid>();
            }
        }

        private void OnEnable()
        {
            if (blockGrid == null) return;
            blockGrid.OnShapePlaced += HandleShapePlaced;
            blockGrid.OnLinesCleared += HandleLinesCleared;
        }

        private void OnDisable()
        {
            if (blockGrid == null) return;
            blockGrid.OnShapePlaced -= HandleShapePlaced;
            blockGrid.OnLinesCleared -= HandleLinesCleared;
        }

        private void Start()
        {
            Locator.Audio?.PlayBgm(gameplayBgm);
        }

        private void HandleShapePlaced()
        {
            Locator.Audio?.PlaySfx(SFX_TYPE.BLOCK_PLACE);
        }

        private void HandleLinesCleared(int rows, int cols, int cells)
        {
            Locator.Audio?.PlaySfx(SFX_TYPE.LINE_CLEAR);
        }
    }
}
