using System.Collections;
using System.Collections.Generic;
using Quang.Base;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        Camera UICam;
        // Start is called before the first frame update
        void Start()
        {
            Locator.UI.SetCameraScreenSpace(Camera.main);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
