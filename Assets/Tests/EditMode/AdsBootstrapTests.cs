using Gameplay.BlockDrag.Ads;
using NUnit.Framework;
using Quang.Ads;
using Quang.Base;

namespace Tests.EditMode
{
    public class AdsBootstrapTests
    {
        private IAdsService previousAds;

        [SetUp]
        public void SetUp()
        {
            previousAds = Locator.Ads;
            Locator.Ads = null;
        }

        [TearDown]
        public void TearDown()
        {
            Locator.Ads = previousAds;
        }

        [Test]
        public void Install_WhenAdsIsNull_AssignsNullAdsService()
        {
            AdsBootstrap.Install();

            Assert.IsInstanceOf<NullAdsService>(Locator.Ads);
        }

        [Test]
        public void Install_WhenAdsAlreadyRegistered_KeepsExistingService()
        {
            var existing = new NullAdsService();
            Locator.Ads = existing;

            AdsBootstrap.Install();

            Assert.AreSame(existing, Locator.Ads);
        }

        [Test]
        public void NullRewardShow_InvokesHiddenOnly_WithoutReward()
        {
            AdsBootstrap.Install();
            bool rewarded = false;
            bool hidden = false;

            Locator.Ads.Reward.Show(() => rewarded = true, () => hidden = true, Placement.X2_COIN);

            Assert.IsFalse(rewarded);
            Assert.IsTrue(hidden);
        }

        [Test]
        public void NullInterShow_InvokesContinuation()
        {
            AdsBootstrap.Install();
            bool continued = false;

            Locator.Ads.Inter.Show(() => continued = true, Placement.NONE);

            Assert.IsTrue(continued);
        }
    }
}
