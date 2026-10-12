using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Báo trước hàng/cột sẽ bị phá khi đang kéo khối: đổi màu ô theo khối, viền glow nhấp nháy và hạt lấp lánh (giống Block Blast)
    /// </summary>
    [AddComponentMenu("Gameplay/Line Clear Preview Effect")]
    public class LineClearPreviewEffect : MonoBehaviour
    {
        private struct TintedBlock
        {
            public Vector2Int Cell;
            public GameObject Block;
            public SpriteRenderer Renderer;
            public Color OriginalColor;
        }

        [Header("References")]
        [Tooltip("Tham chiếu BlockGrid (tự tìm nếu để trống)")]
        [SerializeField] private BlockGrid blockGrid;
        [Tooltip("ParticleSystem hạt lấp lánh (size, lifetime, màu, xoay, material, sorting chỉnh trực tiếp trên ParticleSystem)")]
        [SerializeField] private ParticleSystem sparkles;
        [Tooltip("Khuôn viền 9-slice (luôn tắt), được clone cho mỗi hàng/cột. Sprite, màu, material, sorting chỉnh trên object này")]
        [SerializeField] private SpriteRenderer glowTemplate;

        [Header("Tint")]
        [Tooltip("Đổi màu các ô trong hàng/cột sắp phá sang màu khối đang kéo")]
        [SerializeField] private bool tintBlocks = true;

        [Header("Glow")]
        [Tooltip("Độ tràn của viền ra ngoài hàng/cột (× cellSize)")]
        [SerializeField] private float glowPadding = 0.3f;
        [Tooltip("Khoảng cách (pixel trên sprite) từ mép sprite tới mép hàng/cột, dùng để căn viền khi đổi sprite")]
        [SerializeField] private float glowLineEdgePixels = 16f;
        [Tooltip("Khoảng alpha nhấp nháy (min, max)")]
        [SerializeField] private Vector2 pulseAlphaRange = new Vector2(0.75f, 1f);
        [Tooltip("Thời gian nửa nhịp nhấp nháy (giây)")]
        [SerializeField] private float pulseDuration = 0.45f;
        [Tooltip("Thời gian hiện/ẩn viền (giây)")]
        [SerializeField] private float fadeDuration = 0.1f;

        [Header("Sparkles")]
        [SerializeField] private float sparklesPerSecondPerLine = 14f;
        [Tooltip("Tốc độ trôi ra ngoài viền (× cellSize / giây)")]
        [SerializeField] private Vector2 sparkleSpeedRange = new Vector2(0.15f, 0.5f);

        private readonly List<SpriteRenderer> glowPool = new List<SpriteRenderer>();
        private readonly List<Rect> activeLineRects = new List<Rect>();
        private readonly List<TintedBlock> tintedBlocks = new List<TintedBlock>();
        private Color baseGlowColor;
        private bool isPreviewing;
        private bool glowsVisible;
        private float fade;
        private float pulseTime;
        private float sparkleAccumulator;

        public bool IsPreviewing => isPreviewing;
        public ParticleSystem Sparkles => sparkles;
        public SpriteRenderer GlowTemplate => glowTemplate;
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

            if (sparkles == null || glowTemplate == null)
            {
                Debug.LogError("[LineClearPreviewEffect] Thiếu tham chiếu Sparkles hoặc Glow Template trong prefab.", this);
                enabled = false;
                return;
            }

            baseGlowColor = glowTemplate.color;
            glowTemplate.gameObject.SetActive(false);
            sparkles.Play();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            Hide();
            fade = 0f;
            ApplyGlowAlpha();
        }

        private void SubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnClearPreviewChanged -= HandleClearPreviewChanged;
                blockGrid.OnClearPreviewChanged += HandleClearPreviewChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnClearPreviewChanged -= HandleClearPreviewChanged;
            }
        }

        private void HandleClearPreviewChanged(LineClearResult result, Color shapeColor)
        {
            if (result.HasLines)
            {
                Show(result, shapeColor);
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Bật preview cho các hàng/cột trong result, đổi màu ô sang shapeColor
        /// </summary>
        public void Show(LineClearResult result, Color shapeColor)
        {
            if (blockGrid == null || !result.HasLines)
            {
                Hide();
                return;
            }

            RestoreTint();
            if (tintBlocks)
            {
                ApplyTint(result, shapeColor);
            }

            LayoutGlows(result);
            if (!isPreviewing)
            {
                pulseTime = 0f;
            }
            isPreviewing = true;
            ApplyGlowAlpha();
        }

        /// <summary>
        /// Tắt preview: trả màu ô còn trên bàn, viền mờ dần, hạt còn lại tự tàn
        /// </summary>
        public void Hide()
        {
            RestoreTint();
            isPreviewing = false;
            sparkleAccumulator = 0f;
        }

        private void Update()
        {
            if (!isPreviewing && fade <= 0f && !glowsVisible) return;

            float dt = Time.deltaTime;
            float target = isPreviewing ? 1f : 0f;
            fade = fadeDuration > 0f ? Mathf.MoveTowards(fade, target, dt / fadeDuration) : target;
            pulseTime += dt;
            ApplyGlowAlpha();

            if (isPreviewing)
            {
                EmitSparkles(dt);
            }
        }

        private void ApplyTint(LineClearResult result, Color shapeColor)
        {
            foreach (Vector2Int cell in result.Cells)
            {
                GameObject block = blockGrid.GetOccupiedBlock(cell.x, cell.y);
                if (block == null) continue;

                var sr = block.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                tintedBlocks.Add(new TintedBlock
                {
                    Cell = cell,
                    Block = block,
                    Renderer = sr,
                    OriginalColor = sr.color
                });

                Color tint = shapeColor;
                tint.a = sr.color.a;
                sr.color = tint;
            }
        }

        private void RestoreTint()
        {
            for (int i = 0; i < tintedBlocks.Count; i++)
            {
                TintedBlock tinted = tintedBlocks[i];
                // Block đã bị phá (đang tween fade/scale) không còn nằm trên bàn => giữ nguyên để không phá animation
                if (tinted.Renderer == null || blockGrid == null) continue;
                if (blockGrid.GetOccupiedBlock(tinted.Cell.x, tinted.Cell.y) != tinted.Block) continue;

                tinted.Renderer.color = tinted.OriginalColor;
            }
            tintedBlocks.Clear();
        }

        private void LayoutGlows(LineClearResult result)
        {
            // Root prefab bám theo Grid (không làm con Grid vì ClearGrid() destroy toàn bộ child)
            Transform gridTransform = blockGrid.transform;
            transform.SetPositionAndRotation(gridTransform.position, gridTransform.rotation);
            transform.localScale = gridTransform.lossyScale;

            float cellSize = blockGrid.CellSize;
            float padding = glowPadding * cellSize;
            activeLineRects.Clear();

            for (int i = 0; i < result.Rows.Count; i++)
            {
                activeLineRects.Add(GetLineLocalRect(true, result.Rows[i], blockGrid.MaxRow, blockGrid.MaxColumn,
                    cellSize, blockGrid.Spacing, padding));
            }
            for (int i = 0; i < result.Columns.Count; i++)
            {
                activeLineRects.Add(GetLineLocalRect(false, result.Columns[i], blockGrid.MaxRow, blockGrid.MaxColumn,
                    cellSize, blockGrid.Spacing, padding));
            }

            // Scale sao cho khoảng từ mép sprite tới mép hàng (glowLineEdgePixels) bằng đúng padding
            Sprite sprite = glowTemplate.sprite;
            float edgeUnits = sprite != null ? glowLineEdgePixels / sprite.pixelsPerUnit : 0f;
            float scale = edgeUnits > 0f ? Mathf.Max(0.0001f, padding) / edgeUnits : 1f;

            while (glowPool.Count < activeLineRects.Count)
            {
                glowPool.Add(CreateGlowRenderer());
            }

            for (int i = 0; i < glowPool.Count; i++)
            {
                SpriteRenderer glow = glowPool[i];
                bool active = i < activeLineRects.Count;
                glow.gameObject.SetActive(active);
                if (!active) continue;

                Rect rect = activeLineRects[i];
                glow.transform.localPosition = rect.center;
                glow.transform.localRotation = Quaternion.identity;
                glow.transform.localScale = new Vector3(scale, scale, 1f);
                glow.size = rect.size / scale;
            }

            glowsVisible = activeLineRects.Count > 0;
        }

        private SpriteRenderer CreateGlowRenderer()
        {
            SpriteRenderer sr = Instantiate(glowTemplate, transform, false);
            sr.name = "LineGlow";
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.gameObject.SetActive(false);
            return sr;
        }

        private void ApplyGlowAlpha()
        {
            float pulse = pulseDuration > 0f
                ? Mathf.Lerp(pulseAlphaRange.x, pulseAlphaRange.y, Mathf.PingPong(pulseTime / pulseDuration, 1f))
                : pulseAlphaRange.y;
            Color color = baseGlowColor;
            color.a = baseGlowColor.a * pulse * fade;
            bool hideGlows = fade <= 0f && !isPreviewing;

            for (int i = 0; i < glowPool.Count; i++)
            {
                SpriteRenderer glow = glowPool[i];
                if (glow == null) continue;

                if (hideGlows)
                {
                    glow.gameObject.SetActive(false);
                    continue;
                }
                glow.color = color;
            }

            if (hideGlows)
            {
                glowsVisible = false;
            }
        }

        private void EmitSparkles(float dt)
        {
            if (sparkles == null || activeLineRects.Count == 0) return;

            sparkleAccumulator += sparklesPerSecondPerLine * activeLineRects.Count * dt;
            int count = Mathf.FloorToInt(sparkleAccumulator);
            if (count <= 0) return;
            sparkleAccumulator -= count;

            float cellSize = blockGrid.CellSize;
            float padding = glowPadding * cellSize;
            var emitParams = new ParticleSystem.EmitParams();

            for (int i = 0; i < count; i++)
            {
                Rect rect = activeLineRects[Random.Range(0, activeLineRects.Count)];
                bool isRow = rect.width >= rect.height;
                float side = Random.value < 0.5f ? -1f : 1f;

                // Mép dài của viền, nằm ngay trên mép hàng/cột (rect đã gồm padding)
                Vector2 localPos;
                Vector2 outward;
                if (isRow)
                {
                    float x = Random.Range(rect.xMin + padding, rect.xMax - padding);
                    float y = rect.center.y + side * (rect.height * 0.5f - padding * Random.Range(0.3f, 1f));
                    localPos = new Vector2(x, y);
                    outward = new Vector2(0f, side);
                }
                else
                {
                    float y = Random.Range(rect.yMin + padding, rect.yMax - padding);
                    float x = rect.center.x + side * (rect.width * 0.5f - padding * Random.Range(0.3f, 1f));
                    localPos = new Vector2(x, y);
                    outward = new Vector2(side, 0f);
                }

                Vector2 localDir = (outward + Vector2.up * 0.5f + Random.insideUnitCircle * 0.4f).normalized;
                float speed = Random.Range(sparkleSpeedRange.x, sparkleSpeedRange.y) * cellSize;

                // Chỉ đặt vị trí + vận tốc; size, lifetime, màu, xoay lấy từ main module của ParticleSystem
                emitParams.position = transform.TransformPoint(localPos);
                emitParams.velocity = transform.TransformVector(localDir * speed);

                sparkles.Emit(emitParams, 1);
            }
        }

        /// <summary>
        /// Khung viền của hàng/cột trong toạ độ cục bộ Grid (gồm padding mỗi bên). Hàng 0 ở trên cùng, cột 0 bên trái.
        /// </summary>
        public static Rect GetLineLocalRect(bool isRow, int index, int rows, int cols, float cellSize, float spacing, float padding)
        {
            float step = cellSize + spacing;
            float gridWidth = cols * cellSize + (cols - 1) * spacing;
            float gridHeight = rows * cellSize + (rows - 1) * spacing;

            if (isRow)
            {
                float centerY = gridHeight * 0.5f - cellSize * 0.5f - index * step;
                return new Rect(
                    -gridWidth * 0.5f - padding,
                    centerY - cellSize * 0.5f - padding,
                    gridWidth + padding * 2f,
                    cellSize + padding * 2f);
            }

            float centerX = -gridWidth * 0.5f + cellSize * 0.5f + index * step;
            return new Rect(
                centerX - cellSize * 0.5f - padding,
                -gridHeight * 0.5f - padding,
                cellSize + padding * 2f,
                gridHeight + padding * 2f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            glowPadding = Mathf.Max(0f, glowPadding);
            pulseDuration = Mathf.Max(0f, pulseDuration);
            fadeDuration = Mathf.Max(0f, fadeDuration);
            sparklesPerSecondPerLine = Mathf.Max(0f, sparklesPerSecondPerLine);
            glowLineEdgePixels = Mathf.Max(0f, glowLineEdgePixels);
        }
#endif
    }
}
