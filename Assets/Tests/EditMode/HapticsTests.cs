using NUnit.Framework;
using Gameplay.BlockDrag;
using Quang.Base;

namespace Tests.EditMode
{
    public class HapticsTests
    {
        [Test]
        public void IsOffIn_WhenHapticOffTrue_ReturnsTrue()
        {
            GameData data = new GameData();
            data.setting.hapticOff = true;

            Assert.IsTrue(Haptics.IsOffIn(data));
        }

        [Test]
        public void IsOffIn_WhenHapticOffFalse_ReturnsFalse()
        {
            GameData data = new GameData();
            data.setting.hapticOff = false;

            Assert.IsFalse(Haptics.IsOffIn(data));
        }

        [Test]
        public void IsOffIn_WhenDataNull_ReturnsFalse()
        {
            Assert.IsFalse(Haptics.IsOffIn(null));
        }
    }
}
