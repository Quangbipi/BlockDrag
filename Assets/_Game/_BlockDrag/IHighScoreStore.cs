namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Nơi lưu trữ lâu dài điểm kỷ lục (HighScore)
    /// </summary>
    public interface IHighScoreStore
    {
        int Load();
        void Save(int highScore);
    }
}
