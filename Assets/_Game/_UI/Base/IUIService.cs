using UnityEngine;

namespace Base
{
    using System;
    using Base.UI;
    using UnityEngine.UI;
    using System.Collections.Generic;
    using UnityEngine.Events;

    public interface IUIService
    {
        public RectTransform ParentCanvasTf { get; }
        public Canvas Canvas { get; }
        public CanvasScaler CanvasScaler { get; }
        public float DpUICanvasBanner{ get; }
        public void SetCameraScreenSpace(Camera cam);
        public Vector2 WorldPositionToUIPosition(Vector3 worldPosition, RectTransform uiCanvasRect);
        public GameObject RaycastTarget(Vector3 position);
        public void UpdateBannerSpace(bool value);
        public int ConvertPixelToUnitHeight(float pixel);
        public T OpenUI<T>() where T : UICanvas;
        public T OpenUI<T>(object param) where T : UICanvas;
        public UICanvas OpenUIDirectly(UICanvas ui);
        public UICanvas OpenUIDirectly(UICanvas ui, object param);
        public void HideUI<T>() where T : UICanvas;
        public void ShowUI<T>() where T : UICanvas;
        public void CloseUI<T>() where T : UICanvas;
        public void CloseUIDirectly(UICanvas ui);
        public bool IsOpened<T>() where T : UICanvas;
        public bool IsContain(UICanvas ui);
        public bool IsLoaded<T>() where T : UICanvas;
        public T GetUI<T>() where T : UICanvas;
        public void PreloadUI<T>() where T : UICanvas;
        public void GetAddressableUI<T>(Action<T> onComplete) where T : UICanvas;
        public void UpdateAllUI();
        public void DestroyAllUI(HashSet<UICanvas> exception);
        public void PushBackAction(UICanvas canvas, UnityAction action);
        public void AddBackUI(UICanvas canvas);
        public void RemoveBackUI(UICanvas canvas);
        public void HideAll();
        public void CloseAll();
        public void ClearBackKey();
    }
}
