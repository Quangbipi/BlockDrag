using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.BlockDrag
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Tham chiếu BlockSpawner (tự động tìm nếu để trống)")]
        [SerializeField] private BlockSpawner blockSpawner;

        [Header("UI Elements")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button replayButton;

        [Header("Animation Settings")]
        [SerializeField] private float animDuration = 0.35f;

        private Tween fadeTween;
        private Tween scaleTween;

        public BlockSpawner BlockSpawner
        {
            get => blockSpawner;
            set => blockSpawner = value;
        }

        private void Awake()
        {
            if (blockSpawner == null)
            {
                blockSpawner = FindObjectOfType<BlockSpawner>();
            }

            EnsureUIElements();

            // Khởi tạo trạng thái ẩn
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.zero;
            }
        }

        private void OnEnable()
        {
            if (blockSpawner != null)
            {
                blockSpawner.OnGameOver -= HandleGameOver;
                blockSpawner.OnGameOver += HandleGameOver;

                blockSpawner.OnGameRestarted -= HandleGameRestarted;
                blockSpawner.OnGameRestarted += HandleGameRestarted;
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayButtonClicked);
                replayButton.onClick.AddListener(OnReplayButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (blockSpawner != null)
            {
                blockSpawner.OnGameOver -= HandleGameOver;
                blockSpawner.OnGameRestarted -= HandleGameRestarted;
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(OnReplayButtonClicked);
            }
        }

        private void HandleGameOver()
        {
            Show();
        }

        private void HandleGameRestarted()
        {
            Hide();
        }

        /// <summary>
        /// Hiển thị popup Game Over với hiệu ứng mượt mà
        /// </summary>
        public void Show()
        {
            EnsureUIElements();

            if (messageText != null)
            {
                if (ScoreManager.Ins != null)
                {
                    messageText.text = $"Điểm: {ScoreManager.Ins.CurrentScore}  |  Kỷ Lục: {ScoreManager.Ins.HighScore}";
                }
                else
                {
                    messageText.text = "Không còn nước đi nào hợp lệ!";
                }
            }

            fadeTween?.Kill();
            scaleTween?.Kill();

            if (canvas != null)
            {
                canvas.enabled = true;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                fadeTween = canvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad);
            }

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * 0.6f;
                scaleTween = panelRect.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// Ẩn popup Game Over
        /// </summary>
        public void Hide()
        {
            fadeTween?.Kill();
            scaleTween?.Kill();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                fadeTween = canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
            }

            if (panelRect != null)
            {
                scaleTween = panelRect.DOScale(Vector3.one * 0.7f, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    if (canvas != null)
                    {
                        canvas.enabled = false;
                    }
                });
            }
        }

        private void OnReplayButtonClicked()
        {
            Hide();
            if (blockSpawner != null)
            {
                blockSpawner.RestartGame();
            }
        }

        /// <summary>
        /// Tự động sinh cấu trúc Canvas / Panel / Button hoàn chỉnh tại runtime nếu chưa được cấu hình
        /// </summary>
        private void EnsureUIElements()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = gameObject.AddComponent<Canvas>();
                }
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Luôn hiển thị trên cùng

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Tạo Dark Overlay nếu chưa có
            Transform overlayTf = transform.Find("DarkOverlay");
            if (overlayTf == null)
            {
                GameObject overlayObj = new GameObject("DarkOverlay");
                overlayObj.transform.SetParent(transform, false);
                var overlayImg = overlayObj.AddComponent<Image>();
                overlayImg.color = new Color(0f, 0f, 0f, 0.7f);

                RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.sizeDelta = Vector2.zero;
            }

            // Tạo Dialog Panel nếu chưa có
            if (panelRect == null)
            {
                Transform existingPanel = transform.Find("GameOverPanel");
                if (existingPanel != null)
                {
                    panelRect = existingPanel.GetComponent<RectTransform>();
                }
                else
                {
                    GameObject panelObj = new GameObject("GameOverPanel");
                    panelObj.transform.SetParent(transform, false);
                    var panelImg = panelObj.AddComponent<Image>();
                    panelImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f); // Màu nền tối sang trọng

                    panelRect = panelObj.GetComponent<RectTransform>();
                    panelRect.sizeDelta = new Vector2(750, 650);
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                }
            }

            // Tiêu đề Title Text
            if (titleText == null && panelRect != null)
            {
                Transform titleTf = panelRect.Find("TitleText");
                if (titleTf != null)
                {
                    titleText = titleTf.GetComponent<TMP_Text>();
                }
                else
                {
                    GameObject titleObj = new GameObject("TitleText");
                    titleObj.transform.SetParent(panelRect, false);
                    titleText = titleObj.AddComponent<TextMeshProUGUI>();
                    titleText.text = "GAME OVER";
                    titleText.fontSize = 68;
                    titleText.fontStyle = FontStyles.Bold;
                    titleText.alignment = TextAlignmentOptions.Center;
                    titleText.color = new Color(1f, 0.82f, 0.2f); // Màu vàng gold nổi bật

                    RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                    titleRect.anchorMin = new Vector2(0.5f, 1f);
                    titleRect.anchorMax = new Vector2(0.5f, 1f);
                    titleRect.pivot = new Vector2(0.5f, 1f);
                    titleRect.anchoredPosition = new Vector2(0, -90);
                    titleRect.sizeDelta = new Vector2(700, 100);
                }
            }

            // Lời nhắn Message Text
            if (messageText == null && panelRect != null)
            {
                Transform msgTf = panelRect.Find("MessageText");
                if (msgTf != null)
                {
                    messageText = msgTf.GetComponent<TMP_Text>();
                }
                else
                {
                    GameObject msgObj = new GameObject("MessageText");
                    msgObj.transform.SetParent(panelRect, false);
                    messageText = msgObj.AddComponent<TextMeshProUGUI>();
                    messageText.text = "Không còn nước đi nào hợp lệ!";
                    messageText.fontSize = 32;
                    messageText.alignment = TextAlignmentOptions.Center;
                    messageText.color = new Color(0.85f, 0.88f, 0.95f, 0.85f);

                    RectTransform msgRect = msgObj.GetComponent<RectTransform>();
                    msgRect.anchorMin = new Vector2(0.5f, 0.5f);
                    msgRect.anchorMax = new Vector2(0.5f, 0.5f);
                    msgRect.pivot = new Vector2(0.5f, 0.5f);
                    msgRect.anchoredPosition = new Vector2(0, 10);
                    msgRect.sizeDelta = new Vector2(650, 80);
                }
            }

            // Nút Replay Button
            if (replayButton == null && panelRect != null)
            {
                Transform btnTf = panelRect.Find("ReplayButton");
                if (btnTf != null)
                {
                    replayButton = btnTf.GetComponent<Button>();
                }
                else
                {
                    GameObject btnObj = new GameObject("ReplayButton");
                    btnObj.transform.SetParent(panelRect, false);
                    var btnImg = btnObj.AddComponent<Image>();
                    btnImg.color = new Color(0.18f, 0.65f, 0.95f); // Màu xanh dương hiện đại

                    replayButton = btnObj.AddComponent<Button>();
                    var colors = replayButton.colors;
                    colors.highlightedColor = new Color(0.28f, 0.75f, 1f);
                    colors.pressedColor = new Color(0.12f, 0.5f, 0.8f);
                    replayButton.colors = colors;

                    RectTransform btnRect = btnObj.GetComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.5f, 0f);
                    btnRect.anchorMax = new Vector2(0.5f, 0f);
                    btnRect.pivot = new Vector2(0.5f, 0f);
                    btnRect.anchoredPosition = new Vector2(0, 70);
                    btnRect.sizeDelta = new Vector2(450, 110);

                    // Text của nút
                    GameObject btnTextObj = new GameObject("Text");
                    btnTextObj.transform.SetParent(btnObj.transform, false);
                    var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
                    btnText.text = "CHƠI LẠI";
                    btnText.fontSize = 40;
                    btnText.fontStyle = FontStyles.Bold;
                    btnText.alignment = TextAlignmentOptions.Center;
                    btnText.color = Color.white;

                    RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
                    btnTextRect.anchorMin = Vector2.zero;
                    btnTextRect.anchorMax = Vector2.one;
                    btnTextRect.sizeDelta = Vector2.zero;
                }
            }
        }
    }
}
