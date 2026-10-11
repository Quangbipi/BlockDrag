using System;

namespace Quang.Base
{
    /// <summary>
    /// BlockDrag round lifecycle (game over / restart) exposed through <see cref="Locator.BoardSession"/>.
    /// Compiled into Quang.Base through Quang.Base.asmref.
    /// </summary>
    public interface IBoardSessionService
    {
        bool IsGameOver { get; }

        event Action OnGameOver;
        event Action OnGameRestarted;

        void RestartGame();
    }
}
