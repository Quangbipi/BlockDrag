using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base;
    using Base.UI;
    using TMPro;
    using UnityEngine.UI;

    public class UIBoosterButton : UISButton
    {
        [SerializeField]
        protected Image addingImage;
        [SerializeField]
        protected Image frameCountImage;
        [SerializeField]
        protected Image frameImage;
        [SerializeField]
        protected TMP_Text countTxt;
        public ITEM ItemType => (ITEM)indexID;
        public Image FrameImage => frameImage;
        public void SetQuantity(int quantity)
        {
            if (quantity == 0)
            {
                addingImage.gameObject.SetActive(true);
                countTxt.gameObject.SetActive(false);
                frameCountImage.gameObject.SetActive(false);
            }
            else
            {
                addingImage.gameObject.SetActive(false);
                countTxt.gameObject.SetActive(true);
                frameCountImage.gameObject.SetActive(true);
                countTxt.text = quantity.ToString();
            }
        }
    }
}
