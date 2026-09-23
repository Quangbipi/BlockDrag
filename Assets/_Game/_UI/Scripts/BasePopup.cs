using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    using Base.UI;
    using Base;
    public class BasePopup : UISCanvas
    {
        [SerializeField]
        Button backgroundCloseButton;

        protected override void Start()
        {
            base.Start();
            backgroundCloseButton?.onClick.AddListener(Close);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            backgroundCloseButton?.onClick.RemoveListener(Close);
        }
        public override void Open(object param)
        {
            base.Open(param);
            Locator.Audio?.PlaySfx(SFX_TYPE.POPUP_SHOW);
        }

        public override void Hide()
        {
            base.Hide();
            Locator.Audio?.PlaySfx(SFX_TYPE.POPUP_HIDE);
        }
        
        protected void LockBgCloseInteractable(bool isLock)
        {
            backgroundCloseButton.interactable = !isLock;
        }
    }
}
