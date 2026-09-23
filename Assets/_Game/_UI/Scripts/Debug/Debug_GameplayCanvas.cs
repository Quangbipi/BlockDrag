using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    using Base.UI;
    using System;
    using UnityEngine.UI;

    public class Debug_GameplayCanvas : UISCanvas
    {
        public readonly Color ENABLE_COLOR = Color.white;
        public readonly Color DISABLE_COLOR = new Color(1, 1, 1, 0);
        public static Func<List<GameObject>> GetMeshObject;
        [SerializeField]
        Button toggleButton;
        [SerializeField]
        Image activeBtnImage;
        [SerializeField]
        GameObject contentRegion;
        [SerializeField]
        UIButton waterButton;
        [SerializeField]
        List<UIButton> meshButtons;

        GameObject waterObject;
        List<GameObject> meshObjects;

        private bool isActive;
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                contentRegion.SetActive(value);
                if (isActive)
                {
                    activeBtnImage.color = ENABLE_COLOR;
                }
                else
                {
                    activeBtnImage.color = DISABLE_COLOR;
                }
            }
        }

        private void Awake()
        {
            IsActive = contentRegion.activeInHierarchy;
            waterButton._OnClick += OnWaterButtonClick;
            toggleButton.onClick.AddListener(OnActiveBtnClick);
            for(int i = 0; i < meshButtons.Count; i++)
            {
                meshButtons[i]._OnClick += OnMeshButtonClick;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            waterButton._OnClick -= OnWaterButtonClick;
            toggleButton.onClick.RemoveListener(OnActiveBtnClick);
            for (int i = 0; i < meshButtons.Count; i++)
            {
                meshButtons[i]._OnClick -= OnMeshButtonClick;
            }
        }
        public override void Open(object param)
        {
            base.Open(param);
            if(meshObjects == null)
            {
                List<GameObject> list = GetMeshObject();
                meshObjects = new List<GameObject>();
                waterObject = list[0];
                for(int i = 1; i < list.Count; i++)
                {
                    meshObjects.Add(list[i]);
                }
            }
        }
        protected void OnWaterButtonClick(int index)
        {
            waterObject.SetActive(!waterObject.activeInHierarchy);
        }
        protected void OnActiveBtnClick()
        {
            IsActive = !IsActive;
        }
        protected void OnMeshButtonClick(int index)
        {
            meshObjects[index].SetActive(!meshObjects[index].activeInHierarchy);
        }
    }
}
