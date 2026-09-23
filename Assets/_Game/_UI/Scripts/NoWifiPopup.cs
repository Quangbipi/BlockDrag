using Base.UI;
using UnityEngine;

namespace UI
{
    public class NoWifiPopup : BasePopup
    {
        [SerializeField]
        UIButton retryBtn;

        void Awake()
        {
            retryBtn._OnClick += OnRetryButtonClick;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            retryBtn._OnClick -= OnRetryButtonClick;
        }
        protected void OnRetryButtonClick(int index)
        {
            
        }
    }
}
