using System.Collections.Generic;
using NUnit.Framework;
using Gameplay.BlockDrag;
using Quang.Base;

namespace Tests.EditMode
{
    public class BlockDragAnalyticsTests
    {
        private sealed class RecordingAnalytics : IAnalyticsService
        {
            public readonly List<(string name, AnalyticsCategory category, IReadOnlyDictionary<string, object> parameters)> Events =
                new List<(string, AnalyticsCategory, IReadOnlyDictionary<string, object>)>();

            public void LogEvent(string name, AnalyticsCategory category, IReadOnlyDictionary<string, object> parameters = null)
                => Events.Add((name, category, parameters));

            public void SetUserProperty(string key, string value) { }
            public void AdsRewardOffer(Placement place) { }
            public void AdsRewardClick(Placement place) { }
            public void AdsRewardShow(Placement place) { }
            public void AdsRewardShowFail(Placement place, string error) { }
            public void AdsRewardComplete(Placement place, string type) { }
            public void AdsRewardLoadComplete() { }
            public void AdsRewardLoad() { }
            public void AdsRewardLoadFail() { }
            public void AdsInterFail(string error) { }
            public void AdsInterLoad() { }
            public void AdsInterShow(Placement place) { }
            public void AdsInterClick() { }
            public void AdsInterLoadComplete() { }
            public void AdsInterComplete() { }
            public void BuyIAPComplete(IAP_ITEM item, Placement placement) { }
            public void TutorialStep(string name, int step) { }
            public void LevelTrackEvent(LEVEL_STATE state, int level, int stars, bool firstPass, bool isFtu) { }
            public void EarnVirtualCurrency(string name, long value, string source) { }
            public void SpendVirtualCurrency(string name, long value, string itemName) { }
        }

        [Test]
        public void LogGameLose_SendsNameCategoryAndParams()
        {
            var analytics = new RecordingAnalytics();

            BlockDragAnalytics.LogGameLose(analytics, 1200, 3400);

            Assert.AreEqual(1, analytics.Events.Count);
            var e = analytics.Events[0];
            Assert.AreEqual("game_lose", e.name);
            Assert.AreEqual(AnalyticsCategory.Design, e.category);
            Assert.AreEqual(1200, e.parameters["score"]);
            Assert.AreEqual(3400, e.parameters["high_score"]);
        }

        [Test]
        public void LogComboMilestone_SendsNameCategoryAndParams()
        {
            var analytics = new RecordingAnalytics();

            BlockDragAnalytics.LogComboMilestone(analytics, 10, 560);

            Assert.AreEqual(1, analytics.Events.Count);
            var e = analytics.Events[0];
            Assert.AreEqual("combo_milestone", e.name);
            Assert.AreEqual(AnalyticsCategory.Design, e.category);
            Assert.AreEqual(10, e.parameters["combo"]);
            Assert.AreEqual(560, e.parameters["score"]);
        }

        [Test]
        public void Log_WhenServiceNull_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => BlockDragAnalytics.LogGameLose(null, 1, 1));
            Assert.DoesNotThrow(() => BlockDragAnalytics.LogComboMilestone(null, 5, 1));
        }
    }
}
