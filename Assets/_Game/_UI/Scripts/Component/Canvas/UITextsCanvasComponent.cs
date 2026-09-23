using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    using Base.UI;
    public class UITextsCanvasComponent : UICanvasComponent
    {
        [SerializeField]
        Text[] Texts;
        public override T Get<T>(int index = 0)
        {
            if (index < 0 || index >= Texts.Length) return default(T);
            return Texts[index] as T;
        }
    }
}