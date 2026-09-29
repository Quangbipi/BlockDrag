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

    public BoxCollider2D BoxCollider => boxCollider;

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
            if (b != null)
            {
                Destroy(b);
            }
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

        UpdateColliderBounds(blockSize);
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
}