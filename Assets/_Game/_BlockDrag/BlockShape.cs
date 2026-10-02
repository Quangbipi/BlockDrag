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
    private readonly List<SpriteRenderer> cachedRenderers = new List<SpriteRenderer>();
    private readonly Dictionary<Vector2Int, GameObject> blockMap = new Dictionary<Vector2Int, GameObject>();

    public BoxCollider2D BoxCollider => boxCollider;
    public BlockRotation CurrentRotation { get; private set; } = BlockRotation.Rot_0;
    public bool IsDimmed { get; private set; } = false;
    public int RowsCount => shapeData != null ? shapeData.GetRotatedRowsCount(CurrentRotation) : 0;
    public int ColumnsCount => shapeData != null ? shapeData.GetRotatedColumnsCount(CurrentRotation) : 0;
    public IReadOnlyDictionary<Vector2Int, GameObject> BlockMap => blockMap;

    public bool HasBlockAt(int r, int c)
    {
        return shapeData != null && shapeData.HasBlockAtRotated(r, c, CurrentRotation);
    }

    public GameObject GetBlockAt(int r, int c)
    {
        blockMap.TryGetValue(new Vector2Int(r, c), out var block);
        return block;
    }

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    public void Initialize(BlockShapeData data, GameObject singleBlockPrefab, BlockRotation rotation = BlockRotation.Rot_0, float cellSize = 0f)
    {
        this.shapeData = data;
        this.CurrentRotation = rotation;
        this.IsDimmed = false;

        // Xóa block cũ nếu có
        foreach (var b in activeBlocks)
        {
            if (b != null)
            {
                Destroy(b);
            }
        }
        activeBlocks.Clear();
        cachedRenderers.Clear();
        blockMap.Clear();

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

                    // Đồng bộ scale với cellObj trong BlockGrid
                    ApplyBlockScale(block, blockSize.x);

                    // Đổi màu block con
                    var spriteRenderer = block.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = data.shapeColor;
                        cachedRenderers.Add(spriteRenderer);
                        baseSortingOrder = spriteRenderer.sortingOrder;
                    }

                    activeBlocks.Add(block);
                    blockMap[new Vector2Int(r, c)] = block;
                }
            }
        }

        UpdateColliderBounds(blockSize);
    }

    /// <summary>
    /// Căn chỉnh scale của ô con 1x1 theo kích thước cellSize chuẩn của BlockGrid
    /// </summary>
    private void ApplyBlockScale(GameObject blockObj, float targetSize)
    {
        BlockGrid.ApplyScale(blockObj, targetSize);
    }

    /// <summary>
    /// Cập nhật kích thước BoxCollider2D theo kích thước các khối gạch
    /// </summary>
    private void UpdateColliderBounds(Vector2 blockSize)
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

    /// <summary>
    /// Làm tối/mờ khối gạch khi không thể đặt vừa vào bất kỳ ô nào trên Grid (Block Blast style)
    /// Khi dimmed = true: giảm alpha/color và vô hiệu hóa BoxCollider2D để không kéo được.
    /// Khi dimmed = false: khôi phục màu gốc rực rỡ và kích hoạt lại BoxCollider2D.
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        IsDimmed = dimmed;

        if (boxCollider != null)
        {
            boxCollider.enabled = !dimmed;
        }

        Color originalColor = shapeData != null ? shapeData.shapeColor : Color.white;
        Color targetColor;

        if (dimmed)
        {
            // Màu tối mờ: hòa trộn với màu đen và giảm alpha xuống 0.45f
            targetColor = new Color(originalColor.r * 0.4f, originalColor.g * 0.4f, originalColor.b * 0.4f, 0.45f);
        }
        else
        {
            targetColor = originalColor;
            targetColor.a = 1f;
        }

        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].color = targetColor;
            }
        }
    }
}