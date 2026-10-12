using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Cấu hình sinh khối: trọng số từng shape và tham số độ khó.
    /// Shape không có trong weights nhận trọng số 1/1; trọng số 0 thì không bao giờ ra.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "BlockDrag/Spawn Config")]
    public class SpawnConfigSO : ScriptableObject
    {
        [SerializeField] private SpawnSettings settings = SpawnSettings.CreateDefault();

        [Tooltip("Trọng số spawn theo shape: early khi độ khó = 0, late khi độ khó = 1")]
        [SerializeField] private List<ShapeWeight> weights = new List<ShapeWeight>();

        public SpawnSettings Settings => settings;
        public IReadOnlyList<ShapeWeight> Weights => weights;
    }
}
