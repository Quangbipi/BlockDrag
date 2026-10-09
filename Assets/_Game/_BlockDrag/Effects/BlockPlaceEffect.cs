using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Hiệu ứng particle vuông nhiều màu tỏa ra quanh mép khối khi đặt BlockShape xuống Grid (giống Block Blast)
    /// </summary>
    [AddComponentMenu("Gameplay/Block Place Effect")]
    public class BlockPlaceEffect : MonoBehaviour
    {
        /// <summary>
        /// Một cạnh biên của khối: ô chứa cạnh và hướng ra ngoài theo toạ độ grid (x = row, y = col)
        /// </summary>
        public struct PerimeterEdge
        {
            public Vector2Int Cell;
            public Vector2Int GridDirection;

            /// <summary>
            /// Pháp tuyến ngoài trong không gian cục bộ của Grid (hàng tăng xuống dưới => row - 1 là +Y)
            /// </summary>
            public Vector2 LocalNormal => new Vector2(GridDirection.y, -GridDirection.x);
        }

        private static readonly Vector2Int[] GridDirections =
        {
            new Vector2Int(-1, 0), // Lên
            new Vector2Int(1, 0),  // Xuống
            new Vector2Int(0, -1), // Trái
            new Vector2Int(0, 1)   // Phải
        };

        [Header("References")]
        [Tooltip("Tham chiếu BlockGrid (tự tìm nếu để trống)")]
        [SerializeField] private BlockGrid blockGrid;
        [Tooltip("Material tuỳ chọn cho hạt (để trống sẽ dùng Sprites/Default => hạt vuông trơn)")]
        [SerializeField] private Material materialOverride;

        [Header("Timing")]
        [Tooltip("Độ trễ sau khi đặt khối mới phát hạt (giây). Đặt 0 để phát ngay")]
        [SerializeField] private float playDelay = 0.2f;
        [Tooltip("Thời gian sống của hạt (giây)")]
        [SerializeField] private Vector2 lifetimeRange = new Vector2(0.3f, 0.45f);

        [Header("Amount")]
        [Tooltip("Số hạt trên mỗi cạnh biên của khối")]
        [SerializeField] private float particlesPerEdge = 1.3f;
        [SerializeField] private int minParticles = 6;
        [SerializeField] private int maxParticles = 36;

        [Header("Motion (đơn vị: cellSize)")]
        [Tooltip("Kích thước hạt so với cạnh ô")]
        [SerializeField] private Vector2 sizeRange = new Vector2(0.1f, 0.18f);
        [Tooltip("Quãng bay của hạt so với cạnh ô")]
        [SerializeField] private Vector2 travelRange = new Vector2(0.25f, 1.3f);
        [Tooltip("Độ lệch hướng bay so với pháp tuyến cạnh (độ)")]
        [Range(0f, 90f)]
        [SerializeField] private float spreadAngle = 40f;
        [Tooltip("Lực cản: càng lớn hạt càng bung nhanh rồi dừng gấp")]
        [SerializeField] private float drag = 22f;

        [Header("Rendering")]
        [Tooltip("Sorting Order của hạt (ô nền = 0, block đã đặt = 2)")]
        [SerializeField] private int sortingOrder = 1;
        [SerializeField] private Color[] palette =
        {
            new Color32(93, 214, 92, 255),   // Xanh lá
            new Color32(235, 77, 61, 255),   // Đỏ
            new Color32(247, 151, 59, 255),  // Cam
            new Color32(255, 214, 74, 255),  // Vàng
            new Color32(255, 240, 170, 255), // Vàng nhạt
            new Color32(64, 208, 232, 255),  // Cyan
            new Color32(176, 108, 230, 255)  // Tím
        };

        private ParticleSystem particles;
        private Material runtimeMaterial;
        private readonly List<PerimeterEdge> edgeBuffer = new List<PerimeterEdge>();

        public ParticleSystem Particles => particles;
        public BlockGrid Grid
        {
            get => blockGrid;
            set
            {
                UnsubscribeEvents();
                blockGrid = value;
                if (isActiveAndEnabled) SubscribeEvents();
            }
        }

        private void Awake()
        {
            if (blockGrid == null)
            {
                blockGrid = FindObjectOfType<BlockGrid>();
            }

            BuildParticleSystem();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            DOTween.Kill(this);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);

            if (particles != null)
            {
                Destroy(particles.gameObject);
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void SubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnShapePlacedCells -= HandleShapePlacedCells;
                blockGrid.OnShapePlacedCells += HandleShapePlacedCells;
            }
        }

        private void UnsubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnShapePlacedCells -= HandleShapePlacedCells;
            }
        }

        private void HandleShapePlacedCells(IReadOnlyList<Vector2Int> cells)
        {
            if (playDelay > 0f)
            {
                DOVirtual.DelayedCall(playDelay, () => Play(cells)).SetTarget(this);
            }
            else
            {
                Play(cells);
            }
        }

        /// <summary>
        /// Phát hạt tỏa ra quanh chu vi các ô (row, col) trên Grid
        /// </summary>
        public void Play(IReadOnlyList<Vector2Int> cells)
        {
            if (particles == null || blockGrid == null) return;

            CollectPerimeterEdges(cells, edgeBuffer);
            if (edgeBuffer.Count == 0) return;

            int count = CalculateParticleCount(edgeBuffer.Count, particlesPerEdge, minParticles, maxParticles);
            Transform gridTransform = blockGrid.transform;
            float cellSize = blockGrid.CellSize;
            float halfStep = (cellSize + blockGrid.Spacing) * 0.5f;
            float worldScale = Mathf.Abs(gridTransform.lossyScale.x);

            var emitParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                PerimeterEdge edge = edgeBuffer[Random.Range(0, edgeBuffer.Count)];
                Vector2 normal = edge.LocalNormal;
                Vector2 tangent = new Vector2(-normal.y, normal.x);

                Vector2 localOffset = normal * halfStep + tangent * (Random.Range(-0.5f, 0.5f) * cellSize);
                Vector2 localDir = Quaternion.Euler(0f, 0f, Random.Range(-spreadAngle, spreadAngle)) * normal;

                // Với lực cản tuyến tính k, hạt dừng sau quãng v0 / k => v0 = quãng bay × k
                float travel = Random.Range(travelRange.x, travelRange.y) * cellSize;

                emitParams.position = blockGrid.GetCellWorldPosition(edge.Cell.x, edge.Cell.y)
                                      + gridTransform.TransformVector(localOffset);
                emitParams.velocity = gridTransform.TransformVector(localDir * (travel * drag));
                emitParams.startSize = Random.Range(sizeRange.x, sizeRange.y) * cellSize * worldScale;
                emitParams.rotation = Random.Range(0f, 360f);
                emitParams.startLifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
                emitParams.startColor = palette != null && palette.Length > 0
                    ? palette[Random.Range(0, palette.Length)]
                    : Color.white;

                particles.Emit(emitParams, 1);
            }
        }

        /// <summary>
        /// Gom các cạnh biên của tập ô: cạnh nào có ô kề không thuộc tập là cạnh biên
        /// </summary>
        public static List<PerimeterEdge> CollectPerimeterEdges(IReadOnlyList<Vector2Int> cells)
        {
            var result = new List<PerimeterEdge>();
            CollectPerimeterEdges(cells, result);
            return result;
        }

        public static void CollectPerimeterEdges(IReadOnlyList<Vector2Int> cells, List<PerimeterEdge> result)
        {
            result.Clear();
            if (cells == null) return;

            for (int i = 0; i < cells.Count; i++)
            {
                for (int d = 0; d < GridDirections.Length; d++)
                {
                    if (!ContainsCell(cells, cells[i] + GridDirections[d]))
                    {
                        result.Add(new PerimeterEdge { Cell = cells[i], GridDirection = GridDirections[d] });
                    }
                }
            }
        }

        public static int CalculateParticleCount(int edgeCount, float perEdge, int min, int max)
        {
            if (edgeCount <= 0) return 0;
            return Mathf.Clamp(Mathf.RoundToInt(edgeCount * perEdge), min, Mathf.Max(min, max));
        }

        private static bool ContainsCell(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell) return true;
            }
            return false;
        }

        private void BuildParticleSystem()
        {
            // Root riêng, không làm con của BlockGrid vì ClearGrid() destroy toàn bộ child
            var vfxObj = new GameObject("BlockPlaceVFX");
            particles = vfxObj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true; // Luôn chạy để mô phỏng các hạt Emit() thủ công
            main.playOnAwake = false;
            main.duration = 1f;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 256;

            var emission = particles.emission;
            emission.enabled = false;

            var shape = particles.shape;
            shape.enabled = false;

            var limitVelocity = particles.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.limit = 1000f;
            limitVelocity.drag = drag;
            limitVelocity.multiplyDragByParticleSize = false;
            limitVelocity.multiplyDragByParticleVelocity = false;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 0f)));

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = gradient;

            var psRenderer = vfxObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sortingOrder = sortingOrder;
            if (materialOverride != null)
            {
                psRenderer.sharedMaterial = materialOverride;
            }
            else
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader) { name = "BlockPlaceVFX_Runtime" };
                    psRenderer.sharedMaterial = runtimeMaterial;
                }
                else
                {
                    Debug.LogWarning("[BlockPlaceEffect] Không tìm thấy shader Sprites/Default, hãy gán materialOverride.");
                }
            }

            particles.Play();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minParticles = Mathf.Max(0, minParticles);
            maxParticles = Mathf.Max(minParticles, maxParticles);
            drag = Mathf.Max(0.01f, drag);

            // Cho phép tinh chỉnh trực tiếp trong Play Mode
            if (particles != null)
            {
                var limitVelocity = particles.limitVelocityOverLifetime;
                limitVelocity.drag = drag;
                particles.GetComponent<ParticleSystemRenderer>().sortingOrder = sortingOrder;
            }
        }
#endif
    }
}
