using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class BlockSpawnerReviveTests
    {
        private GameObject spawnerObj;
        private BlockSpawner spawner;

        [SetUp]
        public void SetUp()
        {
            spawnerObj = new GameObject("TestSpawner");
            spawner = spawnerObj.AddComponent<BlockSpawner>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(spawnerObj);
        }

        [Test]
        public void ConfirmGameOver_WhenAwaitingRevive_RaisesOnGameOverOnce()
        {
            int raised = 0;
            spawner.OnGameOver += () => raised++;
            SetField("awaitingRevive", true);

            spawner.ConfirmGameOver();
            spawner.ConfirmGameOver();

            Assert.AreEqual(1, raised);
        }

        [Test]
        public void ConfirmGameOver_WhenNotOutOfMoves_DoesNothing()
        {
            int raised = 0;
            spawner.OnGameOver += () => raised++;

            spawner.ConfirmGameOver();

            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Revive_SecondTime_IsIgnored()
        {
            int revived = 0;
            spawner.OnRevived += () => revived++;
            SetField("awaitingRevive", true);
            SetField("reviveUsed", true);

            spawner.Revive();

            Assert.AreEqual(0, revived);
            Assert.IsFalse(spawner.CanRevive);
        }

        [Test]
        public void RestartGame_ResetsCanRevive()
        {
            SetField("reviveUsed", true);
            Assert.IsFalse(spawner.CanRevive);

            spawner.RestartGame();

            Assert.IsTrue(spawner.CanRevive);
        }

        private void SetField(string name, object value)
        {
            typeof(BlockSpawner)
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(spawner, value);
        }
    }
}
