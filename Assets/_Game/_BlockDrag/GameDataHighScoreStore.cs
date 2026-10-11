using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Lưu HighScore vào GameData.highScore qua Locator.Data (DataManager).
    /// Nếu chưa có DataManager (chạy thẳng GameScene) thì đọc/ghi GameData trực tiếp qua Database.
    /// </summary>
    public class GameDataHighScoreStore : IHighScoreStore
    {
        public const string LegacyHighScoreKey = "BlockDrag_HighScore";

        private readonly string legacyKey;
        private GameData fallbackData;

        public GameDataHighScoreStore(string legacyKey = LegacyHighScoreKey)
        {
            this.legacyKey = legacyKey;
        }

        public int Load()
        {
            GameData data = GetGameData();
            MigrateLegacyHighScore(data);
            return data.highScore;
        }

        public void Save(int highScore)
        {
            GameData data = GetGameData();
            if (highScore <= data.highScore) return;

            data.highScore = highScore;
            Persist();
        }

        /// <summary>
        /// Chuyển HighScore từ key PlayerPrefs cũ sang GameData một lần rồi xoá key cũ
        /// </summary>
        private void MigrateLegacyHighScore(GameData data)
        {
            if (!PlayerPrefs.HasKey(legacyKey)) return;

            int legacyHighScore = PlayerPrefs.GetInt(legacyKey, 0);
            if (legacyHighScore > data.highScore)
            {
                data.highScore = legacyHighScore;
            }
            Persist();

            PlayerPrefs.DeleteKey(legacyKey);
            PlayerPrefs.Save();
        }

        private GameData GetGameData()
        {
            GameData data = Locator.Data?.GetData<GameData>();
            if (data != null) return data;

            fallbackData ??= Database.Load<GameData>();
            return fallbackData;
        }

        private void Persist()
        {
            if (Locator.Data != null)
            {
                Locator.Data.Save();
            }
            else if (fallbackData != null)
            {
                Database.Save(fallbackData);
            }
        }
    }
}
