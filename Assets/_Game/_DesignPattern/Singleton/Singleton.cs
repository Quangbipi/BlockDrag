using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DesignPattern
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Ins
        {
            get
            {
                if (_instance is not null) return _instance;
                _instance = FindObjectOfType<T>() ?? new GameObject().AddComponent<T>();
                return _instance;
            }
        }
    }
}