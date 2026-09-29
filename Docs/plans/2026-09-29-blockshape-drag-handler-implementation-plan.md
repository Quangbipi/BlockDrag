# Kế Hoạch Triển Khai: Hệ Thống Kéo Thả BlockShape (BlockShape Drag Handler)

> **Dành cho agent / kỹ sư thực thi:**  
> **Tài liệu thiết kế:** [`Docs/designs/2026-09-29-blockshape-drag-handler-design.md`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Docs/designs/2026-09-29-blockshape-drag-handler-design.md)  
> **Quy tắc kiến trúc:** [`.agents/rules/architecture.md`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/.agents/rules/architecture.md) & [`.agents/rules/task_planning.md`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/.agents/rules/task_planning.md)

**Mục tiêu:** Xây dựng component độc lập `BlockDragHandler.cs` cho `BlockShape`, hỗ trợ kéo thả chuột/cảm ứng với độ lệch hiển thị ngón tay (Y offset), phóng to khi nhấc lên và tween mượt mà quay về slot khi thả tay.

**Kiến trúc:** Tách biệt trách nhiệm (SRP): `BlockShape` quản lý cấu trúc khối gạch, collider và sorting order; `BlockDragHandler` quản lý tương tác người dùng, tính toán toạ độ và chạy animation qua DOTween; phát sinh C# Events phục vụ mở rộng snap vào `BlockGrid`.

**Tech Stack:** Unity 2022.3.62f2, C#, DOTween (`DG.Tweening`), 2D Physics (`BoxCollider2D`).

---

### Task 1: Cấu hình Assembly Reference cho `BlockDrag.asmdef`

**Files:**
- Sửa đổi: [`Assets/_Game/_BlockDrag/BlockDrag.asmdef`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/BlockDrag.asmdef)

- [ ] **Bước 1: Cập nhật file `BlockDrag.asmdef`**
Thêm GUID của `DOTween.Modules` (`bd7cbe032296944a69986a3acfb90044`) vào danh sách `"references"`.
```json
{
    "name": "BlockDrag",
    "rootNamespace": "Gameplay.BlockDrag",
    "references": [
        "GUID:d8f99e86a031141438d186833c629fd7",
        "GUID:a5893f15044049c40822e21fa22b439c",
        "GUID:bd7cbe032296944a69986a3acfb90044"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Bước 2: Xác minh biên dịch**
Kiểm tra project không báo lỗi thiếu assembly reference của DOTween.

---

### Task 2: Nâng cấp `BlockShape.cs` hỗ trợ Collider tự động và Quản lý Sorting Order

**Files:**
- Sửa đổi: [`Assets/_Game/_BlockDrag/BlockShape.cs`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/BlockShape.cs)

- [ ] **Bước 1: Cập nhật `BlockShape.cs`**
Bổ sung:
1. Thuộc tính và phương thức tính toán `Bounds` tổng thể của các ô gạch 1x1 sau khi instantiate.
2. Tự động thêm hoặc cập nhật `BoxCollider2D` trên GameObject của `BlockShape` khớp với kích thước tổng của hình khối.
3. Lưu trữ `baseSortingOrder` và cung cấp phương thức `SetSortingOrderOffset(int offset)` để tăng/giảm sortingOrder đồng bộ cho toàn bộ Sprite con.

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BlockShape : MonoBehaviour
{
    public BlockShapeData shapeData;

    // Lưu danh sách các ô 1x1 được tạo
    [HideInInspector]
    public List<GameObject> activeBlocks = new List<GameObject>();
    [Tooltip("Khoảng cách khe hở giữa các ô gạch 1x1")]
    public float spacing = 0.1f;

    private BoxCollider2D boxCollider;
    private int baseSortingOrder = 0;
    private List<SpriteRenderer> cachedRenderers = new List<SpriteRenderer>();

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    public void Initialize(BlockShapeData data, GameObject singleBlockPrefab, BlockRotation rotation = BlockRotation.Rot_0, float cellSize = 0f)
    {
        this.shapeData = data;

        // Xóa block cũ nếu có
        foreach (var b in activeBlocks)
        {
            Destroy(b);
        }
        activeBlocks.Clear();
        cachedRenderers.Clear();

        // Dùng số hàng và số cột đã tính theo góc xoay
        int rows = data.GetRotatedRowsCount(rotation);
        int cols = data.GetRotatedColumnsCount(rotation);

        // Tự động lấy kích thước thực tế của Sprite nếu không truyền cellSize
        Vector2 blockSize = new Vector2(cellSize, cellSize);
        if (cellSize <= 0f && singleBlockPrefab != null)
        {
            var prefabSpriteRenderer = singleBlockPrefab.GetComponent<SpriteRenderer>();
            if (prefabSpriteRenderer != null && prefabSpriteRenderer.sprite != null)
            {
                Vector2 spriteSize = prefabSpriteRenderer.sprite.bounds.size;
                Vector3 prefabScale = singleBlockPrefab.transform.localScale;
                blockSize = new Vector2(
                    spriteSize.x * Mathf.Abs(prefabScale.x),
                    spriteSize.y * Mathf.Abs(prefabScale.y)
                );
            }
            else
            {
                blockSize = Vector2.one;
            }
        }

        Vector2 step = new Vector2(blockSize.x + spacing, blockSize.y + spacing);

        // Tính offset để căn giữa hình khối theo pivot của GameObject cha
        Vector2 offset = new Vector2((cols - 1) * 0.5f, (rows - 1) * 0.5f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (data.HasBlockAtRotated(r, c, rotation))
                {
                    // Tọa độ cục bộ: lộn ngược trục Y để row 0 nằm ở trên cùng
                    Vector3 localPos = new Vector3((c - offset.x) * step.x, ((rows - 1 - r) - offset.y) * step.y, 0f);
                    GameObject block = Instantiate(singleBlockPrefab, transform);
                    block.transform.localPosition = localPos;

                    // Đổi màu block con
                    var spriteRenderer = block.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = data.shapeColor;
                        cachedRenderers.Add(spriteRenderer);
                        baseSortingOrder = spriteRenderer.sortingOrder;
                    }

                    activeBlocks.Add(block);
                }
            }
        }

        UpdateColliderBounds(cols, rows, step, blockSize);
    }

    /// <summary>
    /// Cập nhật kích thước BoxCollider2D theo kích thước các khối gạch
    /// </summary>
    private void UpdateColliderBounds(int cols, int rows, Vector2 step, Vector2 blockSize)
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        if (activeBlocks.Count == 0)
        {
            boxCollider.size = Vector2.zero;
            boxCollider.offset = Vector2.zero;
            return;
        }

        // Tính bao viền dựa trên danh sách activeBlocks
        Bounds bounds = new Bounds(activeBlocks[0].transform.localPosition, Vector3.zero);
        for (int i = 1; i < activeBlocks.Count; i++)
        {
            bounds.Encapsulate(activeBlocks[i].transform.localPosition);
        }

        // Kích thước collider bao trọn cả ô ngoài cùng
        boxCollider.size = new Vector2(bounds.size.x + blockSize.x, bounds.size.y + blockSize.y);
        boxCollider.offset = bounds.center;
    }

    /// <summary>
    /// Tăng giảm sortingOrder hiển thị của toàn bộ các ô con
    /// </summary>
    public void SetSortingOrderOffset(int offset)
    {
        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].sortingOrder = baseSortingOrder + offset;
            }
        }
    }
}
```

