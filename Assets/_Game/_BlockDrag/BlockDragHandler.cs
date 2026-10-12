using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    [RequireComponent(typeof(BlockShape))]
    public class BlockDragHandler : MonoBehaviour
    {
        [Header("Drag Lift & Scaling")]
        [Tooltip("Khoảng nâng khối lên trên ngón tay khi kéo, tính theo số ô grid (để ngón tay không che)")]
        [SerializeField] private float dragLiftCells = 3f;

        [Tooltip("Tỷ lệ scale của khối khi đang kéo thả (chuẩn tỷ lệ Grid = 1.0)")]
        [SerializeField] private float dragScale = 1f;

        /// <summary>
        /// Cho phép gán dragScale từ bên ngoài (ví dụ BlockSpawner tính 1/spawnScaleRatio)
        /// </summary>
        public float DragScale
        {
            get => dragScale;
            set => dragScale = value;
        }

        [Tooltip("Thời gian tween scale và khoảng nâng khi nhấc khối")]
        [SerializeField] private float pickUpDuration = 0.15f;

        [Header("Return Animation")]
        [Tooltip("Thời gian bay về lại Slot khi thả tay ra ngoài")]
        [SerializeField] private float returnDuration = 0.2f;

        [Tooltip("Kiểu ease khi bay về Slot")]
        [SerializeField] private Ease returnEase = Ease.OutQuad;

        [Header("Sorting Layer")]
        [Tooltip("Độ tăng SortingOrder khi đang kéo để hiển thị trên cùng")]
        [SerializeField] private int dragSortingOrderOffset = 10;

        [Header("Camera Reference")]
        [Tooltip("Camera chính dùng để quy đổi toạ độ màn hình (nếu để trống tự lấy Camera.main)")]
        [SerializeField] private Camera targetCamera;

        [Header("Grid Reference")]
        [Tooltip("Tham chiếu BlockGrid (nếu để trống tự tìm FindObjectOfType)")]
        [SerializeField] private BlockGrid blockGrid;

        // Events phục vụ mở rộng tích hợp BlockGrid
        public event Action<BlockDragHandler> OnBeginDragEvent;
        public event Action<BlockDragHandler, Vector3> OnDraggingEvent;
        public event Action<BlockDragHandler> OnEndDragEvent;

        private BlockShape blockShape;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isDragging = false;
        private bool isReturning = false;
        private Tween moveTween;
        private Tween scaleTween;
        private Tween liftTween;
        private float currentLift;
        private Vector2 lastScreenPos;

        public bool IsDragging => isDragging;
        public BlockShape BlockShape => blockShape;
        public BlockGrid BlockGrid
        {
            get => blockGrid;
            set => blockGrid = value;
        }
        public bool CanBeginDrag => !isDragging && blockShape != null && !blockShape.IsDimmed;
        public float LiftWorld => dragLiftCells * (blockGrid != null && blockGrid.CellSize > 0f ? blockGrid.CellSize : 1f);
        public Vector3 DragOffset => new Vector3(0f, LiftWorld, 0f);
        public Vector3 OriginalPosition => originalPosition;
        public Vector3 OriginalScale => originalScale;

        private void Awake()
        {
            blockShape = GetComponent<BlockShape>();
            if (blockGrid == null)
            {
                blockGrid = FindObjectOfType<BlockGrid>();
            }
        }

        private void Start()
        {
            originalPosition = transform.position;
            originalScale = transform.localScale;

        }

        /// <summary>
        /// Ghi nhận lại vị trí gốc khi sinh ra hoặc khi đặt vào Slot mới
        /// </summary>
        public void SetOrigin(Vector3 position, Vector3 scale)
        {
            originalPosition = position;
            originalScale = scale;
        }

        private Camera GetCamera()
        {
            return targetCamera != null ? targetCamera : Camera.main;
        }

        /// <summary>
        /// Nhấc khối lên theo ngón tay. Được phép cả khi khối đang bay về slot.
        /// </summary>
        public void BeginDrag(Vector2 screenPos)
        {
            if (!CanBeginDrag) return;

            isDragging = true;
            isReturning = false;
            lastScreenPos = screenPos;

            // Kill các tween cũ (kể cả tween bay về slot)
            KillTweens();

            // Đang kéo không cần nhận chạm nữa
            blockShape.SetInputEnabled(false);

            // Nâng sorting order hiển thị
            blockShape.SetSortingOrderOffset(dragSortingOrderOffset);

            // Tween scale lên kích thước chuẩn khi kéo (không overshoot)
            scaleTween = transform.DOScale(Vector3.one * dragScale, pickUpDuration).SetEase(Ease.OutQuad);

            // Nâng dần khối lên trên ngón tay thay vì nhảy tức thời
            currentLift = 0f;
            liftTween = DOTween.To(() => currentLift, v => currentLift = v, LiftWorld, pickUpDuration)
                .SetEase(Ease.OutQuad);

            UpdateDragPosition();

            if (blockGrid != null)
            {
                blockGrid.UpdateDragIndicator(blockShape, transform.position);
            }

            OnBeginDragEvent?.Invoke(this);
        }

        /// <summary>
        /// Cập nhật vị trí khi kéo. Gọi mỗi frame (kể cả khi ngón tay đứng yên) để khoảng nâng chạy mượt.
        /// </summary>
        public void Drag(Vector2 screenPos)
        {
            if (!isDragging) return;

            lastScreenPos = screenPos;
            UpdateDragPosition();

            if (blockGrid != null)
            {
                blockGrid.UpdateDragIndicator(blockShape, transform.position);
            }

            OnDraggingEvent?.Invoke(this, transform.position);
        }

        /// <summary>
        /// Thả tay: thử đặt lên grid, không được thì bay về slot.
        /// </summary>
        public void EndDrag()
        {
            if (!isDragging) return;

            isDragging = false;
            liftTween?.Kill();
            OnEndDragEvent?.Invoke(this);

            bool placed = false;
            if (blockGrid != null)
            {
                placed = blockGrid.TryPlaceShape(blockShape, transform.position);
            }

            // Nếu không xếp được lên grid, bay về vị trí gốc tại slot
            if (!placed)
            {
                ReturnToOrigin();
            }
        }

        /// <summary>
        /// Touch bị huỷ (cuộc gọi, notification...): không đặt khối, bay về slot.
        /// </summary>
        public void CancelDrag()
        {
            if (!isDragging) return;

            isDragging = false;
            liftTween?.Kill();

            if (blockGrid != null)
            {
                blockGrid.HideIndicator();
            }

            OnEndDragEvent?.Invoke(this);
            ReturnToOrigin();
        }

        /// <summary>
        /// Tính toán và cập nhật toạ độ khối gạch theo ngón tay (bám 1:1, không làm trễ)
        /// </summary>
        private void UpdateDragPosition()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            float planeDistance = Mathf.Abs(cam.transform.position.z - originalPosition.z);
            Vector3 fingerWorld = cam.ScreenToWorldPoint(new Vector3(lastScreenPos.x, lastScreenPos.y, planeDistance));

            transform.position = new Vector3(
                fingerWorld.x,
                fingerWorld.y + currentLift,
                originalPosition.z
            );
        }

        /// <summary>
        /// Chạy hiệu ứng bay trở lại vị trí gốc
        /// </summary>
        public void ReturnToOrigin(Action onComplete = null)
        {
            isReturning = true;
            KillTweens();

            scaleTween = transform.DOScale(originalScale, returnDuration).SetEase(returnEase);
            moveTween = transform.DOMove(originalPosition, returnDuration)
                .SetEase(returnEase)
                .OnComplete(() =>
                {
                    isReturning = false;
                    if (blockShape != null)
                    {
                        blockShape.SetSortingOrderOffset(0);
                        blockShape.SetInputEnabled(true);
                    }
                    onComplete?.Invoke();
                });
        }

        private void KillTweens()
        {
            moveTween?.Kill();
            scaleTween?.Kill();
            liftTween?.Kill();
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}
