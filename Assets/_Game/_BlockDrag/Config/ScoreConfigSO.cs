using UnityEngine;

namespace Gameplay.BlockDrag
{
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "BlockDrag/Score Config")]
    public class ScoreConfigSO : ScriptableObject
    {
        [Header("Block Placement Score")]
        [Tooltip("Số điểm cộng thêm cho mỗi SingleBlock khi đặt BlockShape xuống bàn cờ (mặc định 1 điểm / 1 block con)")]
        [SerializeField] private int pointsPerSingleBlock = 1;

        [Header("Line Clear Score")]
        [Tooltip("Số điểm cộng thêm cho mỗi hàng ngang hoặc cột dọc bị phá hủy (mặc định 25 điểm / dòng)")]
        [SerializeField] private int pointsPerLine = 25;

        [Header("Combo Configuration")]
        [Tooltip("Thời gian (giây) cửa sổ tính combo sau khi phá hàng/cột lần đầu tiên (mặc định 5s)")]
        [SerializeField] private float initialComboDuration = 5f;

        [Tooltip("Thời gian (giây) cộng thêm vào đồng hồ đếm ngược mỗi khi người chơi nối tiếp combo (mặc định 3s)")]
        [SerializeField] private float comboBonusDuration = 3f;

        [Tooltip("Hệ số điểm thưởng combo nhân với (số combo - 1). Ví dụ: combo 2 = +25, combo 3 = +50, ... (mặc định 25)")]
        [SerializeField] private int comboBonusMultiplier = 25;

        public int PointsPerSingleBlock => pointsPerSingleBlock;
        public int PointsPerLine => pointsPerLine;
        public float InitialComboDuration => initialComboDuration;
        public float ComboBonusDuration => comboBonusDuration;
        public int ComboBonusMultiplier => comboBonusMultiplier;

        /// <summary>
        /// Tạo một instance cấu hình mặc định trong trường hợp chưa gán asset trên Inspector
        /// </summary>
        public static ScoreConfigSO CreateDefault()
        {
            var config = CreateInstance<ScoreConfigSO>();
            config.pointsPerSingleBlock = 1;
            config.pointsPerLine = 25;
            config.initialComboDuration = 5f;
            config.comboBonusDuration = 3f;
            config.comboBonusMultiplier = 25;
            return config;
        }
    }
}
