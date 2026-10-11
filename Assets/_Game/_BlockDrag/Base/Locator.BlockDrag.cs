using System;

namespace Quang.Base
{
    /// <summary>
    /// BlockDrag service slots, compiled into Quang.Base through Quang.Base.asmref.
    /// The *Changed events fire on every assignment (including null) so consumers can bind
    /// regardless of whether they start before or after the service registers.
    /// </summary>
    public static partial class Locator
    {
        private static IScoreService score;
        public static event Action<IScoreService> ScoreChanged;
        public static IScoreService Score
        {
            get => score;
            set
            {
                if (ReferenceEquals(score, value)) return;
                score = value;
                ScoreChanged?.Invoke(value);
            }
        }

        private static IBoardSessionService boardSession;
        public static event Action<IBoardSessionService> BoardSessionChanged;
        public static IBoardSessionService BoardSession
        {
            get => boardSession;
            set
            {
                if (ReferenceEquals(boardSession, value)) return;
                boardSession = value;
                BoardSessionChanged?.Invoke(value);
            }
        }
    }
}
