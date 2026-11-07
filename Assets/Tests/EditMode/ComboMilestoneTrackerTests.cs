using System;
using System.Collections.Generic;
using NUnit.Framework;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class ComboMilestoneTrackerTests
    {
        private ComboMilestoneTracker tracker;
        private List<int> results;

        [SetUp]
        public void SetUp()
        {
            tracker = new ComboMilestoneTracker();
            results = new List<int>();
        }

        private List<int> Feed(int combo)
        {
            tracker.Update(combo, results);
            return new List<int>(results);
        }

        [Test]
        public void Update_ComboReachesFive_ReportsFive()
        {
            CollectionAssert.IsEmpty(Feed(1));
            CollectionAssert.IsEmpty(Feed(2));
            CollectionAssert.IsEmpty(Feed(3));
            CollectionAssert.IsEmpty(Feed(4));
            CollectionAssert.AreEqual(new[] { 5 }, Feed(5));
        }

        [Test]
        public void Update_ComboSkipsOverFive_ReportsFive()
        {
            Feed(4);
            CollectionAssert.AreEqual(new[] { 5 }, Feed(6));
        }

        [Test]
        public void Update_ComboJumpsAcrossTwoMilestones_ReportsBoth()
        {
            Feed(3);
            CollectionAssert.AreEqual(new[] { 5, 10 }, Feed(11));
        }

        [Test]
        public void Update_ComboStaysAboveMilestone_ReportsOnce()
        {
            CollectionAssert.AreEqual(new[] { 5 }, Feed(5));
            CollectionAssert.IsEmpty(Feed(6));
            CollectionAssert.IsEmpty(Feed(7));
            CollectionAssert.AreEqual(new[] { 10 }, Feed(10));
        }

        [Test]
        public void Update_AfterComboDrops_ReportsMilestoneAgain()
        {
            CollectionAssert.AreEqual(new[] { 5 }, Feed(7));
            CollectionAssert.IsEmpty(Feed(1));
            CollectionAssert.AreEqual(new[] { 5 }, Feed(6));
        }

        [Test]
        public void Update_NewChainStartsAboveMilestone_ReportsIt()
        {
            Feed(1);
            CollectionAssert.AreEqual(new[] { 5 }, Feed(6));
        }

        [Test]
        public void Update_RepeatedSameCombo_DoesNotReport()
        {
            Feed(5);
            CollectionAssert.IsEmpty(Feed(5));
        }

        [Test]
        public void Constructor_NonPositiveStep_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ComboMilestoneTracker(0));
        }
    }
}
