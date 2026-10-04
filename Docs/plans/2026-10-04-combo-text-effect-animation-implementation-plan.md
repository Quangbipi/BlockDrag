# Combo Text Animation FX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hiển thị text Combo kèm animation scale tuần tự (chữ "COMBO" trước, số combo "x2" sau) tại tâm khối BlockShape vừa đặt khi chuỗi combo đạt từ 2 trở lên.

**Architecture:** Mở rộng `BlockGrid` để tính tâm hình học của khối vừa đặt và phát sự kiện kèm toạ độ; cập nhật `ScoreManager` bắn sự kiện `OnComboTriggered(comboCount, worldPos)` khi `combo >= 2`; xây dựng `ComboTextEffect` chạy hoạt họa DOTween tuần tự và `ComboEffectManager` quản lý Object Pool để tái sử dụng hiệu ứng không gây GC.

**Tech Stack:** Unity 2022.3.62f2, C#, DOTween, TextMeshPro, NUnit (Unity Test Framework).

---

### Task 1: Mở rộng `BlockGrid` tính toán tâm hình học khối vừa đặt và phát sự kiện vị trí

**Files:**
- Modify: `Assets/_Game/_BlockDrag/BlockGrid.cs`
- Test: `Assets/Tests/EditMode/BlockGridPlacementCenterTests.cs`

- [x] **Step 1: Viết test kiểm tra tính toán tâm vị trí đặt khối**

Tạo file [`Assets/Tests/EditMode/BlockGridPlacementCenterTests.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/Tests/EditMode/BlockGridPlacementCenterTests.cs):
```csharp
using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

public class BlockGridPlacementCenterTests
{
    private GameObject gridGo;
    private BlockGrid grid;

    [SetUp]
    public void SetUp()
    {
        gridGo = new GameObject("BlockGrid_Test");
        grid = gridGo.AddComponent<BlockGrid>();
        grid.InitializeGrid(8, 8, 1f, 0f);
    }

    [TearDown]
    public void TearDown()
    {
        if (gridGo != null)
        {
            Object.DestroyImmediate(gridGo);
        }
    }

    [Test]
    public void CalculatePlacedCenterPosition_SingleCell_ReturnsCellWorldPosition()
    {
        Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0) };
        Vector3 expectedPos = grid.GetCellWorldPosition(0, 0);
        
        Vector3 center = grid.CalculateCellsCenterPosition(cells);
        
        Assert.AreEqual(expectedPos.x, center.x, 0.001f);
        Assert.AreEqual(expectedPos.y, center.y, 0.001f);
    }

    [Test]
    public void CalculateCellsCenterPosition_TwoCells_ReturnsAveragePosition()
    {
        Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1) };
        Vector3 pos0 = grid.GetCellWorldPosition(0, 0);
        Vector3 pos1 = grid.GetCellWorldPosition(0, 1);
        Vector3 expectedCenter = (pos0 + pos1) * 0.5f;

        Vector3 center = grid.CalculateCellsCenterPosition(cells);

        Assert.AreEqual(expectedCenter.x, center.x, 0.001f);
        Assert.AreEqual(expectedCenter.y, center.y, 0.001f);
    }
}
```

- [x] **Step 2: Chạy test để xác nhận test FAIL (chưa có method CalculateCellsCenterPosition)**

Chạy Unity EditMode tests.
Kỳ vọng: Lỗi biên dịch vì chưa có `CalculateCellsCenterPosition`.

- [x] **Step 3: Cập nhật `BlockGrid.cs` bổ sung `CalculateCellsCenterPosition`, `LastPlacedCenterPosition` và `OnLinesClearedWithPos`**

Sửa trong [`Assets/_Game/_BlockDrag/BlockGrid.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/BlockGrid.cs):
1. Thêm event & property:
```csharp
    public Vector3 LastPlacedCenterPosition { get; private set; }
    public event System.Action<int, int, int, Vector3> OnLinesClearedWithPos; // (rows, cols, totalCells, placedCenterPos)
```
2. Thêm hàm helper tính tâm:
```csharp
    public Vector3 CalculateCellsCenterPosition(IReadOnlyList<Vector2Int> cellCoords)
    {
        if (cellCoords == null || cellCoords.Count == 0) return transform.position;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < cellCoords.Count; i++)
        {
            sum += GetCellWorldPosition(cellCoords[i].x, cellCoords[i].y);
        }
        return sum / cellCoords.Count;
    }
```
3. Trong `TryPlaceShape`:
Thu thập danh sách các toạ độ `targetR, targetC` mà khối chiếm:
```csharp
    List<Vector2Int> placedCells = new List<Vector2Int>();
    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            if (shape.HasBlockAt(r, c))
            {
                int targetR = baseCoord.x + r;
                int targetC = baseCoord.y + c;
                placedCells.Add(new Vector2Int(targetR, targetC));
                // ... logic gán occupiedBlocks
            }
        }
    }
    LastPlacedCenterPosition = CalculateCellsCenterPosition(placedCells);
```
4. Trong `CheckAndClearLines`:
Khi có hàng/cột bị phá hủy, bắn thêm sự kiện:
```csharp
    OnLinesCleared?.Invoke(fullRows.Count, fullCols.Count, cellsToClear.Count);
    OnLinesClearedWithPos?.Invoke(fullRows.Count, fullCols.Count, cellsToClear.Count, LastPlacedCenterPosition);
```

