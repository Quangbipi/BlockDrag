using UnityEngine;

namespace UI
{
    using Base.UI;
    using Spine.Unity;
    using TMPro;
    using UnityEngine.UI;
    using Utilities;

    public class InitLoadingCanvas : UISCanvas
    {
        [SerializeField]
        TMP_Text loadingText;
        [SerializeField]
        Slider loadingBar;
        [SerializeField]
        SkeletonGraphic loadingSpine;

        public Slider LoadingBar => loadingBar;
        void Awake()
        {
            loadingSpine.AnimationState.SetAnimation(0, "appear", false);
            loadingSpine.AnimationState.AddAnimation(0, "idle", true, 0);
        }
        public void SetPercentage(float value)
        {
            loadingBar.value = value;
            loadingText.text = $"{(int)(value * 100)}%";
        }
    }
}
