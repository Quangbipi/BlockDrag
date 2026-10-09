using Base;
using Gameplay.BlockDrag;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class GameDataHighScoreStoreTests
    {
        private const string TestLegacyKey = "BlockDrag_HighScore_Test";

        private IDataService previousDataService;
        private FakeDataService dataService;
        private GameDataHighScoreStore store;

        private class FakeDataService : IDataService
        {
            public readonly GameData GameData = new GameData();
            public int SaveCallCount;

            public T GetData<T>(int index = 0) where T : class => GameData as T;
            public T GetSOData<T>() where T : ScriptableObject => null;
            public T GetUnit<T>(int type) where T : class => null;
            public void Save() => SaveCallCount++;
        }

        [SetUp]
        public void SetUp()
        {
            previousDataService = Locator.Data;
            dataService = new FakeDataService();
            Locator.Data = dataService;
            PlayerPrefs.DeleteKey(TestLegacyKey);

            store = new GameDataHighScoreStore(TestLegacyKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestLegacyKey);
            Locator.Data = previousDataService;
        }

        [Test]
        public void Load_ReturnsHighScoreFromGameData()
        {
            dataService.GameData.user.highScore = 420;

            Assert.AreEqual(420, store.Load());
            Assert.AreEqual(0, dataService.SaveCallCount);
        }

        [Test]
        public void Load_WithLegacyPlayerPrefsKey_MigratesValueAndDeletesKey()
        {
            PlayerPrefs.SetInt(TestLegacyKey, 900);

            int loaded = store.Load();

            Assert.AreEqual(900, loaded);
            Assert.AreEqual(900, dataService.GameData.user.highScore);
            Assert.AreEqual(1, dataService.SaveCallCount);
            Assert.IsFalse(PlayerPrefs.HasKey(TestLegacyKey));
        }

        [Test]
        public void Load_WithLegacyValueLowerThanGameData_KeepsGameDataValue()
        {
            dataService.GameData.user.highScore = 1000;
            PlayerPrefs.SetInt(TestLegacyKey, 300);

            Assert.AreEqual(1000, store.Load());
            Assert.IsFalse(PlayerPrefs.HasKey(TestLegacyKey));
        }

        [Test]
        public void Save_WithHigherValue_UpdatesGameDataAndSaves()
        {
            dataService.GameData.user.highScore = 100;

            store.Save(250);

            Assert.AreEqual(250, dataService.GameData.user.highScore);
            Assert.AreEqual(1, dataService.SaveCallCount);
        }

        [Test]
        public void Save_WithLowerValue_DoesNotOverwriteHigherHighScore()
        {
            dataService.GameData.user.highScore = 800;

            store.Save(200);

            Assert.AreEqual(800, dataService.GameData.user.highScore);
            Assert.AreEqual(0, dataService.SaveCallCount);
        }
    }
}
