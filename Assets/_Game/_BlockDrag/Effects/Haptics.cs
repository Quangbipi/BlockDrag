using Quang.Base;
using Solo.MOST_IN_ONE;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Cổng rung thiết bị duy nhất của game: bỏ qua mọi lệnh rung khi người chơi tắt Vibration trong Setting
    /// </summary>
    public static class Haptics
    {
        /// <summary>
        /// Người chơi đã tắt rung trong Setting (thiếu Locator.Data thì coi như đang bật)
        /// </summary>
        public static bool IsOff => IsOffIn(Locator.Data?.GetData<GameData>());

        public static bool IsOffIn(GameData data)
        {
            return data?.setting != null && data.setting.hapticOff;
        }

        public static void Play(MOST_HapticFeedback.HapticTypes type, float cooldown)
        {
            if (IsOff) return;

            MOST_HapticFeedback.GenerateWithCooldown(type, cooldown);
        }
    }
}
