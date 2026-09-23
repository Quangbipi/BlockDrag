using Base;
using Base.UI;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.Timer;

namespace UI
{
    public class LoadingCanvas : UISCanvas
    {
        Action action;
        Func<bool> condition;
        List<float> times;
        List<Action> actions;
        object[] param;
        STimer timer;
        [SerializeField]
        SkeletonGraphic boxSpineAnim;
        [SerializeField]
        SkeletonGraphic chocoSpineAnim;

        bool isConditionTrue = false;

        protected void Awake()
        {
            timer = TimerManager.Ins.PopSTimer();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            TimerManager.Ins.PushSTimer(timer);
        }
        public override void Open(object param)
        {
            base.Open(param);
            // BUG: Loading not turn off after done, add this later
            // UIManager.Ins.DestroyAllUI(new HashSet<UICanvas>() { this, UIManager.Ins.GetUI<ToastCanvas>() }); 
            isConditionTrue = false;
            action = null;
            condition = null;
            this.param = param as object[];
            PlayIdleAnim();
            if (this.param[0] is Func<bool>)
            {
                condition = this.param[0] as Func<bool>;
                action = this.param[1] as Action;
                return;
            }            
            else if (this.param[0] is List<float>)
            {
                times = this.param[0] as List<float>;
                actions = this.param[1] as List<Action>;
                timer.Start(times, actions);
            }
        }

        private void Update()
        {
            if(condition != null && !isConditionTrue)
            {
                isConditionTrue = condition.Invoke();
                if (isConditionTrue)
                {
                    timer.Start(0.5f, action);
                }
            }
        }
        public void PlayAppearAnim()
        {
            boxSpineAnim.AnimationState.SetAnimation(0, ANIM_NAME.APPEAR, false);
            boxSpineAnim.AnimationState.AddAnimation(0, ANIM_NAME.IDLE, true, 0);

            chocoSpineAnim.AnimationState.SetAnimation(0, ANIM_NAME.APPEAR, false);
            chocoSpineAnim.AnimationState.AddAnimation(0, ANIM_NAME.IDLE, true, 0);
        }
        public void PlayIdleAnim()
        {
            boxSpineAnim.AnimationState.SetAnimation(0, ANIM_NAME.IDLE, false);
            chocoSpineAnim.AnimationState.SetAnimation(0, ANIM_NAME.IDLE, false);
        }

        public override void Close()
        {
            base.Close();
            UIManager.Ins.RemoveBackUI(this);
        }
    }
}
