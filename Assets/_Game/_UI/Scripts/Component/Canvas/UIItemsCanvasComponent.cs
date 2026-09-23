using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base.UI;
    public class UIItemsCanvasComponent : UICanvasComponent
    {
        [SerializeField]
        UIItem[] UIItems;

        public override T Get<T>(int index) 
        {
            if (index < 0 || index >= UIItems.Length) return default(T);
            return UIItems[index] as T;
        }

        public void Set(UIItemData[] datas)
        {
            for(int i = 0; i < UIItems.Length; i++)
            {
                if(i < datas.Length)
                {
                    UIItems[i].gameObject.SetActive(true);
                    UIItems[i].SetData(datas[i]);
                }
                else
                {
                    UIItems[i].gameObject.SetActive(false);
                }
            }
        }
    }
}