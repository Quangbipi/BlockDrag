using UnityEngine;

namespace UI
{
    /// <summary>
    /// Thu nhỏ (không phóng to) một khối UI có kích thước thiết kế cố định cho vừa RectTransform cha
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScaleToFitParent : MonoBehaviour
    {
        [Tooltip("Kích thước thiết kế (canvas units) của khối cần vừa trong cha")]
        [SerializeField]
        Vector2 designSize = new Vector2(984f, 1290f);

        RectTransform rectTransform;
        RectTransform parentRect;
        Vector2 lastParentSize;

        void OnEnable()
        {
            Apply();
        }

        void LateUpdate()
        {
            // Đổi kích thước cha không phải lúc nào cũng báo xuống con
            if (parentRect != null && parentRect.rect.size != lastParentSize)
            {
                Apply();
            }
        }

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
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
            parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            lastParentSize = parentRect.rect.size;
            rectTransform.sizeDelta = designSize;
            rectTransform.localScale = Vector3.one * ComputeScale(lastParentSize, designSize);
        }

        /// <summary>
        /// Scale = min(1, available / design) theo cả 2 chiều; trả 1 khi kích thước chưa hợp lệ (layout chưa sẵn sàng)
        /// </summary>
        public static float ComputeScale(Vector2 available, Vector2 design)
        {
            if (design.x <= 0f || design.y <= 0f || available.x <= 0f || available.y <= 0f)
            {
                return 1f;
            }
            return Mathf.Min(1f, available.x / design.x, available.y / design.y);
        }
    }
}
