
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base.UI;
    using DG.Tweening;
    using Utilities.Timer;

    public class ToastCanvas : UISCanvas
    {
        public const float DELAY_TOAST_TIME = 1f;
        public Transform initPos, endPos;

        [SerializeField]
        List<ToastItemUICtrl> items;
        [SerializeField]
        ToastItemUICtrl oneSpaceLeftBanner;
        STimer timerDelay;
        private void Awake()
        {
            timerDelay = TimerManager.Ins.PopSTimer();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            TimerManager.Ins.PushSTimer(timerDelay);
        }
        public void Show(string msg, bool isWaitDelay = true)
        {
            if (isWaitDelay && timerDelay.IsStart) 
                return;
            else
                timerDelay.Stop();
            timerDelay.Start(DELAY_TOAST_TIME);
            ToastItemUICtrl toast = items.Find(x => !x.isActiveAndEnabled);
            toast.gameObject.SetActive(true);
            toast.txtMsg.SetText(msg);
            toast.transform.position = initPos.position;
            toast.cvg.alpha = 0f;
            toast.cvg.DOFade(1, 0.25f);
            toast.transform.DOMove(endPos.position, 0.5f)
                .OnComplete(() => FadeToast(toast));
        }

        public void ShowSpecial()
        {
            ToastItemUICtrl toast = oneSpaceLeftBanner;
            toast.gameObject.SetActive(true);
            toast.transform.position = initPos.position;
            toast.cvg.alpha = 0f;
            toast.cvg.DOFade(1, 0.25f);
            toast.transform.DOMove(endPos.position, 0.25f)
                .OnComplete(() => FadeToast(toast));
        }
        void FadeToast(ToastItemUICtrl toast)
        {
            toast.cvg.DOFade(0, 0.5f).SetDelay(0.5f)
                            .OnComplete(() => toast.gameObject.SetActive(false));
        }
    }
}
