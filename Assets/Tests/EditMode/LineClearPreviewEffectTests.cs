using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class LineClearPreviewEffectTests
    {
        private const string PrefabPath = "Assets/_Game/_BlockDrag/Prefabs/LineClearPreviewEffect.prefab";
        private const string GlowSpritePath = "Assets/_Game/_BlockDrag/Effects/Textures/LineClearGlow.png";
        private const string RainbowShaderName = "BlockDrag/LineClearRainbowGlow";
        private const float CellSize = 1f;
        private const float Spacing = 0.1f;
        private const float Padding = 0.3f;
        private const int Rows = 8;
        private const int Cols = 8;
        private static readonly float GridSize = Cols * CellSize + (Cols - 1) * Spacing;

        [Test]
        public void GetLineLocalRect_Row_SpansFullGridWidthPlusPadding()
        {
            Rect rect = LineClearPreviewEffect.GetLineLocalRect(true, 3, Rows, Cols, CellSize, Spacing, Padding);

            Assert.AreEqual(GridSize + Padding * 2f, rect.width, 1e-4f);
            Assert.AreEqual(CellSize + Padding * 2f, rect.height, 1e-4f);
            Assert.AreEqual(0f, rect.center.x, 1e-4f);
        }

        [Test]
        public void GetLineLocalRect_Column_CenteredOnColumn()
        {
            Rect rect = LineClearPreviewEffect.GetLineLocalRect(false, 2, Rows, Cols, CellSize, Spacing, Padding);

            float expectedCenterX = -GridSize * 0.5f + CellSize * 0.5f + 2 * (CellSize + Spacing);
            Assert.AreEqual(expectedCenterX, rect.center.x, 1e-4f);
            Assert.AreEqual(0f, rect.center.y, 1e-4f);
            Assert.AreEqual(GridSize + Padding * 2f, rect.height, 1e-4f);
        }

        [Test]
        public void GetLineLocalRect_TopRow_HasHighestY()
        {
            Rect top = LineClearPreviewEffect.GetLineLocalRect(true, 0, Rows, Cols, CellSize, Spacing, Padding);
            Rect bottom = LineClearPreviewEffect.GetLineLocalRect(true, Rows - 1, Rows, Cols, CellSize, Spacing, Padding);

            Assert.Greater(top.center.y, bottom.center.y);
            Assert.AreEqual(GridSize * 0.5f + Padding, top.yMax, 1e-4f);
        }

        [Test]
        public void LineClearGlowSprite_HasNineSliceBorder()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);

            Assert.IsNotNull(sprite, "Thiếu sprite " + GlowSpritePath);
            Assert.AreEqual(new Vector4(24f, 24f, 24f, 24f), sprite.border);
            Assert.AreEqual(64f, sprite.pixelsPerUnit, 1e-4f);
        }

        [Test]
        public void LineClearPreviewPrefab_HasSparklesAndGlowTemplateAssigned()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Thiếu prefab " + PrefabPath);

            var effect = prefab.GetComponent<LineClearPreviewEffect>();
            Assert.IsNotNull(effect);
            Assert.IsNotNull(effect.Sparkles);
            Assert.IsNotNull(effect.GlowTemplate);
            Assert.IsFalse(effect.GlowTemplate.gameObject.activeSelf, "Glow Template phải tắt sẵn");
            Assert.AreEqual(SpriteDrawMode.Sliced, effect.GlowTemplate.drawMode);
            Assert.IsNotNull(effect.GlowTemplate.sprite);
        }

        [Test]
        public void IsMultiLine_SingleRow_ReturnsFalse()
        {
            Assert.IsFalse(LineClearPreviewEffect.IsMultiLine(CreateResult(new[] { 0 }, new int[0]), 2));
        }

        [Test]
        public void IsMultiLine_TwoRows_ReturnsTrue()
        {
            Assert.IsTrue(LineClearPreviewEffect.IsMultiLine(CreateResult(new[] { 0, 1 }, new int[0]), 2));
        }

        [Test]
        public void IsMultiLine_OneRowOneColumn_ReturnsTrue()
        {
            Assert.IsTrue(LineClearPreviewEffect.IsMultiLine(CreateResult(new[] { 3 }, new[] { 5 }), 2));
        }

        [Test]
        public void IsMultiLine_ThresholdThree_TwoLines_ReturnsFalse()
        {
            Assert.IsFalse(LineClearPreviewEffect.IsMultiLine(CreateResult(new[] { 0 }, new[] { 1 }), 3));
        }

        [Test]
        public void BakeGradientPixels_EndsMatchGradientEndKeys()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.blue, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            Color32[] pixels = LineClearPreviewEffect.BakeGradientPixels(gradient, 16);

            Assert.AreEqual(16, pixels.Length);
            Assert.AreEqual((Color32)Color.red, pixels[0]);
            Assert.AreEqual((Color32)Color.blue, pixels[15]);
        }

        [Test]
        public void LineClearPreviewPrefab_HasRainbowMaterialWithRainbowShader()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var effect = prefab.GetComponent<LineClearPreviewEffect>();

            Assert.IsNotNull(effect.RainbowGlowMaterial, "Chưa gán Rainbow Glow Material trong prefab");
            Assert.AreEqual(RainbowShaderName, effect.RainbowGlowMaterial.shader.name);
        }

        [Test]
        public void LineClearRainbowGlowShader_IsSupported()
        {
            Shader shader = Shader.Find(RainbowShaderName);

            Assert.IsNotNull(shader, "Không tìm thấy shader " + RainbowShaderName);
            Assert.IsTrue(shader.isSupported);
        }

        private static LineClearResult CreateResult(int[] rows, int[] columns)
        {
            return new LineClearResult(rows, columns, new List<Vector2Int>());
        }
    }
}
