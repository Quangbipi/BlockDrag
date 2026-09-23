using Base;
using Base.UI;
using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
//using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIPack : MonoBehaviour
    {
        public event Action<IAP_ITEM> _OnItemPurchased;
        [SerializeField]
        Button buyButton;
        [SerializeField]
        TMP_Text costTxt;
        [SerializeField]
        IAP_ITEM item;
        [SerializeField]
        List<UIAnim> anims;

        IAPItem data;
        public IAP_ITEM Item => item;
        protected void Awake()
        {
            buyButton.onClick.AddListener(OnBuyButtonClick);
        }
        protected void OnDestroy()
        {
            buyButton.onClick.RemoveAllListeners();
        }
        public void OnInit(IAPItem data)
        {
            this.data = data;
            costTxt.text = $"{data.Price}";
        }

        public void Play(UIAnim.ANIM anim)
        {
            for(int i = 0; i < anims.Count; i++)
            {
                anims[i].Play(anim);
            }
        }
        protected void OnBuyButtonClick()
        {
            _OnItemPurchased?.Invoke(item);
        }

    }
}
