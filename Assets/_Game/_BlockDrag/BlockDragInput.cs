using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Đọc touch/chuột tập trung và điều khiển BlockDragHandler.
    /// Chỉ theo ngón tay đã nhấc khối, xử lý touch bị huỷ, set targetFrameRate cho mobile.
    /// </summary>
    public class BlockDragInput : MonoBehaviour
    {
        private const int MouseId = -1;

        [Tooltip("Camera dùng để quy đổi toạ độ màn hình (nếu để trống tự lấy Camera.main)")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("FPS mục tiêu (mobile mặc định 30). <= 0: giữ mặc định của Unity")]
        [SerializeField] private int targetFrameRate = 60;

        private readonly Collider2D[] hitBuffer = new Collider2D[8];
        private BlockDragHandler activeHandler;
        private int activeFingerId = MouseId;
        private bool hasActiveHandler;

        private void Awake()
        {
            if (targetFrameRate > 0)
            {
                Application.targetFrameRate = targetFrameRate;
            }
        }

        private void Update()
        {
            // Khối đang kéo bị destroy giữa chừng (restart...) → reset trạng thái
            if (hasActiveHandler && activeHandler == null)
            {
                ClearActive();
            }

            if (Input.touchCount > 0)
            {
                HandleTouches();
            }
            else
            {
                HandleMouse();
            }
        }

        private void HandleTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (!hasActiveHandler)
                {
                    if (touch.phase == TouchPhase.Began && TryBegin(touch.position))
                    {
                        activeFingerId = touch.fingerId;
                    }
                    continue;
                }

                if (touch.fingerId != activeFingerId) continue;

                switch (touch.phase)
                {
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        activeHandler.Drag(touch.position);
                        break;
                    case TouchPhase.Ended:
                        activeHandler.EndDrag();
                        ClearActive();
                        break;
                    case TouchPhase.Canceled:
                        activeHandler.CancelDrag();
                        ClearActive();
                        break;
                }
            }
        }

        private void HandleMouse()
        {
            // Ngón tay vừa nhấc ở frame trước nhưng không nhận được Ended → coi như huỷ
            if (hasActiveHandler && activeFingerId != MouseId)
            {
                activeHandler.CancelDrag();
                ClearActive();
                return;
            }

            if (!hasActiveHandler)
            {
                if (Input.GetMouseButtonDown(0) && TryBegin(Input.mousePosition))
                {
                    activeFingerId = MouseId;
                }
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                activeHandler.EndDrag();
                ClearActive();
            }
            else if (Input.GetMouseButton(0))
            {
                activeHandler.Drag(Input.mousePosition);
            }
        }

        private bool TryBegin(Vector2 screenPos)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return false;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            int hitCount = Physics2D.OverlapPointNonAlloc(world, hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                BlockDragHandler handler = hitBuffer[i].GetComponent<BlockDragHandler>();
                if (handler == null || !handler.CanBeginDrag) continue;

                handler.BeginDrag(screenPos);
                activeHandler = handler;
                hasActiveHandler = true;
                return true;
            }
            return false;
        }

        private void ClearActive()
        {
            activeHandler = null;
            hasActiveHandler = false;
            activeFingerId = MouseId;
        }

        private void OnDisable()
        {
            if (hasActiveHandler && activeHandler != null)
            {
                activeHandler.CancelDrag();
            }
            ClearActive();
        }
    }
}
