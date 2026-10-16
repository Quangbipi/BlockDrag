using System;
using System.Collections.Generic;
using Quang.Base;
using Quang.DesignPattern;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Spawn chữ "+N" (TextScoreFly) từ pool mỗi khi ăn điểm phá hàng / Perfect Clear và cho bay lên chữ Score.
    /// PendingAmount = tổng điểm đang bay, GameplayCanvas trừ đi để Score chỉ chạy số khi chữ bay tới nơi.
    /// </summary>
    [AddComponentMenu("UI/Score Fly Manager")]
    public class ScoreFlyManager : MonoBehaviour
    {
        private const string PoolContainerName = "ScoreFlyPool";

        [Header("References")]
        [Tooltip("Prefab chữ điểm bay (để trống sẽ load Resources/Prefabs/TextScoreFly)")]
        [SerializeField] private TextScoreFly flyPrefab;

        [Tooltip("Đích bay tới (chữ Score trên GameplayCanvas)")]
        [SerializeField] private RectTransform flyTarget;

        [Tooltip("Transform chứa các instance pool (để trống sẽ tự tạo con ScoreFlyPool)")]
        [SerializeField] private RectTransform poolContainer;

        [Header("Pool")]
        [SerializeField] private int initialPoolSize = 4;

        [Header("Spawn")]
        [Tooltip("Các nguồn điểm có chữ bay")]
        [SerializeField] private ScoreSource[] flySources = { ScoreSource.LineClear, ScoreSource.PerfectClear };

        [Tooltip("Độ lệch so với vị trí ăn điểm (đẩy lên trên chữ Combo)")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 90f);

        [Tooltip("Thời gian chờ trước khi chữ hiện ra sau khi phá hàng")]
        [SerializeField] private float lineClearDelay = 0.3f;

        [Tooltip("Chờ thêm cho chữ Perfect Clear để không chồng lên chữ phá hàng cùng lượt")]
        [SerializeField] private float perfectClearExtraDelay = 0.25f;

        [Tooltip("Khoảng cách tối thiểu với mép màn hình (Canvas unit)")]
        [SerializeField] private float screenEdgePadding = 80f;

        private readonly MiniPool<TextScoreFly> pool = new MiniPool<TextScoreFly>();
        private readonly List<TextScoreFly> flying = new List<TextScoreFly>();
        private Action<TextScoreFly> arriveHandler;
        private bool poolReady;
        private IScoreService boundScore;

        public event Action<int> OnFlyArrived;

        public int PendingAmount { get; private set; }
        public int FlyingCount => flying.Count;

        public TextScoreFly FlyPrefab
        {
            get => flyPrefab;
            set => flyPrefab = value;
        }

        public RectTransform FlyTarget
        {
            get => flyTarget;
            set => flyTarget = value;
        }

        private void Awake()
        {
            EnsurePool();
        }

        private void OnEnable()
        {
            Locator.ScoreChanged -= BindScore;
            Locator.ScoreChanged += BindScore;
            BindScore(Locator.Score);
        }

        private void OnDisable()
        {
            Locator.ScoreChanged -= BindScore;
            UnbindScore();
            CancelAll();
        }

        private void BindScore(IScoreService score)
        {
            if (ReferenceEquals(score, boundScore)) return;

            UnbindScore();
            CancelAll();
            if (score == null) return;

            boundScore = score;
            boundScore.OnScoreGained += HandleScoreGained;
        }

        private void UnbindScore()
        {
            if (boundScore == null) return;

            boundScore.OnScoreGained -= HandleScoreGained;
            boundScore = null;
        }

        private void HandleScoreGained(ScoreGain gain)
        {
            TrySpawn(gain);
        }

        /// <summary>
        /// Spawn chữ bay cho lần cộng điểm; false nếu nguồn không bay hoặc thiếu prefab/đích/toạ độ
        /// </summary>
        public bool TrySpawn(ScoreGain gain)
        {
            if (gain.Amount <= 0 || !gain.HasWorldPos || !ShouldFly(gain.Source)) return false;
            if (flyTarget == null || !EnsurePool()) return false;

            Vector2 startPos = WorldToContainerPoint(gain.WorldPos) + spawnOffset;
            startPos = ClampToContainer(startPos);

            float delay = lineClearDelay;
            if (gain.Source == ScoreSource.PerfectClear)
            {
                delay += perfectClearExtraDelay;
            }

            // Vẽ đè lên board và chữ Combo
            poolContainer.SetAsLastSibling();

            TextScoreFly fly = pool.Spawn();
            flying.Add(fly);
            PendingAmount += gain.Amount;
            arriveHandler ??= HandleArrive;
            fly.Play(gain.Amount, startPos, flyTarget, delay, arriveHandler);
            return true;
        }

        /// <summary>
        /// Huỷ mọi chữ đang bay, không cộng điểm (ván mới / đổi ScoreService)
        /// </summary>
        public void CancelAll()
        {
            for (int i = 0; i < flying.Count; i++)
            {
                if (flying[i] != null)
                {
                    flying[i].ForceComplete();
                }
            }
            flying.Clear();
            PendingAmount = 0;

            if (poolReady)
            {
                pool.Collect();
            }
        }

        private void HandleArrive(TextScoreFly fly)
        {
            if (!flying.Remove(fly)) return;

            PendingAmount = Mathf.Max(0, PendingAmount - fly.Amount);
            pool.Despawn(fly);
            OnFlyArrived?.Invoke(fly.Amount);
        }

        private bool ShouldFly(ScoreSource source)
        {
            if (flySources == null) return false;
            for (int i = 0; i < flySources.Length; i++)
            {
                if (flySources[i] == source) return true;
            }
            return false;
        }

        private bool EnsurePool()
        {
            if (poolReady) return true;

            if (flyPrefab == null)
            {
                flyPrefab = Resources.Load<TextScoreFly>("Prefabs/TextScoreFly");
            }
            if (flyPrefab == null) return false;

            EnsurePoolContainer();
            pool.OnInit(flyPrefab, Mathf.Max(0, initialPoolSize), poolContainer);
            poolReady = true;
            return true;
        }

        private void EnsurePoolContainer()
        {
            if (poolContainer != null) return;

            Transform existing = transform.Find(PoolContainerName);
            if (existing != null)
            {
                poolContainer = (RectTransform)existing;
                return;
            }

            GameObject containerGo = new GameObject(PoolContainerName, typeof(RectTransform));
            containerGo.layer = gameObject.layer;
            containerGo.transform.SetParent(transform, false);
            RectTransform rt = containerGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            poolContainer = rt;
        }

        /// <summary>
        /// World (board) → screen → toạ độ local của pool container (giống ComboTextEffect)
        /// </summary>
        private Vector2 WorldToContainerPoint(Vector3 worldPos)
        {
            Camera worldCam = Camera.main;
            if (worldCam == null && Camera.allCameras.Length > 0)
            {
                worldCam = Camera.allCameras[0];
            }

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Camera uiCam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : worldCam;
            }

            Vector3 screenPoint = worldCam != null ? worldCam.WorldToScreenPoint(worldPos) : worldPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(poolContainer, screenPoint, uiCam, out Vector2 localPoint);
            return localPoint;
        }

        private Vector2 ClampToContainer(Vector2 point)
        {
            Rect bounds = poolContainer.rect;
            if (bounds.width <= 0f || bounds.height <= 0f) return point;

            float minX = bounds.xMin + screenEdgePadding;
            float maxX = bounds.xMax - screenEdgePadding;
            float minY = bounds.yMin + screenEdgePadding;
            float maxY = bounds.yMax - screenEdgePadding;

            if (minX <= maxX) point.x = Mathf.Clamp(point.x, minX, maxX);
            if (minY <= maxY) point.y = Mathf.Clamp(point.y, minY, maxY);
            return point;
        }
    }
}
