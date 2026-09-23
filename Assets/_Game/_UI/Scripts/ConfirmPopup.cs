
namespace UI
{
    using Base.UI;
    using System;
    using UnityEngine;

    using System.Collections.Generic;
    using TMPro;
    using Base;

    public class ConfirmPopup : BasePopup
    {
        [SerializeField]
        TMP_Text title;
        [SerializeField]
        UIButton[] groupButtons;
        [SerializeField]
        List<Action> actions;

        public override void Open(object param)
        {
            base.Open(param);
            actions = (List<Action>)param;
        }
        protected void Awake()
        {
            for (int i = 0; i < groupButtons.Length; i++)
            {
                groupButtons[i]._OnClick += OnButtonClick;
            }
        }

        public override void UpdateUI()
        {
            base.UpdateUI();
            title.text = $"Level {Locator.Data.GetData<GameData>().user.normalLevelIndex}";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < groupButtons.Length; i++)
            {
                groupButtons[i]._OnClick -= OnButtonClick;
            }
        }
        protected void OnButtonClick(int index)
        {
            switch (index)
            {
                case 0:
                    break;
                case 1:
                    break;
            }
            if (index < actions.Count)
            {
                actions[index]?.Invoke();
            }
            Close();
        }
    }
}
