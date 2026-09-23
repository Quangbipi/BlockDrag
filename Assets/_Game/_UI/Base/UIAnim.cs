using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Base.UI
{
    using DG.Tweening;
    using System;
    public abstract class UIAnim : MonoBehaviour
    {
        public event Action<int, int> _OnAnimExit;
        public event Action<int, int> _OnAnimEnter;
        [Serializable]
        public class Propertys
        {
            public ANIM Id;
            public float Time = 0.5f;
            public Ease Ease;
        }
        public enum ANIM
        {
            NONE = 0,
            SHOW = 1,
            HIDE = 2,
            IDLE = 3,
        }
        public virtual IReadOnlyList<Propertys> Datas
        {
            get;
        }

        protected ANIM state;
        protected const int LEFT_X = -850;
        protected const int UP_Y = 850;
        public virtual void OnInit() { }
        public virtual void Play() { }
        public virtual void Play(ANIM anim) { }
        public abstract void Stop();
        protected void OnAnimExit(int animCode)
        {
            _OnAnimExit?.Invoke(GetHashCode(), animCode);
        }

        protected void OnAnimEnter(int animCode)
        {
            _OnAnimEnter?.Invoke(GetHashCode(), animCode);
        }

        public virtual void SetupBaseData()
        {
            
        }
    }
}