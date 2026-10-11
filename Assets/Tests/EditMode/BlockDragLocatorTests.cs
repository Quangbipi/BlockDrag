using System;
using NUnit.Framework;
using Quang.Base;
using UnityEngine;

namespace Tests.EditMode
{
    public class BlockDragLocatorTests
    {
        private sealed class FakeScoreService : IScoreService
        {
            public int CurrentScore => 0;
            public int HighScore => 0;
            public event Action<int> OnScoreChanged { add { } remove { } }
            public event Action<int> OnHighScoreChanged { add { } remove { } }
            public event Action<int, Vector3> OnComboTriggered { add { } remove { } }
        }

        private sealed class FakeBoardSession : IBoardSessionService
        {
            public int RestartCount;
            public bool IsGameOver => false;
            public event Action OnGameOver { add { } remove { } }
            public event Action OnGameRestarted { add { } remove { } }
            public void RestartGame() => RestartCount++;
        }

        private IScoreService previousScore;
        private IBoardSessionService previousSession;

        [SetUp]
        public void SetUp()
        {
            previousScore = Locator.Score;
            previousSession = Locator.BoardSession;
            Locator.Score = null;
            Locator.BoardSession = null;
        }

        [TearDown]
        public void TearDown()
        {
            Locator.Score = previousScore;
            Locator.BoardSession = previousSession;
        }

        [Test]
        public void Score_WhenAssigned_RaisesScoreChangedWithNewValue()
        {
            var service = new FakeScoreService();
            IScoreService received = null;
            void Handler(IScoreService s) => received = s;
            Locator.ScoreChanged += Handler;

            try
            {
                Locator.Score = service;
            }
            finally
            {
                Locator.ScoreChanged -= Handler;
            }

            Assert.AreSame(service, received);
        }

        [Test]
        public void Score_WhenAssignedSameValue_DoesNotRaiseAgain()
        {
            var service = new FakeScoreService();
            Locator.Score = service;
            int calls = 0;
            void Handler(IScoreService s) => calls++;
            Locator.ScoreChanged += Handler;

            try
            {
                Locator.Score = service;
            }
            finally
            {
                Locator.ScoreChanged -= Handler;
            }

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void BoardSession_WhenCleared_RaisesChangedWithNull()
        {
            Locator.BoardSession = new FakeBoardSession();
            bool raisedNull = false;
            void Handler(IBoardSessionService s) => raisedNull = s == null;
            Locator.BoardSessionChanged += Handler;

            try
            {
                Locator.BoardSession = null;
            }
            finally
            {
                Locator.BoardSessionChanged -= Handler;
            }

            Assert.IsTrue(raisedNull);
        }

        [Test]
        public void BoardSession_RestartGame_ReachesRegisteredService()
        {
            var session = new FakeBoardSession();
            Locator.BoardSession = session;

            Locator.BoardSession?.RestartGame();

            Assert.AreEqual(1, session.RestartCount);
        }
    }
}
