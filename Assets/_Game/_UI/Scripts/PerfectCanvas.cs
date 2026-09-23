using Base;
using Base.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using Solo.MOST_IN_ONE;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Timer;

namespace UI
{
    public class PerfectCanvas : UISCanvas
    {
        STimer timer;
        [SerializeField]
        RectTransform point;
        [SerializeField]
        Image background1Img;
        [SerializeField]
        Ease easeBg1;
        [SerializeField]
        Image background2Img;
        [SerializeField]
        Ease easeBg2;
        [SerializeField]
        Image fxPerfectImg;
        [SerializeField]
        GameObject fxPerfectGO;
        [SerializeField]
        Image perfectTxtImg;
        [SerializeField]
        Ease easePerfectTxtImg;


        Vector2 position;
        Vector2 bound;

        List<float> times;
        List<Action> actions;
        float camProjectileValue;
        protected void Awake()
        {
            timer = TimerManager.Ins.PopSTimer();
            times = new List<float>();
            actions = new List<Action>();

            times.Add(0.3f);
            actions.Add(() =>
            {
                background1Img.rectTransform.DOSizeDelta(Vector2.one * 5000, 0.5f).SetEase(easeBg1);
                MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.LightImpact, 0.1f);        
            });
            times.Add(0.5f);
            actions.Add(() =>
            {
                background2Img.rectTransform.DOSizeDelta(bound * 135, 0.4f).SetEase(easeBg2);
                Locator.Audio.PlaySfx(SFX_TYPE.POP);
                MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.LightImpact, 0.1f);
            });
            times.Add(0.3f);
            actions.Add(() =>
            {
                fxPerfectImg.rectTransform.sizeDelta = bound * 150 + Vector2.one * 150;
                fxPerfectGO.SetActive(true);
                MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.LightImpact, 0.1f);
                Locator.Audio.PlaySfx(SFX_TYPE.POP);
            });
            times.Add(0.25f);
            actions.Add(() =>
            {
                perfectTxtImg.rectTransform.DOScale(1f, 0.3f).SetEase(easePerfectTxtImg);
                Locator.Audio.PlaySfx(SFX_TYPE.PERFECT);
                MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.MediumImpact, 0.3f);
            });
            times.Add(2.5f);
            actions.Add(Close);
        }
        public override void Open(object param)
        {
            base.Open(param);
            point.localPosition = position;
            background1Img.rectTransform.sizeDelta = Vector2.zero;
            background2Img.rectTransform.sizeDelta = Vector2.zero;
            fxPerfectGO.SetActive(false);
            perfectTxtImg.rectTransform.localScale = Vector3.zero;
            timer.Start(times, actions, null, STimer.CAL_TYPE.ADD);
            Locator.Audio.PlaySfx(SFX_TYPE.WHOOSH);
            camProjectileValue = (float)param;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            TimerManager.Ins.PushSTimer(timer);
        }
        public virtual void ShowAnim(Vector2 position, Vector2Int bound)
        {
            this.position = position;
            this.bound.x = Mathf.Max(bound.x, bound.y);
            this.bound.y = Mathf.Max(bound.x, bound.y);
            this.bound += Vector2Int.one;

            float value = Mathf.Sqrt( Mathf.Pow(this.bound.x, 2) + Mathf.Pow(this.bound.y, 2));
            this.bound = new Vector2(value, value);
            // this.bound *= camProjectileValue / CONSTANTS.DEFAULT_PROJECTION_SIZE;

            point.localPosition = position;
            background1Img.rectTransform.sizeDelta = Vector2.zero;
            background2Img.rectTransform.sizeDelta = Vector2.zero;
            fxPerfectGO.SetActive(false);
            perfectTxtImg.rectTransform.localScale = Vector3.zero;
            timer.Start(times, actions, null, STimer.CAL_TYPE.ADD);
        }

    }
}
