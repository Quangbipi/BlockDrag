using NUnit.Framework;
using UnityEngine;
using TMPro;
using Gameplay.BlockDrag;
using UI;

namespace Tests.EditMode
{
    public class ComboEffectTests
    {
        private GameObject managerGo;
        private ComboEffectManager effectManager;
        private GameObject prefabGo;
        private ComboTextEffect effectPrefab;

        [SetUp]
        public void SetUp()
        {
            // Tạo Dummy UI Prefab
            prefabGo = new GameObject("ComboTextEffect_Prefab", typeof(RectTransform), typeof(CanvasGroup));
            effectPrefab = prefabGo.AddComponent<ComboTextEffect>();

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(prefabGo.transform);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();

            GameObject numberGo = new GameObject("Number", typeof(RectTransform));
            numberGo.transform.SetParent(prefabGo.transform);
            var numberTmp = numberGo.AddComponent<TextMeshProUGUI>();

            effectPrefab.InitReferences(labelTmp, numberTmp, labelGo.transform, numberGo.transform, prefabGo.GetComponent<CanvasGroup>());

            // Tạo Manager
            managerGo = new GameObject("ComboEffectManager_Test", typeof(RectTransform));
            effectManager = managerGo.AddComponent<ComboEffectManager>();
            effectManager.EffectPrefab = effectPrefab;
            effectManager.InitPool();
        }

        [TearDown]
        public void TearDown()
        {
            if (managerGo != null) Object.DestroyImmediate(managerGo);
            if (prefabGo != null) Object.DestroyImmediate(prefabGo);
        }

        [Test]
        public void ComboEffectManager_InitPool_CreatesSpecifiedInstances()
        {
            Assert.AreEqual(4, effectManager.Pool.Count);
            foreach (var item in effectManager.Pool)
            {
                Assert.IsFalse(item.gameObject.activeSelf);
            }
        }

        [Test]
        public void ComboEffectManager_GetFromPool_ReusesInactiveInstances()
        {
            var item1 = effectManager.GetFromPool();
            Assert.IsNotNull(item1);
            item1.gameObject.SetActive(true);

            var item2 = effectManager.GetFromPool();
            Assert.IsNotNull(item2);
            Assert.AreNotSame(item1, item2);

            item1.gameObject.SetActive(false);
            var item3 = effectManager.GetFromPool();
            Assert.AreSame(item1, item3);
        }

        [Test]
        public void ComboEffectManager_GetFromPool_ExpandsPoolWhenFull()
        {
            for (int i = 0; i < 4; i++)
            {
                var item = effectManager.GetFromPool();
                item.gameObject.SetActive(true);
            }

            Assert.AreEqual(4, effectManager.Pool.Count);

            var extra = effectManager.GetFromPool();
            Assert.IsNotNull(extra);
            Assert.AreEqual(5, effectManager.Pool.Count);
        }

        [Test]
        public void ComboTextEffect_Play_SetsTextValuesCorrectly()
        {
            var item = effectManager.GetFromPool();
            item.Play(3, new Vector3(5f, 6f, 0f));
            Assert.AreEqual("COMBO", item.ComboLabelText.text);
            Assert.AreEqual("x3", item.ComboNumberText.text);
            Assert.IsTrue(item.gameObject.activeSelf);
        }

        [Test]
        public void ComboTextEffect_ClampToParentBounds_RestrictsExtremesToSafeMargins()
        {
            var item = effectManager.GetFromPool();

            // Tạo parent RectTransform giả lập Canvas kích thước 1080 x 1920 (pivot 0.5, 0.5)
            // xMin = -540, xMax = 540, yMin = -960, yMax = 960
            GameObject parentGo = new GameObject("ParentCanvasRect", typeof(RectTransform));
            RectTransform parentRt = parentGo.GetComponent<RectTransform>();
            parentRt.sizeDelta = new Vector2(1080f, 1920f);

            // Item width = 400 (half = 200), paddingX = 30 -> minX = -540 + 200 + 30 = -310, maxX = 310
            // Item height = 100 (half = 50), bottomPadding = 40 -> minY = -960 + 50 + 40 = -870
            // Item topPadding = 120, floatUp = 75 -> maxY = 960 - 50 - 75 - 120 = 715

            // Test 1: Vị trí cực trái (ví dụ x = -800) -> phải được clamp về -310
            Vector2 leftExtreme = new Vector2(-800f, 0f);
            Vector2 clampedLeft = item.ClampToParentBounds(parentRt, leftExtreme);
            Assert.AreEqual(-310f, clampedLeft.x, 0.01f);
            Assert.AreEqual(0f, clampedLeft.y, 0.01f);

            // Test 2: Vị trí cực phải (ví dụ x = 800) -> phải được clamp về 310
            Vector2 rightExtreme = new Vector2(800f, 0f);
            Vector2 clampedRight = item.ClampToParentBounds(parentRt, rightExtreme);
            Assert.AreEqual(310f, clampedRight.x, 0.01f);
            Assert.AreEqual(0f, clampedRight.y, 0.01f);

            // Test 3: Vị trí cực trên (ví dụ y = 900) -> phải được clamp về 715
            Vector2 topExtreme = new Vector2(0f, 900f);
            Vector2 clampedTop = item.ClampToParentBounds(parentRt, topExtreme);
            Assert.AreEqual(0f, clampedTop.x, 0.01f);
            Assert.AreEqual(715f, clampedTop.y, 0.01f);

            // Test 4: Vị trí an toàn ở giữa (ví dụ (50, 100)) -> giữ nguyên
            Vector2 safePos = new Vector2(50f, 100f);
            Vector2 clampedSafe = item.ClampToParentBounds(parentRt, safePos);
            Assert.AreEqual(50f, clampedSafe.x, 0.01f);
            Assert.AreEqual(100f, clampedSafe.y, 0.01f);

            Object.DestroyImmediate(parentGo);
        }
    }
}
