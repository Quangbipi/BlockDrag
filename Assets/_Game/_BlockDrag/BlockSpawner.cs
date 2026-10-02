using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("Prefabs & References")]
    [Tooltip("Prefab ô vuông 1x1 chỉ gồm SpriteRenderer và BoxCollider2D")]
    public GameObject singleBlockPrefab;

    [Tooltip("Prefab rỗng có gắn sẵn script BlockShape")]
    public GameObject shapeContainerPrefab;

    [Tooltip("Tham chiếu BlockGrid để lấy cellSize và spacing thực tế")]
    public BlockGrid blockGrid;

    [Header("Shape Data Pool")]
    public List<BlockShapeData> availableShapes;

    [Header("Spawn Position – Screen Percentages")]
    [Tooltip("Khoảng cách từ cạnh dưới màn hình, tính theo % chiều cao (0 = sát đáy, 1 = đỉnh)")]
    [SerializeField, Range(0f, 1f)]
    private float bottomOffsetPercent = 0.12f;

    [Tooltip("Lề trái / phải, tính theo % chiều rộng màn hình (0 = sát mép, 0.5 = giữa màn)")]
    [SerializeField, Range(0f, 0.5f)]
    private float sideMarginPercent = 0.15f;

    [Header("Spawn Scale")]
    [Tooltip("Tỷ lệ thu nhỏ của khối khi nằm tại slot so với kích thước grid (1 = bằng grid, 0.5 = nhỏ bằng nửa)")]
    [SerializeField, Range(0.1f, 1f)]
    private float spawnScaleRatio = 0.6f;

    [Header("Camera")]
    [Tooltip("Camera dùng để tính toạ độ (nếu để trống sẽ tự lấy Camera.main)")]
    [SerializeField] private Camera targetCamera;

    private const int SlotCount = 3;
    private Transform[] spawnSlots;

    private void Awake()
    {
        if (blockGrid == null)
        {
            blockGrid = FindObjectOfType<BlockGrid>();
        }
    }

    private void OnEnable()
    {
        if (blockGrid != null)
        {
            blockGrid.OnShapePlaced -= HandleShapePlaced;
            blockGrid.OnShapePlaced += HandleShapePlaced;
        }
    }

    private void OnDisable()
    {
        if (blockGrid != null)
        {
            blockGrid.OnShapePlaced -= HandleShapePlaced;
        }
    }

    void Start()
    {
        if (blockGrid == null)
        {
            blockGrid = FindObjectOfType<BlockGrid>();
        }

        if (blockGrid != null)
        {
            blockGrid.OnShapePlaced -= HandleShapePlaced;
            blockGrid.OnShapePlaced += HandleShapePlaced;
        }

        // Đảm bảo grid đã tính cellSize trước khi spawn
        if (blockGrid != null && blockGrid.CellSize <= 0f)
        {
            blockGrid.CalculateGridParameters();
        }

        CreateSpawnSlots();
        SpawnNewHand();
    }

    private void HandleShapePlaced()
    {
        if (IsHandEmpty())
        {
            SpawnNewHand();
        }
    }

    /// <summary>
    /// Kiểm tra xem cả 3 slot chờ đã đặt hết khối lên grid hay chưa
    /// </summary>
    public bool IsHandEmpty()
    {
        if (spawnSlots == null) return false;
        for (int i = 0; i < spawnSlots.Length; i++)
        {
            if (spawnSlots[i] != null && spawnSlots[i].childCount > 0)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Tạo 3 Transform con làm slot, vị trí tính từ viewport Camera.
    /// </summary>
    private void CreateSpawnSlots()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogError("[BlockSpawner] Không tìm thấy Camera!");
            return;
        }

        spawnSlots = new Transform[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            // Chia đều khoảng từ sideMarginPercent → (1 - sideMarginPercent)
            float viewportX = Mathf.Lerp(sideMarginPercent, 1f - sideMarginPercent, i / (float)(SlotCount - 1));
            float viewportY = bottomOffsetPercent;

            Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(viewportX, viewportY, cam.nearClipPlane));
            worldPos.z = 0f;

            GameObject slotObj = new GameObject($"SpawnSlot_{i}");
            slotObj.transform.SetParent(transform);
            slotObj.transform.position = worldPos;

            spawnSlots[i] = slotObj.transform;
        }
    }

    public void SpawnNewHand()
    {
        if (spawnSlots == null) return;

        for (int i = 0; i < spawnSlots.Length; i++)
        {
            if (spawnSlots[i].childCount == 0) // Slot đang trống
            {
                SpawnRandomShapeAt(spawnSlots[i]);
            }
        }
    }

    private void SpawnRandomShapeAt(Transform slot)
    {
        if (availableShapes == null || availableShapes.Count == 0) return;

        // Chọn ngẫu nhiên 1 hình khối
        BlockShapeData randomData = availableShapes[Random.Range(0, availableShapes.Count)];

        // Tạo khung chứa hình khối tại vị trí Slot
        GameObject newShapeObj = Instantiate(shapeContainerPrefab, slot.position, Quaternion.identity, slot);
        BlockShape shapeComp = newShapeObj.GetComponent<BlockShape>();

        // Lấy cellSize thật từ grid (giống Block Blast: luôn tạo đúng kích thước grid)
        float gridCellSize = blockGrid != null ? blockGrid.CellSize : 0f;

        // Đồng bộ spacing của BlockShape với grid để khi scale lên khớp hoàn hảo
        if (shapeComp != null && blockGrid != null)
        {
            shapeComp.spacing = blockGrid.Spacing;
        }

        // Khởi tạo các ô gạch 1x1 với kích thước BẰNG grid (cellSize thật)
        if (shapeComp != null)
        {
            shapeComp.Initialize(randomData, singleBlockPrefab, rotation: BlockRotation.Rot_90, cellSize: gridCellSize);
        }

        // Thu nhỏ container bằng localScale → hiển thị nhỏ tại slot chờ
        // Khi drag sẽ DOScale về Vector3.one → khớp hoàn hảo với grid
        newShapeObj.transform.localScale = Vector3.one * spawnScaleRatio;

        // Ghi nhận toạ độ gốc và tỷ lệ gốc (scale nhỏ) cho BlockDragHandler
        // dragScale = 1 vì block đã được tạo đúng kích thước grid,
        // chỉ cần scale container về Vector3.one là khớp
        BlockDragHandler dragHandler = newShapeObj.GetComponent<BlockDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.BlockGrid = blockGrid;
            dragHandler.SetOrigin(newShapeObj.transform.position, newShapeObj.transform.localScale);
            dragHandler.DragScale = 1f;
        }
    }
}