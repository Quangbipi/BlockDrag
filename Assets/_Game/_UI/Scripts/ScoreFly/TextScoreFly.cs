using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Chữ "+N" xuất hiện tại chỗ ăn điểm rồi bay lên chữ Score:
    /// chờ → pop-in → giữ (nhích lên) → bay tới đích (tăng tốc) → thu nhỏ & mờ → onArrive
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class TextScoreFly : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Pop In")]
        [SerializeField] private float popInDuration = 0.15f;
        [SerializeField] private float popInStartScale = 0.5f;
        [SerializeField] private float popInOvershootScale = 1.1f;

        [Header("Hold")]
        [SerializeField] private float holdDuration = 0.2f;
        [Tooltip("Quãng nhích lên khi đang giữ (Canvas unit)")]
        [SerializeField] private float holdRise = 20f;

        [Header("Fly")]
        [SerializeField] private float flyDuration = 0.25f;
        [SerializeField] private Ease flyEase = Ease.InQuad;

        [Header("Absorb")]
        [SerializeField] private float absorbDuration = 0.1f;
        [SerializeField] private float absorbScale = 0.3f;

        private RectTransform rectTransform;
        private Sequence sequence;

        public int Amount { get; private set; }

        private RectTransform RectTf => rectTransform != null ? rectTransform : rectTransform = (RectTransform)transform;

        public void InitReferences(TMP_Text text, CanvasGroup group)
        {
            amountText = text;
            canvasGroup = group;
        }

        private void EnsureReferences()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (amountText == null) amountText = GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// Bắt đầu animation. startAnchoredPos là toạ độ trong parent; target là RectTransform đích (chữ Score)
        /// </summary>
        public void Play(int amount, Vector2 startAnchoredPos, RectTransform target, float startDelay, Action<TextScoreFly> onArrive)
        {
            KillSequence();
            EnsureReferences();

            Amount = amount;
            if (amountText != null)
            {
                amountText.text = "+" + amount;
            }

            RectTransform rt = RectTf;
            rt.anchoredPosition = startAnchoredPos;
            rt.localScale = Vector3.one * popInStartScale;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            transform.SetAsLastSibling();

            float popHalf = popInDuration * 0.5f;

            sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            sequence.AppendInterval(Mathf.Max(0f, startDelay));

            // Pop-in
            sequence.Append(rt.DOScale(popInOvershootScale, popHalf).SetEase(Ease.OutQuad));
            if (canvasGroup != null) sequence.Join(canvasGroup.DOFade(1f, popHalf));
            sequence.Append(rt.DOScale(1f, popHalf).SetEase(Ease.OutQuad));

            // Giữ và nhích lên
            sequence.Append(rt.DOAnchorPosY(startAnchoredPos.y + holdRise, holdDuration).SetEase(Ease.OutSine));

            // Bay tới đích: tính toạ độ lúc bắt đầu bay để đúng cả khi layout đổi
            Vector2 flyFrom = Vector2.zero;
            Vector2 flyTo = Vector2.zero;
            sequence.AppendCallback(() =>
            {
                flyFrom = rt.anchoredPosition;
                flyTo = GetTargetAnchoredPos(target, flyFrom);
            });
            sequence.Append(DOVirtual.Float(0f, 1f, flyDuration, t => rt.anchoredPosition = Vector2.LerpUnclamped(flyFrom, flyTo, t))
                .SetEase(flyEase));

            // Hút vào chữ Score
            sequence.Append(rt.DOScale(absorbScale, absorbDuration).SetEase(Ease.InQuad));
            if (canvasGroup != null) sequence.Join(canvasGroup.DOFade(0f, absorbDuration));

            sequence.OnComplete(() =>
            {
                sequence = null;
                onArrive?.Invoke(this);
            });
        }

        /// <summary>
        /// Dừng ngay, không gọi onArrive (reset ván)
        /// </summary>
        public void ForceComplete()
        {
            KillSequence();
        }

        private Vector2 GetTargetAnchoredPos(RectTransform target, Vector2 fallback)
        {
            RectTransform parent = RectTf.parent as RectTransform;
            if (target == null || parent == null) return fallback;

            Vector3 local = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
            // anchoredPosition = localPosition - điểm neo trong parent
            Vector2 anchorRef = Vector2.Scale(parent.rect.size, (RectTf.anchorMin + RectTf.anchorMax) * 0.5f - parent.pivot);
            return (Vector2)local - anchorRef;
        }

        private void KillSequence()
        {
            if (sequence != null && sequence.IsActive())
            {
                sequence.Kill();
            }
            sequence = null;
        }

        private void OnDisable()
        {
            KillSequence();
        }
    }
}
