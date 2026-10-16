using UnityEngine;

namespace Quang.Base
{
    /// <summary>
    /// Nguồn của một lần cộng điểm
    /// </summary>
    public enum ScoreSource
    {
        Placement,
        LineClear,
        PerfectClear
    }

    /// <summary>
    /// Một lần cộng điểm: số điểm, nguồn và toạ độ world nơi điểm phát sinh (nếu có).
    /// Compiled into Quang.Base through Quang.Base.asmref.
    /// </summary>
    public readonly struct ScoreGain
    {
        public readonly int Amount;
        public readonly ScoreSource Source;
        public readonly Vector3 WorldPos;
        public readonly bool HasWorldPos;

        public ScoreGain(int amount, ScoreSource source)
        {
            Amount = amount;
            Source = source;
            WorldPos = Vector3.zero;
            HasWorldPos = false;
        }

        public ScoreGain(int amount, ScoreSource source, Vector3 worldPos)
        {
            Amount = amount;
            Source = source;
            WorldPos = worldPos;
            HasWorldPos = true;
        }
    }
}
