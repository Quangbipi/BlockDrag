using UnityEngine;

namespace Base
{
    public interface IPiggyBankService
    {
        int PiggyGoldProgress { get; }
        void IncreasePiggyProgress();
        void ResetPiggyProgress();
    }
}
