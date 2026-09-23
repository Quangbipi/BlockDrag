using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Base.UI
{
    using System.Threading.Tasks;
    using Base.Init;
    using DesignPattern;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.UI;

    public class UIManager : Singleton<UIManager>, IUIService
    {
        [SerializeField]
        private RectTransform parentCanvasTf;
        [SerializeField]
        protected CanvasScaler canvasScaler;
        [SerializeField]
        private Canvas canvas;
        [SerializeField]
        protected List<AssetReference> canvasReferences;
        //dict for UI active
        private readonly Dictionary<Type, UICanvas> uiCanvas = new();

        //dict for quick query UI prefab
        private readonly Dictionary<Type, UICanvas> uiCanvasPrefab = new();

        //list from resource
        private UICanvas[] uiResources;
        public RectTransform ParentCanvasTf => parentCanvasTf;
        public Canvas Canvas => canvas;
        public CanvasScaler CanvasScaler => canvasScaler;
        public float DpUICanvasBanner
        {
            get
            {
                //DevLog.Log(DevId.Hung, "Scale: " + GameplayCanvas.scaleFactor + "Screen Density: " + MaxSdkUtils.GetScreenDensity());
                //return 168 / (canvasUI.scaleFactor * MaxSdkUtils.GetScreenDensity());
                float unitHeight = parentCanvasTf.rect.height;
                float pixelHeight = Screen.height;
                return 168 / pixelHeight * unitHeight;
            }
        }
        private void Awake()
        {
            DontDestroyOnLoad(this);
            Locator.UI = this;
        }
        public void SetCameraScreenSpace(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 10;
        }
        public Vector2 WorldPositionToUIPosition(Vector3 worldPosition, RectTransform uiCanvasRect)
        {
            // Convert world position to screen point
            Vector3 screenPoint = canvas.worldCamera.WorldToScreenPoint(worldPosition);

            // Convert screen point to canvas local position
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiCanvasRect, screenPoint, canvas.worldCamera, out localPoint);

            // Set the UI element's local position
            return localPoint;
        }

        public GameObject RaycastTarget(Vector3 position)
        {
            Ray ray = canvas.worldCamera.ScreenPointToRay(position);
            //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return hit.transform.gameObject;
            }
            return null;
        }
        public void UpdateBannerSpace(bool value)
        {
            return;
            if (DebugManager.Ins)
            {
                if (DebugManager.Ins.IsShowAds && value)
                {
                    parentCanvasTf.offsetMin = new Vector2(parentCanvasTf.offsetMin.x, DpUICanvasBanner);
                }
                else
                {
                    parentCanvasTf.offsetMin = new Vector2(parentCanvasTf.offsetMin.x, 0);
                }
            }
            else
            {
                if (value)
                {
                    parentCanvasTf.offsetMin = new Vector2(parentCanvasTf.offsetMin.x, DpUICanvasBanner);
                }
                else
                {
                    parentCanvasTf.offsetMin = new Vector2(parentCanvasTf.offsetMin.x, 0);
                }
            }
        }
        public int ConvertPixelToUnitHeight(float pixel)
        {
            float unitHeight = ((RectTransform)parentCanvasTf).rect.height;
            float pixelHeight = Screen.height;
            return (int)(pixel / pixelHeight * unitHeight);
        }

        #region Canvas
        public T OpenUI<T>() where T : UICanvas
        {
            UICanvas canvas = GetUI<T>();
            canvas.Setup();
            canvas.Open();

            return (T)canvas;
        }

        public T OpenUI<T>(object param) where T : UICanvas
        {
            UICanvas canvas = GetUI<T>();
            canvas.Setup(param);
            canvas.Open(param);
            return (T)canvas;
        }

        public UICanvas OpenUIDirectly(UICanvas ui)
        {
            UICanvas canvas = Instantiate(ui, parentCanvasTf);
            canvas.Setup();
            canvas.Open();
            return canvas;
        }

        public UICanvas OpenUIDirectly(UICanvas ui, object param)
        {
            UICanvas canvas = Instantiate(ui, parentCanvasTf);
            canvas.Setup(param);
            canvas.Open(param);
            return canvas;
        }

        public void HideUI<T>() where T : UICanvas
        {
            if (IsOpened<T>()) GetUI<T>().Hide();
        }

        public void ShowUI<T>() where T : UICanvas
        {
            if (!IsOpened<T>()) GetUI<T>().Show();
        }

        public void CloseUI<T>() where T : UICanvas
        {
            if (IsOpened<T>()) GetUI<T>().Close();
        }

        public void CloseUIDirectly(UICanvas ui)
        {
            if (!ui.gameObject.activeInHierarchy) return;
            ui.CloseDirectly();
        }

        public bool IsOpened<T>() where T : UICanvas
        {
            return IsLoaded<T>() && uiCanvas[typeof(T)].gameObject.activeInHierarchy;
        }

        public bool IsContain(UICanvas ui)
        {
            return uiCanvas.ContainsValue(ui);
        }


        public bool IsLoaded<T>() where T : UICanvas
        {
            Type type = typeof(T);
            return uiCanvas.ContainsKey(type) && uiCanvas[type] != null;
        }

        public T GetUI<T>() where T : UICanvas
        {
            if (!IsLoaded<T>())
            {
                UICanvas canvas = Instantiate(GetUIPrefab<T>(), parentCanvasTf);
                canvas.gameObject.SetActive(false);
                uiCanvas[typeof(T)] = canvas;
            }
            return uiCanvas[typeof(T)] as T;
        }

        public void PreloadUI<T>() where T : UICanvas
        {
            if (IsLoaded<T>()) return;
            UICanvas canvas = Instantiate(GetUIPrefab<T>(), parentCanvasTf);
            canvas.gameObject.SetActive(false);
            uiCanvas[typeof(T)] = canvas;
        }

        public async void GetAddressableUI<T>(Action<T> onComplete) where T : UICanvas
        {
            await LoadAddressableUIPrefab<T>();
            if (!IsLoaded<T>())
            {
                UICanvas canvas = Instantiate(GetUIPrefab<T>(), parentCanvasTf);
                canvas.gameObject.SetActive(false);
                uiCanvas[typeof(T)] = canvas;
            }
            onComplete?.Invoke(uiCanvas[typeof(T)] as T);
        }

        private T GetUIPrefab<T>() where T : UICanvas
        {
            if (uiCanvasPrefab.ContainsKey(typeof(T))) return uiCanvasPrefab[typeof(T)] as T;
            uiResources ??= Resources.LoadAll<UICanvas>("UI/");

            for (int i = 0; i < uiResources.Length; i++)
                if (uiResources[i] is T)
                {
                    uiCanvasPrefab[typeof(T)] = uiResources[i];
                    break;
                }

            return uiCanvasPrefab[typeof(T)] as T;
        }

        private async Task LoadAddressableUIPrefab<T>() where T : UICanvas
        {
            AsyncOperationHandle<GameObject> handle = default;
            if (!uiCanvasPrefab.ContainsKey(typeof(T)))
            {
                uiCanvasPrefab.Add(typeof(T), null);
                handle = Addressables.LoadAssetAsync<GameObject>($"UI/{typeof(T)}.prefab");
            }
            await handle.Task;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                uiCanvasPrefab[typeof(T)] = handle.Result.GetComponent<UICanvas>();
                Debug.Log("BƯỚC 3: Addressable đã tải xong và khởi tạo.");
            }
            else
            {
                Debug.LogError("Tải Addressable thất bại!");
            }
        }
        public void UpdateAllUI()
        {
            for (int i = 0; i < backCanvas.Count; i++)
            {
                backCanvas[i].UpdateUI();
            }
        }

        public void DestroyAllUI(HashSet<UICanvas> exception)
        {
            foreach (KeyValuePair<Type, UICanvas> item in uiCanvas)
            {
                if (item.Value is null) continue;
                if (exception.Contains(item.Value)) continue;
                if (item.Value.gameObject.activeInHierarchy) item.Value.Close();
                Destroy(item.Value.gameObject);
            }
            uiCanvas.Clear();
        }

        #endregion
        #region Back Button
        private readonly Dictionary<UICanvas, UnityAction> backActionEvents = new();
        private readonly List<UICanvas> backCanvas = new();

        private UICanvas BackTopUI
        {
            get
            {
                UICanvas canvas = null;
                if (backCanvas.Count > 0) canvas = backCanvas[^1];

                return canvas;
            }
        }


        // private void LateUpdate()
        // {
        //     if (Input.GetKey(KeyCode.Escape) && BackTopUI != null)
        //     {
        //         BackActionEvents[BackTopUI]?.Invoke();
        //     }
        // }

        public void PushBackAction(UICanvas canvas, UnityAction action)
        {
            backActionEvents.TryAdd(canvas, action);
        }

        public void AddBackUI(UICanvas canvas)
        {
            if (!backCanvas.Contains(canvas)) backCanvas.Add(canvas);
        }

        public void RemoveBackUI(UICanvas canvas)
        {
            backCanvas.Remove(canvas);
        }

        public void HideAll()
        {
            foreach (KeyValuePair<Type, UICanvas> item in uiCanvas.Where(item =>
                         item.Value != null && item.Value.gameObject.activeInHierarchy))
                item.Value.Hide();
        }
        public void CloseAll()
        {
            foreach (KeyValuePair<Type, UICanvas> item in uiCanvas.Where(item =>
                         item.Value != null && item.Value.gameObject.activeInHierarchy))
                item.Value.Close();
        }


        /// <summary>
        ///     CLear back key when comeback index UI canvas
        /// </summary>
        public void ClearBackKey()
        {
            backCanvas.Clear();
        }
        #endregion
    }
}