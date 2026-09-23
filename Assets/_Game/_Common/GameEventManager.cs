using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    using DesignPattern;
    [DefaultExecutionOrder(-100)]
    public class GameEventManager : Dispatcher<GameEventManager>
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }
}