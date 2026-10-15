using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Rung toàn màn hình bằng cách dịch view matrix (worldToCameraMatrix) của các camera (Base + Overlay trong URP stack).
    /// Không dùng projection vì canvas Screen Space - Camera của UICamera biến mất khi projection bị ghi đè.
    /// Transform camera không đổi nên toạ độ world của Grid và luật đặt khối không bị ảnh hưởng.
    /// </summary>
    [AddComponentMenu("Gameplay/Screen Shake")]
    public class ScreenShake : MonoBehaviour
    {
        [Tooltip("Các camera cùng rung (camera gameplay + UICamera + Top). Để trống sẽ dùng Camera.main")]
        [SerializeField] private Camera[] cameras;
        [Tooltip("Biên độ rung theo tỉ lệ chiều rộng màn hình (0.009 ≈ 6.5px trên màn 720px)")]
        [SerializeField] private float amplitude = 0.009f;
        [Tooltip("Thời gian rung (giây)")]
        [SerializeField] private float duration = 0.15f;
        [Tooltip("Số lần đổi hướng mỗi giây")]
        [SerializeField] private float frequency = 24f;
        [Tooltip("Dùng thời gian thực để rung luôn kết thúc kể cả khi Time.timeScale = 0")]
        [SerializeField] private bool useUnscaledTime = true;

        private float elapsed;
        private int seed;
        private bool isShaking;
        private bool warnedMissingCamera;

        public bool IsShaking => isShaking;
        public float Duration => duration;

        public Camera[] Cameras
        {
            get => cameras;
            set
            {
                StopAndReset();
                cameras = value;
            }
        }

        /// <summary>
        /// Bắt đầu rung (rung lại từ đầu nếu đang rung)
        /// </summary>
        public void Shake()
        {
            elapsed = 0f;
            seed++;
            isShaking = true;
            ApplyOffset();
        }

        /// <summary>
        /// Dừng rung và trả view matrix của mọi camera về mặc định
        /// </summary>
        public void StopAndReset()
        {
            isShaking = false;
            elapsed = 0f;
            ResetCameras();
        }

        /// <summary>
        /// Logic một frame (LateUpdate gọi; public để EditMode test chạy không cần Play mode)
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isShaking) return;

            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed >= duration)
            {
                StopAndReset();
                return;
            }

            ApplyOffset();
        }

        private void LateUpdate()
        {
            Tick(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        private void OnDisable()
        {
            StopAndReset();
        }

        private void OnDestroy()
        {
            StopAndReset();
        }

        private void ApplyOffset()
        {
            Vector2 offset = ShakeOffset.Evaluate(elapsed, duration, amplitude, frequency, seed);

            if (cameras != null && cameras.Length > 0)
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    ApplyOffset(cameras[i], offset);
                }
            }
            else
            {
                ApplyOffset(GetFallbackCamera(), offset);
            }
        }

        private static void ApplyOffset(Camera cam, Vector2 screenOffset)
        {
            if (cam == null) return;

            // Đổi tỉ lệ chiều rộng màn hình sang đơn vị world của camera (x và y cùng đơn vị => biên độ dọc = ngang theo pixel)
            float viewWidth = GetViewWidth(cam);
            cam.ResetWorldToCameraMatrix();
            cam.worldToCameraMatrix = Matrix4x4.Translate(new Vector3(screenOffset.x * viewWidth, screenOffset.y * viewWidth, 0f))
                                      * cam.worldToCameraMatrix;
        }

        /// <summary>
        /// Chiều rộng vùng nhìn (world) của camera: orthographic theo orthographicSize, perspective đo tại mặt phẳng z = 0
        /// </summary>
        public static float GetViewWidth(Camera cam)
        {
            if (cam.orthographic) return 2f * cam.orthographicSize * cam.aspect;

            float distance = Mathf.Max(cam.nearClipPlane, Mathf.Abs(cam.transform.position.z));
            return 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
        }

        private void ResetCameras()
        {
            if (cameras != null && cameras.Length > 0)
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] != null) cameras[i].ResetWorldToCameraMatrix();
                }
            }
            else
            {
                Camera main = Camera.main;
                if (main != null) main.ResetWorldToCameraMatrix();
            }
        }

        private Camera GetFallbackCamera()
        {
            Camera main = Camera.main;
            if (main == null && !warnedMissingCamera)
            {
                warnedMissingCamera = true;
                Debug.LogWarning("[ScreenShake] Không có camera nào để rung.", this);
            }
            return main;
        }
    }
}
