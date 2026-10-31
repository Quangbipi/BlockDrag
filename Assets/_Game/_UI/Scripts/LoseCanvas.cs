using Gameplay.BlockDrag;
using UnityEngine;

namespace UI
{
    using Quang.Base;
    using Quang.UI;
    using System.Globalization;
    using TMPro;
    using DG.Tweening;

    public class LoseCanvas : UISCanvas
    {
        [SerializeField]
        UIButton tryAgainButton;
        [SerializeField]
        UIButton goHomeButton;
        [SerializeField]
        TMP_Text scoreTxt;
        [SerializeField]
        TMP_Text highScoreTxt;
        [SerializeField]
        float scoreCountDuration = 0.5f;
        Tween scoreTween;

        private void Awake()
        {
            tryAgainButton._OnClick += OnTryAgain;
            goHomeButton._OnClick += OnGoHome;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillScoreTween();
            tryAgainButton._OnClick -= OnTryAgain;
            goHomeButton._OnClick -= OnGoHome;
        }
        public override void Open(object param)
        {
            base.Open(param);
            //Locator.Audio.PlaySfx(SFX_TYPE.LOSE);
            ShowScore();
        }
        protected override void OnClose()
        {
            base.OnClose();
            KillScoreTween();
        }
        /// <summary>
        /// Hiện HighScore ngay và chạy điểm ván vừa chơi tăng dần từ 0 (định dạng 1,240)
        /// </summary>
        void ShowScore()
        {
            KillScoreTween();
            IScoreService score = Locator.Score;
            int finalScore = score != null ? score.CurrentScore : 0;
            // Max để đúng cả khi ScoreManager chưa kịp SaveHighScore lúc Game Over
            int highScore = score != null ? Mathf.Max(finalScore, score.HighScore) : 0;

            highScoreTxt.text = highScore.ToString("N0", CultureInfo.InvariantCulture);
            scoreTxt.text = "0";
            if (finalScore <= 0) return;

            int shown = 0;
            scoreTween = DOTween.To(() => shown, v =>
                {
                    shown = v;
                    scoreTxt.text = v.ToString("N0", CultureInfo.InvariantCulture);
                }, finalScore, scoreCountDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(this);
        }
        void KillScoreTween()
        {
            scoreTween?.Kill();
            scoreTween = null;
        }
        /// <summary>
        /// Chơi lại ván mới (giống nút Replay của GameOverUI cũ)
        /// </summary>
        protected void OnTryAgain(int index)
        {
            Close();
            UIManager.Ins.CloseUI<TopCanvas>();

            Locator.BoardSession?.RestartGame();
        }

        protected void OnGoHome(int index)
        {
            Close();
            Locator.UI.OpenUI<HomeCanvas>();
        }
    }
}
