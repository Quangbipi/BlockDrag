using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    using DG.Tweening;
    using Quang.Base;
    using Quang.UI;
    using TMPro;

    /// <summary>
    /// Popup hồi sinh khi hết nước đi: đếm ngược 5s (vòng progress tụt theo giây, đỏ từ số 2),
    /// Skip hiện sau 2s. REVIVE → BoardSession.Revive(); Skip / hết giờ → BoardSession.ConfirmGameOver() → LoseCanvas.
    /// </summary>
    public class RevivePopup : BasePopup
    {
        [Header("Buttons")]
        [SerializeField] UIButton reviveButton;
        [SerializeField] UIButton skipButton;
        [SerializeField] CanvasGroup skipGroup;

        [Header("Texts")]
        [SerializeField] TMP_Text countTxt;
        [SerializeField] TMP_Text scoreTxt;

        [Header("Ring")]
        [SerializeField] Image ringFill;
        [SerializeField] Sprite ringYellow;
        [SerializeField] Sprite ringRed;

        [Header("Colors")]
        [SerializeField] Color numberNormal = Color.white;
        [SerializeField] Color numberRed = new Color32(0xF0, 0x44, 0x4F, 0xFF);

        [Header("Timing")]
        [SerializeField] int countdownSeconds = 5;
        [SerializeField] float skipDelay = 2f;
        [SerializeField] float zeroHold = 0.3f;
        [SerializeField] int redFromSecond = 2;
        [Tooltip("Thời gian vòng chạy mượt tới mốc mới mỗi giây; 0 = nhảy bậc như video tham chiếu")]
        [SerializeField] float fillStepDuration = 0.2f;
        [SerializeField] float skipFadeDuration = 0.2f;

        ReviveCountdown countdown;
        IBoardSessionService session;
        bool resolved;

        void Awake()
        {
            countdown = new ReviveCountdown(countdownSeconds, skipDelay, zeroHold, redFromSecond);
            countdown.OnSecondChanged += HandleSecondChanged;
            countdown.OnSkipAvailable += ShowSkip;
            countdown.OnExpired += Decline;

            if (reviveButton != null) reviveButton._OnClick += OnReviveClick;
            if (skipButton != null) skipButton._OnClick += OnSkipClick;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillTweens();
            UnbindSession();
            if (countdown != null)
            {
                countdown.OnSecondChanged -= HandleSecondChanged;
                countdown.OnSkipAvailable -= ShowSkip;
                countdown.OnExpired -= Decline;
            }
            if (reviveButton != null) reviveButton._OnClick -= OnReviveClick;
            if (skipButton != null) skipButton._OnClick -= OnSkipClick;
        }

        public override void Open(object param = null)
        {
            base.Open(param);
            resolved = false;
            KillTweens();

            BindSession(Locator.BoardSession);
            ShowScore();
            SetSkipVisible(false, false);
            if (ringFill != null) ringFill.fillAmount = 1f;
            if (reviveButton != null) reviveButton.SetInteractable(true);

            countdown.Start();
        }

        protected override void OnClose()
        {
            base.OnClose();
            countdown?.Stop();
            KillTweens();

            // Bị đóng từ nơi khác (scope exit...) khi chưa quyết định: coi như bỏ qua để ván không kẹt
            IBoardSessionService target = session;
            UnbindSession();
            if (!resolved)
            {
                resolved = true;
                target?.ConfirmGameOver();
            }
        }

        /// <summary>
        /// Nút Back của Android = Skip
        /// </summary>
        protected override void OnBackKey()
        {
            Decline();
        }

        void Update()
        {
            countdown?.Tick(Time.unscaledDeltaTime);
        }

        void ShowScore()
        {
            if (scoreTxt == null) return;
            int score = Locator.Score != null ? Locator.Score.CurrentScore : 0;
            scoreTxt.text = score.ToString("N0", CultureInfo.InvariantCulture);
        }

        void HandleSecondChanged(int shown)
        {
            bool red = countdown.IsRed;
            if (countTxt != null)
            {
                countTxt.text = shown.ToString(CultureInfo.InvariantCulture);
                countTxt.color = red ? numberRed : numberNormal;
            }

            if (ringFill == null) return;

            Sprite sprite = red ? ringRed : ringYellow;
            if (sprite != null) ringFill.sprite = sprite;

            float target = (float)shown / countdown.Seconds;
            DOTween.Kill(ringFill);
            if (fillStepDuration <= 0f)
            {
                ringFill.fillAmount = target;
            }
            else
            {
                ringFill.DOFillAmount(target, fillStepDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(ringFill);
            }
        }

        void ShowSkip()
        {
            SetSkipVisible(true, true);
        }

        void SetSkipVisible(bool visible, bool animate)
        {
            if (skipButton != null) skipButton.SetInteractable(visible);
            if (skipGroup == null) return;

            DOTween.Kill(skipGroup);
            skipGroup.interactable = visible;
            skipGroup.blocksRaycasts = visible;
            float alpha = visible ? 1f : 0f;
            if (animate && skipFadeDuration > 0f)
            {
                skipGroup.DOFade(alpha, skipFadeDuration).SetUpdate(true).SetTarget(skipGroup);
            }
            else
            {
                skipGroup.alpha = alpha;
            }
        }

        void OnReviveClick(int index)
        {
            if (resolved) return;
            resolved = true;
            countdown.Stop();
            if (reviveButton != null) reviveButton.SetInteractable(false);

            TryShowReviveAd(() =>
            {
                IBoardSessionService target = session;
                Close();
                target?.Revive();
            });
        }

        /// <summary>
        /// Chỗ gắn quảng cáo thưởng sau này (Locator.Ads.Reward). Hiện chưa có Ads nên hồi sinh ngay.
        /// </summary>
        void TryShowReviveAd(Action onSuccess)
        {
            // TODO: Locator.Ads.Reward.Show(onSuccess, onHidden, placement) khi tích hợp Ads
            onSuccess?.Invoke();
        }

        void OnSkipClick(int index)
        {
            Decline();
        }

        /// <summary>
        /// Bỏ qua / hết giờ: đóng popup và kết thúc ván (BoardSession bắn OnGameOver → GameplayCanvas mở LoseCanvas)
        /// </summary>
        void Decline()
        {
            if (resolved) return;
            resolved = true;
            countdown.Stop();

            IBoardSessionService target = session;
            Close();
            target?.ConfirmGameOver();
        }

        void BindSession(IBoardSessionService value)
        {
            UnbindSession();
            session = value;
            if (session != null) session.OnGameRestarted += HandleGameRestarted;
        }

        void UnbindSession()
        {
            if (session != null) session.OnGameRestarted -= HandleGameRestarted;
            session = null;
        }

        /// <summary>
        /// Ván bị reset từ nơi khác (vd. mở Home) trong lúc popup đang mở: đóng mà không quyết định gì thêm
        /// </summary>
        void HandleGameRestarted()
        {
            if (resolved) return;
            resolved = true;
            countdown.Stop();
            Close();
        }

        void KillTweens()
        {
            if (ringFill != null) DOTween.Kill(ringFill);
            if (skipGroup != null) DOTween.Kill(skipGroup);
        }
    }
}