- [x] **Step 4: Chạy test để xác nhận test PASS**

Chạy EditMode tests.
Kỳ vọng: Test `BlockGridPlacementCenterTests` PASS 100%.

- [x] **Step 5: Commit task 1**

```bash
git add Assets/_Game/_BlockDrag/BlockGrid.cs Assets/Tests/EditMode/BlockGridPlacementCenterTests.cs
git commit -m "feat(grid): add placed center position calculation and OnLinesClearedWithPos event"
```

---

### Task 2: Cập nhật `ScoreManager` bắn sự kiện `OnComboTriggered` khi combo >= 2

**Files:**
- Modify: `Assets/_Game/_BlockDrag/ScoreManager.cs`
- Modify: `Assets/Tests/EditMode/ScoreSystemTests.cs`

- [x] **Step 1: Viết test kiểm tra sự kiện `OnComboTriggered` trong `ScoreSystemTests`**

Thêm test vào [`Assets/Tests/EditMode/ScoreSystemTests.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/Tests/EditMode/ScoreSystemTests.cs):
```csharp
    [Test]
    public void HandleLinesClearedWithPos_Combo1_DoesNotTriggerOnComboTriggered()
    {
        int comboTriggered = 0;
        scoreManager.OnComboTriggered += (c, pos) => comboTriggered = c;

        scoreManager.HandleLinesClearedWithPos(1, 0, 8, new Vector3(1f, 2f, 0f));

        Assert.AreEqual(0, comboTriggered, "Combo 1 should not trigger visual combo effect");
    }

    [Test]
    public void HandleLinesClearedWithPos_Combo2_TriggersOnComboTriggeredWithPosition()
    {
        int comboTriggered = 0;
        Vector3 triggeredPos = Vector3.zero;
        scoreManager.OnComboTriggered += (c, pos) => {
            comboTriggered = c;
            triggeredPos = pos;
        };

        // Lần 1: kích hoạt combo 1
        scoreManager.HandleLinesClearedWithPos(1, 0, 8, new Vector3(1f, 1f, 0f));
        // Lần 2: trong cửa sổ combo -> lên combo 2
        Vector3 targetPos = new Vector3(3f, 4f, 0f);
        scoreManager.HandleLinesClearedWithPos(1, 0, 8, targetPos);

        Assert.AreEqual(2, comboTriggered);
        Assert.AreEqual(targetPos, triggeredPos);
    }
```

- [x] **Step 2: Chạy test để xác nhận test FAIL**

Chạy EditMode tests.
Kỳ vọng: Lỗi biên dịch vì chưa có `HandleLinesClearedWithPos` và sự kiện `OnComboTriggered`.

- [x] **Step 3: Triển khai trong `ScoreManager.cs`**

Cập nhật [`Assets/_Game/_BlockDrag/ScoreManager.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/ScoreManager.cs):
1. Thêm event:
```csharp
    public event Action<int, Vector3> OnComboTriggered; // (comboCount, worldPos)
```
2. Đăng ký & hủy đăng ký `blockGrid.OnLinesClearedWithPos`:
```csharp
    if (blockGrid != null)
    {
        blockGrid.OnLinesClearedWithPos -= HandleLinesClearedWithPos;
        blockGrid.OnLinesClearedWithPos += HandleLinesClearedWithPos;
    }
```
3. Cập nhật hàm xử lý:
```csharp
    public void HandleLinesClearedWithPos(int rowsCleared, int colsCleared, int totalCellsCleared, Vector3 placedPos)
    {
        HandleLinesCleared(rowsCleared, colsCleared, totalCellsCleared);

        if (currentCombo >= 2)
        {
            OnComboTriggered?.Invoke(currentCombo, placedPos);
        }
    }
```

