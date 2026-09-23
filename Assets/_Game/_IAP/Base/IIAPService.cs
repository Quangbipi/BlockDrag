using System;

namespace Base
{
    public interface IIAPService
    {
        public void Purchase(IAP_ITEM item, Action onPuchaseCompleted, Action onPurchaseFail = null);
    }
}


