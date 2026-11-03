using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Âm thanh gameplay qua Locator.Audio: tiếng đặt khối, tiếng phá hàng (1 lần mỗi lượt), nhạc nền khi vào game.
    /// Áp trạng thái mute đã lưu trong GameData.setting lúc Start (AudioManager không tự đọc GameData).
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
            IAudioService audio = Locator.Audio;
            if (audio == null) return;

            GameData data = Locator.Data?.GetData<GameData>();
            if (data != null)
            {
                audio.ToggleSfxVolume(data.setting.isSfxMute);
                audio.ToggleBgmVolume(data.setting.isBgmMute);
            }
            audio.PlayBgm(gameplayBgm);
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
