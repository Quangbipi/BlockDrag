using System;

namespace Quang.Base
{
    /// <summary>
    /// BlockDrag round lifecycle (out of moves / revive / game over / restart) exposed through <see cref="Locator.BoardSession"/>.
    /// Compiled into Quang.Base through Quang.Base.asmref.
    /// </summary>
    public interface IBoardSessionService
    {
        /// <summary>True while a revive is being offered and after the final lose.</summary>
        bool IsGameOver { get; }

        /// <summary>The run has not used its revive yet.</summary>
        bool CanRevive { get; }

        /// <summary>No shape in the tray fits. UI either offers a revive or calls <see cref="ConfirmGameOver"/>.</summary>
        event Action OnOutOfMoves;

        /// <summary>Final lose: raised by <see cref="ConfirmGameOver"/>.</summary>
        event Action OnGameOver;

        /// <summary>The revive hand has been spawned and play continues.</summary>
        event Action OnRevived;

        event Action OnGameRestarted;

        /// <summary>Replace the tray with a hand that can clear a line and resume. Ignored unless out of moves and <see cref="CanRevive"/>.</summary>
        void Revive();

        /// <summary>Revive declined or timed out: raise <see cref="OnGameOver"/> once.</summary>
        void ConfirmGameOver();

        void RestartGame();
    }
}
