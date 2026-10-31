using UnityEngine;

namespace UI
{
    using Quang.UI;
    using Quang.Base;
    using Spine.Unity;
    using TMPro;
    using UnityEngine.UI;
    using DG.Tweening;

    public class InitLoadingCanvas : UISCanvas
    {
        [SerializeField]
        TMP_Text loadingText;
        [SerializeField]
        Slider loadingBar;
        [SerializeField]
        SkeletonGraphic loadingSpine;

        [Header("Fake Loading")]
        [Tooltip("Tự chạy loading rồi vào GameScene khi Start (tắt cho instance không dùng để vào game)")]
        [SerializeField]
        bool autoLoadOnStart = true;
        [Tooltip("Thời gian (giây) loading bar chạy từ 0% đến 100%; 0 = vào game ngay")]
        [SerializeField]
        float fakeLoadDuration = 1.5f;
        [SerializeField]
        string gameSceneName = "GameScene";
        [Tooltip("Sau khi GameScene load xong, nếu không ai gọi Dismiss trong thời gian này (giây, unscaled) thì tự tắt")]
        [SerializeField]
        float dismissTimeout = 3f;

        bool isLoadingScene;
        bool isDismissed;
        SceneGameManager sceneManager;

        public static InitLoadingCanvas Current { get; private set; }

        public Slider LoadingBar => loadingBar;

        void Awake()
        {
            Current = this;
        }

        protected override void Start()
        {
            base.Start();
            if (autoLoadOnStart)
            {
                StartFakeLoading();
            }
        }

        /// <summary>
        /// Chưa có data cần load nên chạy giả tiến độ 0 → 100% rồi vào GameScene
        /// </summary>
        public void StartFakeLoading()
        {
            SetPercentage(0f);
            if (fakeLoadDuration <= 0f)
            {
                SetPercentage(1f);
                LoadGameScene();
                return;
            }

            DOVirtual.Float(0f, 1f, fakeLoadDuration, SetPercentage)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(LoadGameScene);
        }

        public void SetPercentage(float value)
        {
            value = Mathf.Clamp01(value);
            loadingBar.value = value;
            loadingText.text = FormatLoadingText(value);
        }

        public static string FormatLoadingText(float value)
        {
            return $"LOADING... {(int)(Mathf.Clamp01(value) * 100)}%";
        }

        void LoadGameScene()
        {
            if (isLoadingScene) return;
            isLoadingScene = true;
            sceneManager = SceneGameManager.Ins;
            sceneManager.OnSceneLoaded += OnGameSceneLoaded;
            sceneManager.LoadingSceneAsync(gameSceneName);
        }

        void OnGameSceneLoaded(int sceneId)
        {
            sceneManager.OnSceneLoaded -= OnGameSceneLoaded;
            // Giữ loading che màn hình cho tới khi GameScene gọi Dismiss (cùng frame mở UI đầu tiên)
            DOVirtual.DelayedCall(dismissTimeout, Dismiss, true).SetLink(gameObject);
        }

        /// <summary>
        /// Tắt loading; GameScene gọi cùng frame mở UI đầu tiên để không lộ scene
        /// </summary>
        public void Dismiss()
        {
            if (isDismissed) return;
            isDismissed = true;
            // Canvas nằm dưới UIManager (DontDestroyOnLoad) nên phải tự huỷ
            Destroy(gameObject);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Current == this)
            {
                Current = null;
            }
            if (sceneManager != null)
            {
                sceneManager.OnSceneLoaded -= OnGameSceneLoaded;
            }
        }
    }
}
