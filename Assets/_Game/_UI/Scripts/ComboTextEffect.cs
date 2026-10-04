using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI
{
    public class ComboTextEffect : MonoBehaviour
    {
        [Header("UI / Text References")]
        [SerializeField] private TMP_Text comboLabelText;
        [SerializeField] private TMP_Text comboNumberText;
        [SerializeField] private Transform labelContainer;
        [SerializeField] private Transform numberContainer;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float labelScaleDuration = 0.25f;
        [SerializeField] private float numberScaleDuration = 0.25f;
        [SerializeField] private float stayDuration = 0.5f;
        [SerializeField] private float floatUpDistance = 75f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        [Header("Screen Boundary Settings")]
        [Tooltip("Khoảng cách an toàn tối thiểu với mép trái/phải màn hình (Canvas unit)")]
        [SerializeField] private float horizontalPadding = 30f;
        [Tooltip("Khoảng cách an toàn với mép trên màn hình (để khi float up vẫn không chạm header)")]
        [SerializeField] private float topPadding = 120f;
        [Tooltip("Khoảng cách an toàn tối thiểu với mép dưới màn hình")]
        [SerializeField] private float bottomPadding = 40f;

        private Sequence animSequence;
        private Vector3 initialPosition;
        private Vector2 initialAnchoredPosition;
        private RectTransform rectTransform;

        public TMP_Text ComboLabelText => comboLabelText;
        public TMP_Text ComboNumberText => comboNumberText;
        public float HorizontalPadding => horizontalPadding;
        public float TopPadding => topPadding;
        public float BottomPadding => bottomPadding;

        private void Awake()
        {
            EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (comboLabelText == null)
            {
                Transform labelTr = transform.Find("ComboLabel");
                if (labelTr != null)
                {
                    comboLabelText = labelTr.GetComponent<TMP_Text>();
                    labelContainer = labelTr;
                }
            }

            if (comboNumberText == null)
            {
                Transform numTr = transform.Find("ComboNumber");
                if (numTr != null)
                {
                    comboNumberText = numTr.GetComponent<TMP_Text>();
                    numberContainer = numTr;
                }
            }

            if (labelContainer == null && comboLabelText != null)
            {
                labelContainer = comboLabelText.transform;
            }

            if (numberContainer == null && comboNumberText != null)
            {
                numberContainer = comboNumberText.transform;
            }
        }

        public void InitReferences(TMP_Text label, TMP_Text number, Transform labelTf, Transform numberTf, CanvasGroup cvg = null)
        {
            comboLabelText = label;
            comboNumberText = number;
            labelContainer = labelTf != null ? labelTf : (label != null ? label.transform : null);
            numberContainer = numberTf != null ? numberTf : (number != null ? number.transform : null);
            canvasGroup = cvg;
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        public void KillSequence()
        {
            if (animSequence != null && animSequence.IsActive())
            {
                animSequence.Kill();
                animSequence = null;
            }
        }

        /// <summary>
        /// Giới hạn toạ độ localPoint trong phạm vi an toàn của parentRect (Canvas),
        /// tránh trường hợp chữ bị tràn mép màn hình hoặc che khuất Top Header khi trôi lên.
        /// </summary>
        public Vector2 ClampToParentBounds(RectTransform parentRect, Vector2 localPoint)
        {
            if (parentRect == null) return localPoint;

            float width = (rectTransform != null && rectTransform.rect.width > 0f) ? rectTransform.rect.width : 400f;
            float height = (rectTransform != null && rectTransform.rect.height > 0f) ? rectTransform.rect.height : 100f;

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            Rect parentBounds = parentRect.rect;

            float minX = parentBounds.xMin + halfWidth + horizontalPadding;
            float maxX = parentBounds.xMax - halfWidth - horizontalPadding;
            float minY = parentBounds.yMin + halfHeight + bottomPadding;
            // Tính cả quãng đường bay lên (floatUpDistance) để khi kết thúc animation vẫn nằm dưới mép trên an toàn
            float maxY = parentBounds.yMax - halfHeight - floatUpDistance - topPadding;

            if (minX <= maxX)
            {
                localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            }
            else
            {
                localPoint.x = (parentBounds.xMin + parentBounds.xMax) * 0.5f;
            }

            if (minY <= maxY)
            {
                localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
            }
            else
            {
                localPoint.y = (parentBounds.yMin + parentBounds.yMax) * 0.5f;
            }

            return localPoint;
        }

        /// <summary>
        /// Kích hoạt chuỗi hoạt họa Combo UI:
        /// 1. "COMBO" scale từ 0 lên 1 trước (Ease.OutBack)
        /// 2. Số combo (vd "x2") scale nảy từ 0 lên 1.2 -> 1.0 (Ease.OutBack)
        /// 3. Dừng khoảng 0.5s
        /// 4. Bay nhẹ lên trên và Fade Out biến mất
        /// </summary>
        public void Play(int comboCount, Vector3 spawnPosition, Action onComplete = null)
        {
            KillSequence();
            EnsureReferences();

            RectTransform parentRect = transform.parent as RectTransform;
            bool isUI = rectTransform != null && parentRect != null;

            if (isUI)
            {
                Camera worldCam = Camera.main;
                if (worldCam == null && Camera.allCameras.Length > 0)
                {
                    worldCam = Camera.allCameras[0];
                }

                Canvas rootCanvas = GetComponentInParent<Canvas>();
                Camera uiCam = null;
                if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : worldCam;
                }

                Vector3 screenPoint = worldCam != null ? worldCam.WorldToScreenPoint(spawnPosition) : spawnPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCam, out Vector2 localPoint);

                // Giới hạn toạ độ trong phạm vi an toàn để không bị tràn/khuất ngoài mép màn hình hoặc che header
                localPoint = ClampToParentBounds(parentRect, localPoint);

                rectTransform.anchoredPosition = localPoint;
                rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
                initialAnchoredPosition = localPoint;
            }
            else
            {
                transform.position = spawnPosition;
                initialPosition = spawnPosition;
            }

            // Đảm bảo vẽ đè lên trên các UI khác trong Canvas
            transform.SetAsLastSibling();

            gameObject.SetActive(true);

            // Cập nhật text
            if (comboLabelText != null)
            {
                comboLabelText.text = "COMBO";
                comboLabelText.alpha = 1f;
            }

            if (comboNumberText != null)
            {
                comboNumberText.text = $"x{comboCount}";
                comboNumberText.alpha = 1f;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }

            // Đặt scale ban đầu về 0
            if (labelContainer != null) labelContainer.localScale = Vector3.zero;
            if (numberContainer != null) numberContainer.localScale = Vector3.zero;

            animSequence = DOTween.Sequence();

            // Bước 1: Nhãn "COMBO" scale xuất hiện trước
            if (labelContainer != null)
            {
                animSequence.Append(labelContainer.DOScale(Vector3.one, labelScaleDuration).SetEase(Ease.OutBack));
            }

            // Bước 2: Số combo nảy scale xuất hiện ngay sau đó
            if (numberContainer != null)
            {
                animSequence.Append(numberContainer.DOScale(Vector3.one * 1.25f, numberScaleDuration * 0.7f).SetEase(Ease.OutBack));
                animSequence.Append(numberContainer.DOScale(Vector3.one, numberScaleDuration * 0.3f).SetEase(Ease.OutQuad));
            }

            // Bước 3: Dừng hiển thị
            animSequence.AppendInterval(stayDuration);

            // Bước 4: Trôi nhẹ lên trên và mờ dần
            if (isUI)
            {
                float targetY = initialAnchoredPosition.y + floatUpDistance;
                animSequence.Append(rectTransform.DOAnchorPosY(targetY, fadeOutDuration).SetEase(Ease.OutQuad));
            }
            else
            {
                float targetY = initialPosition.y + (floatUpDistance > 5f ? (floatUpDistance * 0.01f) : floatUpDistance);
                animSequence.Append(transform.DOMoveY(targetY, fadeOutDuration).SetEase(Ease.OutQuad));
            }

            if (canvasGroup != null)
            {
                animSequence.Join(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
            }
            else
            {
                if (comboLabelText != null)
                {
                    animSequence.Join(DOTween.To(() => comboLabelText.alpha, a => comboLabelText.alpha = a, 0f, fadeOutDuration).SetEase(Ease.InQuad));
                }
                if (comboNumberText != null)
                {
                    animSequence.Join(DOTween.To(() => comboNumberText.alpha, a => comboNumberText.alpha = a, 0f, fadeOutDuration).SetEase(Ease.InQuad));
                }
            }

            // Bước 5: Hoàn thành hoạt họa -> Ẩn và trả về Pool
            animSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
    }
}
