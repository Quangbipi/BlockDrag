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
    }
}
