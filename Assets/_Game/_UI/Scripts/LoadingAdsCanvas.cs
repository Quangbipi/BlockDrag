using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base.UI;
    using System;
    using Utilities.Timer;

    public class LoadingAdsCanvas : UISCanvas
    {
        Action showAdsAction;
        public override void Open(object param)
        {
            base.Open(param);
            showAdsAction = (Action)param;
            TimerManager.Ins.WaitForTime(0.6f, Close);
        }

        public override void Close()
        {
            base.Close();
            showAdsAction?.Invoke();
        }
    }
}
