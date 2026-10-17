using Gameplay.BlockDrag;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Quang.Common;
    using TMPro;

    using Quang.UI;
    using Quang.Base;
    using Quang.Base.Init;
    using System;
    using PolyAndCode.UI;
    using DG.Tweening;
    using Quang.Utilities.Timer;
    using Quang.Utilities;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using Spine.Unity;

    public class HomeCanvas : UISCanvas
    {
        [SerializeField]
        UIButton startButton;
        [SerializeField]
        UIButton settingButton;
        [SerializeField]
        TMP_Text highScoreTxt;
        

        GameData gameData;
        GameplayData gameplayData;

        void Awake()
        {
            startButton._OnClick += OnStartGame;
            settingButton._OnClick += OnSetting;
            
        }
        
        protected override void Start()
        {
            base.Start();
           
            //highScoreTxt.text = $"{gameData.highScore}";
        }

        private void OnSetting(int obj)
        {
            throw new NotImplementedException();
        }

        private void OnStartGame(int obj)
        {
            Close();
        }
    }
}
