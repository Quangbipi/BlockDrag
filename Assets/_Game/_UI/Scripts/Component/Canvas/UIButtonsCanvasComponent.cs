using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Base.UI
{
    public class UIButtonsCanvasComponent : UICanvasComponent 
    {
        // Start is called before the first frame update
        [SerializeField]
        UIButton[] UIButtons;

        public override T Get<T>(int index = 0) 
        {
            if (index < 0 || index >= UIButtons.Length) return default(T);
            return UIButtons[index] as T;
        }
    }
}