- [ ] **Bước 2: Xác minh biên dịch**
Đảm bảo `BlockShape.cs` biên dịch không lỗi và các method public sẵn sàng cho `BlockDragHandler`.

---

### Task 3: Tạo mới script `BlockDragHandler.cs`

**Files:**
- Tạo mới: [`Assets/_Game/_BlockDrag/BlockDragHandler.cs`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/BlockDragHandler.cs)

- [ ] **Bước 1: Viết mã nguồn `BlockDragHandler.cs`**
Tạo file với đầy đủ logic:
1. Quản lý trạng thái kéo thả (`isDragging`, `isReturning`).
2. Nhấc lên (Y offset), scale lên 1.0, tăng sorting order.
3. Di chuyển theo ngón tay / chuột dựa trên `Camera.ScreenToWorldPoint`.
4. Khi thả tay: Tween về vị trí và tỷ lệ ban đầu bằng `transform.DOMove` và `transform.DOScale`.
5. Bắn các event `OnBeginDragEvent`, `OnDraggingEvent`, `OnEndDragEvent`.

```csharp
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
        mouseScreen.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
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
```

- [ ] **Bước 2: Xác minh biên dịch file mới**
Kiểm tra code `BlockDragHandler.cs` không có lỗi cú pháp hoặc cảnh báo lặp biến.

---

### Task 4: Cập nhật Prefab `BlockShape.prefab` và `BlockSpawner.cs`

**Files:**
- Sửa đổi: [`Assets/_Game/_BlockDrag/Prefabs/BlockShape.prefab`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/Prefabs/BlockShape.prefab)
- Sửa đổi (nếu cần): [`Assets/_Game/_BlockDrag/BlockSpawner.cs`](file:///Users/minhquang/Documents/Unity/ITCProjects/BlockDrag/Assets/_Game/_BlockDrag/BlockSpawner.cs)

- [ ] **Bước 1: Gắn component `BlockDragHandler` và `BoxCollider2D` lên `BlockShape.prefab`**
Đảm bảo khi `BlockSpawner` instantiate `shapeContainerPrefab`, GameObject sinh ra đã có sẵn `BlockShape`, `BlockDragHandler`, và `BoxCollider2D`.

- [ ] **Bước 2: Cập nhật `BlockSpawner.cs` (nếu cần)**
Khi instantiate shape tại slot, gọi `dragHandler.SetOrigin(slot.position, newShapeObj.transform.localScale)` để đảm bảo vị trí gốc luôn chính xác theo slot.

---

### Task 5: Kiểm tra và Nghiệm thu (Verification & Testing)

- [ ] **Bước 1: Kiểm tra Biên Dịch**
Xác nhận toàn bộ dự án biên dịch thành công 100% trong Unity không có compile errors.

- [ ] **Bước 2: Kiểm thử tương tác PlayMode**
1. Nhấn Play trong Unity Editor tại Scene chứa `BlockGrid` & `BlockSpawner`.
2. Click và giữ chuột vào khối gạch tại một Slot bất kỳ: Khối gạch phóng to mượt mà và dịch chuyển tâm lên trên vị trí con trỏ theo `dragOffset.y = 1.5`.
3. Kéo di chuyển chuột quanh màn hình: Khối gạch bám mượt mà theo con trỏ chuột.
4. Nhả chuột bất kỳ đâu: Khối gạch bay mượt mà (`DOMove`) về lại vị trí Slot ban đầu, scale co lại đúng bằng scale gốc ở Slot, sorting order được trả về lớp bình thường.
