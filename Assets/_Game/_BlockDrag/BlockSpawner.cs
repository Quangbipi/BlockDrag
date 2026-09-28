using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("Prefabs & References")]
    [Tooltip("Prefab ô vuông 1x1 chỉ gồm SpriteRenderer và BoxCollider2D")]
    public GameObject singleBlockPrefab;
    
    [Tooltip("Prefab rỗng có gắn sẵn script BlockShape")]
    public GameObject shapeContainerPrefab;

    [Header("Shape Data Pool")]
    public List<BlockShapeData> availableShapes;

    [Header("Spawn Slots")]
    [Tooltip("3 vị trí chờ dưới đáy màn hình")]
    public Transform[] spawnSlots;

    void Start()
    {
        SpawnNewHand();
    }

    public void SpawnNewHand()
    {
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
        if (availableShapes.Count == 0) return;

        // Chọn ngẫu nhiên 1 hình khối
        BlockShapeData randomData = availableShapes[Random.Range(0, availableShapes.Count)];

        // Tạo khung chứa hình khối tại vị trí Slot
        GameObject newShapeObj = Instantiate(shapeContainerPrefab, slot.position, Quaternion.identity, slot);
        BlockShape shapeComp = newShapeObj.GetComponent<BlockShape>();

        // Khởi tạo các ô gạch 1x1 bên trong
        shapeComp.Initialize(randomData, singleBlockPrefab, rotation: BlockRotation.Rot_90);
    }
}