- [x] **Step 4: Chạy test để xác nhận test PASS**

Chạy EditMode tests.
Kỳ vọng: Mọi test trong `ScoreSystemTests` PASS 100%.

- [x] **Step 5: Commit task 2**

```bash
git add Assets/_Game/_BlockDrag/ScoreManager.cs Assets/Tests/EditMode/ScoreSystemTests.cs
git commit -m "feat(score): add OnComboTriggered event with placed position when combo >= 2"
```

---

### Task 3: Tạo component hoạt họa `ComboTextEffect`

**Files:**
- Create: `Assets/_Game/_BlockDrag/Effects/ComboTextEffect.cs`

- [x] **Step 1: Viết script `ComboTextEffect.cs`**

Tạo file [`Assets/_Game/_BlockDrag/Effects/ComboTextEffect.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/Effects/ComboTextEffect.cs):
```csharp
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Gameplay.BlockDrag.Effects
{
    public class ComboTextEffect : MonoBehaviour
    {
        [Header("UI / Text References")]
        [SerializeField] private TMP_Text comboLabelText;
        [SerializeField] private TMP_Text comboNumberText;
        [SerializeField] private Transform labelContainer;
        [SerializeField] private Transform numberContainer;

        [Header("Animation Settings")]
        [SerializeField] private float labelScaleDuration = 0.25f;
        [SerializeField] private float numberScaleDuration = 0.25f;
        [SerializeField] private float stayDuration = 0.5f;
        [SerializeField] private float floatUpDistance = 0.8f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private Sequence animSequence;
        private Vector3 initialPosition;

        private void Awake()
        {
            if (labelContainer == null && comboLabelText != null)
                labelContainer = comboLabelText.transform;
            if (numberContainer == null && comboNumberText != null)
                numberContainer = comboNumberText.transform;
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        public void KillSequence()
        {
            if (animSequence != null && animSequence.IsActive())
            {
                animSequence.Kill();
                animSequence = null;
            }
        }

        /// <summary>
        /// Kích hoạt chuỗi hoạt họa Combo:
        /// 1. "COMBO" scale từ 0 lên 1 trước (Ease.OutBack)
        /// 2. Số combo (vd "x2") scale nảy từ 0 lên 1.2 -> 1.0 (Ease.OutBack)
        /// 3. Dừng 0.5s
        /// 4. Bay nhẹ lên trên và Fade Out biến mất
        /// </summary>
        public void Play(int comboCount, Vector3 spawnPosition, Action onComplete = null)
        {
            KillSequence();

            transform.position = spawnPosition;
            initialPosition = spawnPosition;
            gameObject.SetActive(true);

            // Cập nhật text
            if (comboLabelText != null)
            {
                comboLabelText.text = "COMBO";
                comboLabelText.alpha = 1f;
            }
            if (comboNumberText != null)
            {
                comboNumberText.text = $"x{comboCount}";
                comboNumberText.alpha = 1f;
            }

            // Đặt scale ban đầu về 0
            if (labelContainer != null) labelContainer.localScale = Vector3.zero;
            if (numberContainer != null) numberContainer.localScale = Vector3.zero;

            animSequence = DOTween.Sequence();

            // Bước 1: "COMBO" xuất hiện trước
            if (labelContainer != null)
            {
                animSequence.Append(labelContainer.DOScale(Vector3.one, labelScaleDuration).SetEase(Ease.OutBack));
            }

            // Bước 2: Số combo xuất hiện ngay sau đó
            if (numberContainer != null)
            {
                animSequence.Append(numberContainer.DOScale(Vector3.one * 1.2f, numberScaleDuration * 0.7f).SetEase(Ease.OutBack));
                animSequence.Append(numberContainer.DOScale(Vector3.one, numberScaleDuration * 0.3f).SetEase(Ease.Linear));
            }

            // Bước 3: Dừng hiển thị
            animSequence.AppendInterval(stayDuration);

            // Bước 4: Trôi nhẹ lên trên và Fade Out
            float targetY = initialPosition.y + floatUpDistance;
            animSequence.Append(transform.DOMoveY(targetY, fadeOutDuration).SetEase(Ease.OutQuad));

            if (comboLabelText != null)
            {
                animSequence.Join(comboLabelText.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
            }
            if (comboNumberText != null)
            {
                animSequence.Join(comboNumberText.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
            }

            // Bước 5: Hoàn tất
            animSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
    }
}
```

- [x] **Step 2: Commit task 3**

```bash
git add Assets/_Game/_BlockDrag/Effects/ComboTextEffect.cs
git commit -m "feat(effects): add ComboTextEffect sequential scale and fade animation"
```

---

### Task 4: Tạo `ComboEffectManager` và quản lý Object Pool

**Files:**
- Create: `Assets/_Game/_BlockDrag/Effects/ComboEffectManager.cs`

- [x] **Step 1: Viết script `ComboEffectManager.cs`**

Tạo file [`Assets/_Game/_BlockDrag/Effects/ComboEffectManager.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/Effects/ComboEffectManager.cs):
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag.Effects
{
    public class ComboEffectManager : MonoBehaviour
    {
        public static ComboEffectManager Ins { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ComboTextEffect effectPrefab;
        [SerializeField] private int initialPoolSize = 4;
        [SerializeField] private Transform poolContainer;

        private readonly List<ComboTextEffect> pool = new List<ComboTextEffect>();

        private void Awake()
        {
            if (Ins != null && Ins != this)
            {
                Destroy(gameObject);
                return;
            }
            Ins = this;

            if (poolContainer == null)
            {
                poolContainer = transform;
            }

            InitPool();
        }

        private void OnEnable()
        {
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnComboTriggered -= HandleComboTriggered;
                ScoreManager.Ins.OnComboTriggered += HandleComboTriggered;
            }
        }

        private void OnDisable()
        {
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnComboTriggered -= HandleComboTriggered;
            }
        }

        private void Start()
        {
            // Dự phòng nếu lúc Awake ScoreManager chưa khởi tạo xong
            if (ScoreManager.Ins != null)
            {
                ScoreManager.Ins.OnComboTriggered -= HandleComboTriggered;
                ScoreManager.Ins.OnComboTriggered += HandleComboTriggered;
            }
        }

        private void InitPool()
        {
            if (effectPrefab == null) return;

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewInstance();
            }
        }

        private ComboTextEffect CreateNewInstance()
        {
            if (effectPrefab == null) return null;

            ComboTextEffect instance = Instantiate(effectPrefab, poolContainer);
            instance.gameObject.SetActive(false);
            pool.Add(instance);
            return instance;
        }

        private ComboTextEffect GetFromPool()
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
            ComboTextEffect effect = GetFromPool();
            if (effect != null)
            {
                effect.Play(comboCount, spawnPosition);
            }
        }
    }
}
```

- [x] **Step 2: Commit task 4**

```bash
git add Assets/_Game/_BlockDrag/Effects/ComboEffectManager.cs
git commit -m "feat(effects): add ComboEffectManager with object pooling"
```

---

### Task 5: Tạo Prefab `ComboTextEffect` và tích hợp vào Scene

**Files:**
- Create: `Assets/_Game/Resources/Prefabs/ComboTextEffect.prefab` (hoặc tạo programmatic / prefab asset)
- Modify: `Assets/Scenes/SampleScene.unity` hoặc bootstrap GameObject trong Scene

- [x] **Step 1: Tạo script Editor tự động build Prefab `ComboTextEffect` với TextMeshPro styling đẹp mắt**

Tạo file [`Assets/_Game/_BlockDrag/Editor/ComboEffectPrefabCreator.cs`](file:///Users/minhquang/Documents/Unity%20Project/BlockDrag/Assets/_Game/_BlockDrag/Editor/ComboEffectPrefabCreator.cs):
Tự động tạo Prefab `ComboTextEffect.prefab` với:
- Root GameObject chứa `ComboTextEffect`.
- Hai sub GameObject: `ComboLabel` (Text: `"COMBO"`, font asset có sẵn, màu vàng cam rực rỡ, outline đậm, size 6, sortingOrder 50) và `ComboNumber` (Text: `"x2"`, màu trắng viền vàng, size 7, sortingOrder 50).
- Căn lề ngang hợp lý.
- Gắn vào `ComboEffectManager` trong `SampleScene`.

- [x] **Step 2: Chạy kiểm tra PlayMode và xác nhận hiệu ứng hoạt động hoàn hảo**

- [x] **Step 3: Commit task 5**

```bash
git add Assets/_Game/Resources/Prefabs/ComboTextEffect.prefab Assets/Scenes/SampleScene.unity
git commit -m "feat: setup ComboTextEffect prefab and integrate into SampleScene"
```
