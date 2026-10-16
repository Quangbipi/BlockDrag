using System;
using UnityEngine;

namespace Quang.Base
{
    /// <summary>
    /// BlockDrag score/combo state exposed to presentation through <see cref="Locator.Score"/>.
    /// Compiled into Quang.Base through Quang.Base.asmref.
    /// </summary>
    public interface IScoreService
    {
        int CurrentScore { get; }
        int HighScore { get; }

        event Action<int> OnScoreChanged;
        event Action<int> OnHighScoreChanged;
        event Action<int, Vector3> OnComboTriggered; // (comboCount, worldPos) khi combo >= 2
        event Action<int, Vector3> OnPerfectClear;   // (bonus, worldPos) khi phá hàng làm bàn trống hoàn toàn
        event Action<ScoreGain> OnScoreGained;       // mỗi lần cộng điểm, phát TRƯỚC OnScoreChanged
    }
}
