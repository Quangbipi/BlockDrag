using System.Collections.Generic;
using Base;
using Gameplay.BlockDrag;
using TMPro;
using UnityEngine;

namespace UI
{
    [AddComponentMenu("UI/Combo Effect Manager")]
    public class ComboEffectManager : MonoBehaviour
    {
        public static ComboEffectManager Ins { get; private set; }

        [Header("Configuration")]
        [Tooltip("Prefab hiệu ứng chữ Combo UI")]
        [SerializeField] private ComboTextEffect effectPrefab;

        [Tooltip("Số lượng object khởi tạo sẵn trong pool")]
        [SerializeField] private int initialPoolSize = 4;

        [Tooltip("Transform chứa các instance pool (để trống sẽ tự gán vào GameplayCanvas)")]
        [SerializeField] private Transform poolContainer;

        private readonly List<ComboTextEffect> pool = new List<ComboTextEffect>();

        public ComboTextEffect EffectPrefab
        {
            get => effectPrefab;
            set => effectPrefab = value;
        }

        public IReadOnlyList<ComboTextEffect> Pool => pool;

        private void Awake()
        {
            if (Ins != null && Ins != this)
            {
                Destroy(this);
                return;
            }
            Ins = this;

            EnsurePoolContainer();

            if (effectPrefab == null)
            {
                effectPrefab = Resources.Load<ComboTextEffect>("Prefabs/ComboTextEffect");
                if (effectPrefab == null)
                {
                    effectPrefab = Resources.Load<ComboTextEffect>("UI/ComboTextEffect");
                }

                if (effectPrefab == null)
                {
                    effectPrefab = CreateDefaultProceduralPrefab();
                }
            }

            InitPool();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Start()
        {
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            if (Ins == this)
            {
                Ins = null;
            }
        }

        private void SubscribeEvents()
        {
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnComboTriggered -= HandleComboTriggered;
                ScoreManager.Ins.OnComboTriggered += HandleComboTriggered;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnComboTriggered -= HandleComboTriggered;
            }
        }

        public Transform EnsurePoolContainer()
        {
            if (poolContainer != null) return poolContainer;

            // Kiểm tra xem đã có GameObject con ComboTextPool chưa
            Transform existingChild = transform.Find("ComboTextPool");
            if (existingChild != null)
            {
                poolContainer = existingChild;
                return poolContainer;
            }

            // Tạo GameObject con riêng biệt làm pool container
            GameObject containerGo = new GameObject("ComboTextPool", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            RectTransform rt = containerGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            poolContainer = containerGo.transform;
            return poolContainer;
        }

        private ComboTextEffect CreateDefaultProceduralPrefab()
        {
            GameObject rootGo = new GameObject("ComboTextEffect_RuntimeTemplate", typeof(RectTransform), typeof(CanvasGroup), typeof(ComboTextEffect));
            rootGo.SetActive(false);
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(rootGo);
            }

            RectTransform rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(400, 100);
            CanvasGroup cvg = rootGo.GetComponent<CanvasGroup>();
            ComboTextEffect effect = rootGo.GetComponent<ComboTextEffect>();

            // Sub-object ComboLabel
            GameObject labelGo = new GameObject("ComboLabel", typeof(RectTransform));
            labelGo.transform.SetParent(rootGo.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(1f, 0.5f);
            labelRect.anchoredPosition = new Vector2(-10f, 0f);
            labelRect.sizeDelta = new Vector2(250f, 80f);

            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "COMBO";
            labelTmp.fontSize = 50f;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.Right;
            labelTmp.color = new Color(1f, 0.82f, 0.15f);

            // Sub-object ComboNumber
            GameObject numberGo = new GameObject("ComboNumber", typeof(RectTransform));
            numberGo.transform.SetParent(rootGo.transform, false);
            RectTransform numberRect = numberGo.GetComponent<RectTransform>();
            numberRect.anchorMin = new Vector2(0.5f, 0.5f);
            numberRect.anchorMax = new Vector2(0.5f, 0.5f);
            numberRect.pivot = new Vector2(0f, 0.5f);
            numberRect.anchoredPosition = new Vector2(10f, 0f);
            numberRect.sizeDelta = new Vector2(150f, 80f);

            var numberTmp = numberGo.AddComponent<TextMeshProUGUI>();
            numberTmp.text = "x2";
            numberTmp.fontSize = 58f;
            numberTmp.fontStyle = FontStyles.Bold;
            numberTmp.alignment = TextAlignmentOptions.Left;
            numberTmp.color = new Color(1f, 1f, 0.9f);

            var anyText = FindObjectOfType<TMP_Text>();
            if (anyText != null && anyText.font != null)
            {
                labelTmp.font = anyText.font;
                numberTmp.font = anyText.font;
            }

            effect.InitReferences(labelTmp, numberTmp, labelGo.transform, numberGo.transform, cvg);
            return effect;
        }

        public void InitPool()
        {
            if (effectPrefab == null) return;
            EnsurePoolContainer();

            while (pool.Count < initialPoolSize)
            {
                CreateNewInstance();
            }
        }

        public ComboTextEffect CreateNewInstance()
        {
            if (effectPrefab == null) return null;
            EnsurePoolContainer();

            ComboTextEffect instance = Instantiate(effectPrefab, poolContainer);
            instance.gameObject.SetActive(false);
            pool.Add(instance);
            return instance;
        }

        public ComboTextEffect GetFromPool()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                {
                    return pool[i];
                }
            }
            return CreateNewInstance();
        }

        public void HandleComboTriggered(int comboCount, Vector3 spawnPosition)
        {
            EnsurePoolContainer();

            ComboTextEffect effect = GetFromPool();
            if (effect != null)
            {
                effect.Play(comboCount, spawnPosition);
            }
        }
    }
}
