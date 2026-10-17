using DG.Tweening;
using Quang.Base;
using Solo.MOST_IN_ONE;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Phản hồi va chạm khi phá hàng/cột: rung toàn màn hình, rung thiết bị (haptic) và viền sáng gradient loé lên quanh khung board rồi mờ dần (giống Block Blast)
    /// </summary>
    [AddComponentMenu("Gameplay/Line Clear Impact Effect")]
    public class LineClearImpactEffect : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Tham chiếu BlockGrid (tự tìm nếu để trống)")]
        [SerializeField] private BlockGrid blockGrid;
        [Tooltip("Rung màn hình (tự tìm nếu để trống)")]
        [SerializeField] private ScreenShake screenShake;
        [Tooltip("Viền 9-slice rỗng giữa ôm khung board. Sprite, sorting chỉnh trên object này")]
        [SerializeField] private SpriteRenderer glowRenderer;
        [Tooltip("Material mẫu dùng shader BlockDrag/LineClearRainbowGlow")]
        [SerializeField] private Material rainbowGlowMaterial;

        [Header("Toggle")]
        [SerializeField] private bool enableShake = true;
        [SerializeField] private bool enableGlow = true;
        [SerializeField] private bool enableHaptic = true;

        [Header("Haptic")]
        [Tooltip("Số đường phá cùng lúc từ mức này trở lên dùng rung mạnh (HeavyImpact)")]
        [SerializeField] private int heavyHapticLineCount = 2;
        [Tooltip("Khoảng nghỉ tối thiểu giữa hai lần rung (giây)")]
        [SerializeField] private float hapticCooldown = 0.1f;

        [Header("Glow Layout")]
        [Tooltip("Độ tràn quầng sáng ra ngoài khung board (× cellSize)")]
        [SerializeField] private float glowPadding = 0.3f;
        [Tooltip("Khoảng cách (pixel trên sprite) từ mép sprite tới vạch sáng, dùng để căn vạch sáng trùng mép khung")]
        [SerializeField] private float glowLineEdgePixels = 16f;
        [Tooltip("Phần viền khung board (× cellSize), khớp BlockGrid.framePaddingRatio")]
        [SerializeField] private float framePaddingRatio = 0.12f;

        [Header("Glow Timing")]
        [SerializeField] private float peakAlpha = 1f;
        [Tooltip("Thời gian giữ sáng trước khi mờ (giây)")]
        [SerializeField] private float holdDuration = 0.15f;
        [Tooltip("Thời gian mờ (x: phá 1 đường, y: phá từ Max Lines For Fade đường)")]
        [SerializeField] private Vector2 fadeDurationRange = new Vector2(0.25f, 0.4f);
        [SerializeField] private int maxLinesForFade = 3;

        [Header("Glow Color")]
        [SerializeField] private Gradient glowGradient = CreateDefaultGradient();
        [Tooltip("Hướng đổi màu trên bàn (x: ngang, y: dọc). Giữ |x| + |y| <= 1 để gradient không bị cuộn vòng (shader dùng frac)")]
        [SerializeField] private Vector2 gradientDirection = new Vector2(0.36f, 0.6f);

        private const int GradientTextureWidth = 256;
        private static readonly int GradientTexId = Shader.PropertyToID("_GradientTex");
        private static readonly int BoundsMinId = Shader.PropertyToID("_BoundsMin");
        private static readonly int BoundsSizeId = Shader.PropertyToID("_BoundsSize");
        private static readonly int GradientDirId = Shader.PropertyToID("_GradientDir");
        private static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");

        private Material glowInstance;
        private Texture2D gradientTexture;
        private Sequence glowSequence;

        public bool IsGlowVisible => glowRenderer != null && glowRenderer.enabled;
        public SpriteRenderer GlowRenderer => glowRenderer;

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

        public ScreenShake Shake
        {
            get => screenShake;
            set => screenShake = value;
        }

        private void Awake()
        {
            if (blockGrid == null)
            {
                blockGrid = FindObjectOfType<BlockGrid>();
            }
            if (screenShake == null)
            {
                screenShake = FindObjectOfType<ScreenShake>();
            }

            if (glowRenderer == null)
            {
                Debug.LogWarning("[LineClearImpactEffect] Thiếu Glow Renderer, sẽ chỉ rung màn hình.", this);
                return;
            }

            glowRenderer.drawMode = SpriteDrawMode.Sliced;
            glowRenderer.enabled = false;

            if (rainbowGlowMaterial != null)
            {
                gradientTexture = new Texture2D(GradientTextureWidth, 1, TextureFormat.RGBA32, false)
                {
                    name = "BoardImpactGlowGradient",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                gradientTexture.SetPixels32(LineClearPreviewEffect.BakeGradientPixels(glowGradient, GradientTextureWidth));
                gradientTexture.Apply(false);

                glowInstance = new Material(rainbowGlowMaterial) { hideFlags = HideFlags.DontSave };
                glowInstance.SetTexture(GradientTexId, gradientTexture);
                glowInstance.SetVector(GradientDirId, gradientDirection);
                glowInstance.SetFloat(ScrollSpeedId, 0f);
                glowRenderer.sharedMaterial = glowInstance;
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            HideGlow();
        }

        private void OnDestroy()
        {
            glowSequence?.Kill();
            DestroyRuntimeObject(glowInstance);
            DestroyRuntimeObject(gradientTexture);
            glowInstance = null;
            gradientTexture = null;
        }

        private static void DestroyRuntimeObject(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void SubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnLinesCleared -= HandleLinesCleared;
                blockGrid.OnLinesCleared += HandleLinesCleared;
            }
        }

        private void UnsubscribeEvents()
        {
            if (blockGrid != null)
            {
                blockGrid.OnLinesCleared -= HandleLinesCleared;
            }
        }

        private void HandleLinesCleared(int rowsCleared, int colsCleared, int totalCellsCleared)
        {
            PlayImpact(rowsCleared + colsCleared);
        }

        /// <summary>
        /// Rung màn hình + loé viền quanh board cho lần phá lineCount đường
        /// </summary>
        public void PlayImpact(int lineCount)
        {
            if (lineCount <= 0) return;

            if (enableShake && screenShake != null)
            {
                screenShake.Shake();
            }

            if (enableGlow)
            {
                PlayGlow(lineCount);
            }

            if (enableHaptic)
            {
                PlayHaptic(lineCount);
            }
        }

        private void PlayHaptic(int lineCount)
        {
            if (IsHapticOff()) return;

            MOST_HapticFeedback.GenerateWithCooldown(GetHapticType(lineCount, heavyHapticLineCount), hapticCooldown);
        }

        /// <summary>
        /// Người chơi tắt rung trong Setting (thiếu Locator.Data thì coi như đang bật)
        /// </summary>
        private static bool IsHapticOff()
        {
            GameData data = Locator.Data?.GetData<GameData>();
            return data?.setting != null && data.setting.hapticOff;
        }

        private void PlayGlow(int lineCount)
        {
            if (glowRenderer == null || blockGrid == null) return;

            glowSequence?.Kill();
            FitGlowToBoard();

            // Bật tức thì ở alpha đỉnh, giữ ngắn rồi mờ dần
            SetGlowAlpha(peakAlpha);
            glowRenderer.enabled = true;

            glowSequence = DOTween.Sequence()
                .AppendInterval(Mathf.Max(0f, holdDuration))
                .Append(glowRenderer.DOFade(0f, GetFadeDuration(lineCount, fadeDurationRange, maxLinesForFade)).SetEase(Ease.OutQuad))
                .OnComplete(() => glowRenderer.enabled = false)
                .SetLink(gameObject);
        }

        private void HideGlow()
        {
            glowSequence?.Kill();
            glowSequence = null;
            if (glowRenderer != null)
            {
                glowRenderer.enabled = false;
            }
        }

        private void SetGlowAlpha(float alpha)
        {
            Color color = glowRenderer.color;
            color.a = alpha;
            glowRenderer.color = color;
        }

        /// <summary>
        /// Đặt viền ôm khung board: vạch sáng của sprite trùng mép khung, quầng tràn ra ngoài glowPadding
        /// </summary>
        private void FitGlowToBoard()
        {
            // Root bám theo Grid (không làm con Grid vì ClearGrid() destroy toàn bộ child)
            Transform gridTransform = blockGrid.transform;
            transform.SetPositionAndRotation(gridTransform.position, gridTransform.rotation);
            transform.localScale = gridTransform.lossyScale;

            float cellSize = blockGrid.CellSize;
            float padding = Mathf.Max(0.0001f, glowPadding * cellSize);
            Vector2 size = GetGlowLocalSize(blockGrid.GridWidth, blockGrid.GridHeight, cellSize, framePaddingRatio, glowPadding);

            Sprite sprite = glowRenderer.sprite;
            float edgeUnits = sprite != null ? glowLineEdgePixels / sprite.pixelsPerUnit : 0f;
            float scale = edgeUnits > 0f ? padding / edgeUnits : 1f;

            Transform glowTransform = glowRenderer.transform;
            glowTransform.localPosition = Vector3.zero;
            glowTransform.localRotation = Quaternion.identity;
            glowTransform.localScale = new Vector3(scale, scale, 1f);
            glowRenderer.size = size / scale;

            if (glowInstance != null)
            {
                Vector2 worldA = transform.TransformPoint(-size * 0.5f);
                Vector2 worldB = transform.TransformPoint(size * 0.5f);
                Vector2 min = Vector2.Min(worldA, worldB);
                glowInstance.SetVector(BoundsMinId, min);
                glowInstance.SetVector(BoundsSizeId, Vector2.Max(worldA, worldB) - min);
            }
        }

        /// <summary>
        /// Kích thước cục bộ của viền = khung board + quầng glowPadding mỗi cạnh
        /// </summary>
        public static Vector2 GetGlowLocalSize(float gridWidth, float gridHeight, float cellSize, float framePaddingRatio, float glowPadding)
        {
            Vector2 frame = BlockGrid.ComputeFrameSize(gridWidth, gridHeight, cellSize, framePaddingRatio);
            float halo = 2f * cellSize * Mathf.Max(0f, glowPadding);
            return new Vector2(frame.x + halo, frame.y + halo);
        }

        /// <summary>
        /// Thời gian mờ nội suy tuyến tính theo số đường: 1 đường = range.x, từ maxLines đường = range.y
        /// </summary>
        public static float GetFadeDuration(int lineCount, Vector2 range, int maxLines)
        {
            if (maxLines <= 1) return range.y;
            float t = Mathf.Clamp01((lineCount - 1f) / (maxLines - 1f));
            return Mathf.Lerp(range.x, range.y, t);
        }

        /// <summary>
        /// Loại rung theo số đường phá: dưới heavyLineCount = MediumImpact, từ heavyLineCount trở lên = HeavyImpact
        /// </summary>
        public static MOST_HapticFeedback.HapticTypes GetHapticType(int lineCount, int heavyLineCount)
        {
            return lineCount >= heavyLineCount
                ? MOST_HapticFeedback.HapticTypes.HeavyImpact
                : MOST_HapticFeedback.HapticTypes.MediumImpact;
        }

        private static Gradient CreateDefaultGradient()
        {
            // Hướng (0.36, 0.6): đầu gradient ở dưới/trái (cyan), cuối ở trên/phải (hồng) như video tham chiếu
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color32(0x3D, 0xF2, 0xFF, 0xFF), 0f),
                    new GradientColorKey(new Color32(0x3D, 0x9B, 0xFF, 0xFF), 0.35f),
                    new GradientColorKey(new Color32(0xA8, 0x5C, 0xFF, 0xFF), 0.7f),
                    new GradientColorKey(new Color32(0xFF, 0x5C, 0xD6, 0xFF), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }
    }
}
