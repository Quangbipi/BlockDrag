using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BlockShape))]
public class BlockDragHandler : MonoBehaviour
{
    [Header("Drag Offset & Scaling")]
    [Tooltip("Khoảng cách dịch chuyển khi nhấc khối lên (trục Y nhấc lên để ngón tay không che)")]
    [SerializeField] private Vector3 dragOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Tỷ lệ scale của khối khi đang kéo thả (chuẩn tỷ lệ Grid = 1.0)")]
    [SerializeField] private float dragScale = 1f;

    [Tooltip("Thời gian tween scale khi nhấc khối")]
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

    public bool IsDragging => isDragging;
    public BlockShape BlockShape => blockShape;
    public Vector3 DragOffset => dragOffset;
    public Vector3 OriginalPosition => originalPosition;
    public Vector3 OriginalScale => originalScale;

    private void Awake()
    {
        blockShape = GetComponent<BlockShape>();
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

    private void OnMouseDown()
    {
        if (isReturning) return;

        isDragging = true;
        originalPosition = transform.position;
        originalScale = transform.localScale;

        // Kill các tween cũ nếu có
        moveTween?.Kill();
        scaleTween?.Kill();

        // Nâng sorting order hiển thị
        if (blockShape != null)
        {
            blockShape.SetSortingOrderOffset(dragSortingOrderOffset);
        }

        // Tween scale lên kích thước chuẩn khi kéo
        scaleTween = transform.DOScale(Vector3.one * dragScale, pickUpDuration).SetEase(Ease.OutBack);

        // Cập nhật vị trí ngay lập tức theo con trỏ
        UpdateDragPosition();

        OnBeginDragEvent?.Invoke(this);
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        UpdateDragPosition();
        OnDraggingEvent?.Invoke(this, transform.position);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;
        OnEndDragEvent?.Invoke(this);

        // Mặc định: Trở về vị trí và scale ban đầu tại Slot
        ReturnToOrigin();
    }

    /// <summary>
    /// Tính toán và cập nhật toạ độ khối gạch theo con trỏ chuột/touch
    /// </summary>
    private void UpdateDragPosition()
    {
        Camera cam = GetCamera();
        if (cam == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        float planeDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);
        mouseScreen.z = planeDistance;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        transform.position = new Vector3(
            mouseWorld.x + dragOffset.x,
            mouseWorld.y + dragOffset.y,
            originalPosition.z
        );
    }

    /// <summary>
    /// Chạy hiệu ứng bay trở lại vị trí gốc
    /// </summary>
    public void ReturnToOrigin(Action onComplete = null)
    {
        isReturning = true;
        moveTween?.Kill();
        scaleTween?.Kill();

        scaleTween = transform.DOScale(originalScale, returnDuration).SetEase(returnEase);
        moveTween = transform.DOMove(originalPosition, returnDuration)
            .SetEase(returnEase)
            .OnComplete(() =>
            {
                isReturning = false;
                if (blockShape != null)
                {
                    blockShape.SetSortingOrderOffset(0);
                }
                onComplete?.Invoke();
            });
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        scaleTween?.Kill();
    }
}
