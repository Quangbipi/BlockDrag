using UnityEngine;

namespace UI
{
    /// <summary>
    /// Fits a RectTransform to Screen.safeArea (notch, home indicator) by driving its anchors.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rectTransform;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;

        void OnEnable()
        {
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                Apply();
            }
        }

        void Apply()
        {
            if (rectTransform == null)
            {
                rectTransform = (RectTransform)transform;
            }

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            ComputeAnchors(lastSafeArea, lastScreenSize, out Vector2 min, out Vector2 max);
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Converts a safe area in screen pixels to normalized anchors. Returns full-screen anchors for a zero-size screen.
        /// </summary>
        public static void ComputeAnchors(Rect safeArea, Vector2 screenSize, out Vector2 min, out Vector2 max)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }

            min = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            max = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
        }
    }
}
