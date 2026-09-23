using System;
using System.Collections.Generic;
using Base;
using Base.UI;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Timer;

namespace UI
{
    public class ShowItemPopups : BasePopup
    {
        [SerializeField]
        protected List<UIItem> uiItems;
        [SerializeField]
        protected List<RectTransform> layoutElements;
        [SerializeField]
        UIButton confirmButton;

        protected List<ITEM> items;
        protected List<int> quantitys;
        protected GameplayData gameplayData;
        protected List<Action> actions;
        protected List<float> times;
        protected STimer timer;

        protected void Awake()
        {
            confirmButton._OnClick += OnConfirmButtonClick;
            actions = new List<Action>();
            times = new List<float>();
            timer = TimerManager.Ins.PopSTimer();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            confirmButton._OnClick -= OnConfirmButtonClick;
        }

        private void OnConfirmButtonClick(int obj)
        {
            Close();
        }

        public override void Open(object param)
        {
            base.Open(param);
            gameplayData = Locator.Data.GetSOData<GameplayData>();
        }
        public void ShowItems(List<ITEM> items, List<int> quantitys)
        {
            this.items = items;
            this.quantitys = quantitys;
            times.Clear();
            actions.Clear();

            float animTime = 0.2f;
            uiItems.ForEach(x => x.gameObject.SetActive(false));
            for (int i = 0; i < items.Count; i++)
            {
                uiItems[i].gameObject.SetActive(true);
                uiItems[i].transform.localScale = Vector3.zero;
                uiItems[i].SetData(gameplayData.Items[items[i]].ShowIcon);
                switch (items[i])
                {
                    case ITEM.GOLD:
                        uiItems[i].SetData(quantitys[i], false);
                        break;
                    case ITEM.REMOVE_ADS:
                        uiItems[i].SetData("");
                        break;
                    default:
                        uiItems[i].SetData(quantitys[i]);
                        break;
                }
                uiItems[i].SetFrame(gameplayData.Frames[gameplayData.Items[items[i]].Rarity]);

                times.Add(animTime);
                int index = i;
                actions.Add(() => PlayAnim(index, UIAnim.ANIM.SHOW));
                animTime += 0.15f;
            }
            for (int i = 0; i < layoutElements.Count; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutElements[i]);
            }
            timer.Start(times, actions);
            void PlayAnim(int index, UIAnim.ANIM anim)
            {
                uiItems[index].PlayAnim(anim);
                Locator.Audio.PlaySfx(SFX_TYPE.POP);
            }
        }
    }
}
