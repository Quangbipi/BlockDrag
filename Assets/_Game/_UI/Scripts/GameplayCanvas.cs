using Base.UI;
using Common;
using UnityEngine;

namespace UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Base;
    using DesignPattern;
    using DG.Tweening;
    using Solo.MOST_IN_ONE;
    using TMPro;
    using UnityEngine.UI;
    using Utilities.Timer;

    public class GameplayCanvas : UISCanvas
    {
        [SerializeField] private UIButton _settingBtn;

        [SerializeField] private TMP_Text HightScoreTxt;

        [SerializeField] private TMP_Text ScoreTxt;


        protected ToastCanvas toastCanvas;

        void Awake()
        {

            _settingBtn._OnClick += OnSettingBtnClick;

        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            _settingBtn._OnClick -= OnSettingBtnClick;

        }
        public override void Open(object param = null)
        {
            base.Open(param);
            toastCanvas = UIManager.Ins.OpenUI<ToastCanvas>();
        }

        protected void OnSettingBtnClick(int index)
        {
            //UIManager.Ins.OpenUI<SettingPopup>();
        }

    }
}
