using UnityEngine;

namespace Base
{
    
    public interface IDailyGiftService
    {
        public DailyGiftDbModel DataModel { get; }
        public void InvokeCallbackDailyGift();
        public void ClaimDailyGift(int day);
        public void NextDay();
        public void ResetData();
    }
}
