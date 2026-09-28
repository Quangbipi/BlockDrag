using System.Collections.Generic;
using UnityEngine;

public class BlockShape : MonoBehaviour
{
    public BlockShapeData shapeData;

    // Lưu danh sách các ô 1x1 được tạo
    [HideInInspector]
    public List<GameObject> activeBlocks = new List<GameObject>();
    [Tooltip("Khoảng cách khe hở giữa các ô gạch 1x1")]
    public float spacing = 0.1f;
    public void Initialize(BlockShapeData data, GameObject singleBlockPrefab, BlockRotation rotation = BlockRotation.Rot_0, float cellSize = 0f)
    {
        this.shapeData = data;

        // Xóa block cũ nếu có
        foreach (var b in activeBlocks)
        {
            Destroy(b);
        }
        activeBlocks.Clear();

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
                    }

                    activeBlocks.Add(block);
                }
            }
        }
    }
}