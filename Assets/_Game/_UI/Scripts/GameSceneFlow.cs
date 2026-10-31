using System.Collections;
using UnityEngine;

namespace UI
{
    using Quang.Base;
    using Quang.UI;
    using Gameplay.BlockDrag;

    /// <summary>
    /// Vào GameScene thì mở HomeCanvas trước, khoá kéo khối cho tới khi Home đóng (nhấn Start).
    /// Kéo khối cũng bị khoá khi SettingPopup đang mở.
    /// </summary>
    public class GameSceneFlow : MonoBehaviour
    {
        [Tooltip("Input kéo khối, bị tắt khi HomeCanvas đang mở (để trống sẽ tự tìm trong scene)")]
        [SerializeField]
        BlockDragInput blockDragInput;
        [Tooltip("Số frame chờ sau khi preload HomeCanvas (loading vẫn che) để qua hitch load scene rồi mới mở Home")]
        [SerializeField]
        int settleFrames = 2;

        HomeCanvas homeCanvas;
        SettingPopup settingPopup;
        bool homeOpen;
        bool settingOpen;

        void Awake()
        {
            if (blockDragInput == null)
            {
                blockDragInput = FindObjectOfType<BlockDragInput>();
            }
        }

        IEnumerator Start()
        {
            if (Locator.UI == null)
            {
                Debug.LogWarning("[GameSceneFlow] Chưa có UIManager (chạy thẳng GameScene?), bỏ qua HomeCanvas.");
                DismissLoading();
                yield break;
            }

            homeOpen = true;
            RefreshInput();
            // Preload inactive: hitch instantiate rơi vào lúc loading còn che màn hình
            Locator.UI.GetUI<HomeCanvas>();
            settingPopup = Locator.UI.GetUI<SettingPopup>();
            if (settingPopup != null)
            {
                settingPopup._OnOpen += OnSettingOpened;
                settingPopup._OnClose += OnSettingClosed;
            }
            for (int i = 0; i < settleFrames; i++)
            {
                yield return null;
            }

            // Cùng 1 frame: mở Home (Bg full màn che scene) rồi mới tắt loading → không lộ scene, anim chạy trọn
            homeCanvas = Locator.UI.OpenUI<HomeCanvas>();
            DismissLoading();
            if (homeCanvas == null)
            {
                homeOpen = false;
                RefreshInput();
                yield break;
            }
            homeCanvas._OnOpen += OnHomeOpened;
            homeCanvas._OnClose += OnHomeClosed;
        }

        void OnDestroy()
        {
            if (homeCanvas != null)
            {
                homeCanvas._OnOpen -= OnHomeOpened;
                homeCanvas._OnClose -= OnHomeClosed;
            }
            if (settingPopup != null)
            {
                settingPopup._OnOpen -= OnSettingOpened;
                settingPopup._OnClose -= OnSettingClosed;
            }
        }

        /// <summary>
        /// Home mở lại (vd. từ LoseCanvas): khoá kéo khối, ván đã thua thì reset bàn mới khi Bg Home còn che board
        /// </summary>
        void OnHomeOpened(UISCanvas canvas)
        {
            homeOpen = true;
            RefreshInput();
            IBoardSessionService session = Locator.BoardSession;
            if (session != null && session.IsGameOver)
            {
                session.RestartGame();
            }
        }

        void OnHomeClosed(UISCanvas canvas)
        {
            Locator.UI.OpenUI<GameplayCanvas>();
            homeOpen = false;
            RefreshInput();
        }

        void OnSettingOpened(UISCanvas canvas)
        {
            settingOpen = true;
            RefreshInput();
        }

        void OnSettingClosed(UISCanvas canvas)
        {
            settingOpen = false;
            RefreshInput();
        }

        void RefreshInput()
        {
            SetInputEnabled(!homeOpen && !settingOpen);
        }

        static void DismissLoading()
        {
            if (InitLoadingCanvas.Current != null)
            {
                InitLoadingCanvas.Current.Dismiss();
            }
        }

        void SetInputEnabled(bool value)
        {
            if (blockDragInput != null)
            {
                blockDragInput.enabled = value;
            }
        }
    }
}
