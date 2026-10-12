using System.Collections.Generic;
using Gameplay.BlockDrag;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public class BlockColorPaletteTests
    {
        private BlockColorPaletteSO palette;

        [TearDown]
        public void TearDown()
        {
            if (palette != null)
            {
                Object.DestroyImmediate(palette);
            }
        }

        private BlockColorPaletteSO CreatePalette(params Color[] colors)
        {
            palette = ScriptableObject.CreateInstance<BlockColorPaletteSO>();
            var so = new SerializedObject(palette);
            SerializedProperty list = so.FindProperty("colors");
            list.arraySize = colors.Length;
            for (int i = 0; i < colors.Length; i++)
            {
                list.GetArrayElementAtIndex(i).colorValue = colors[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return palette;
        }

        [Test]
        public void TryGetRandomColor_WhenEmpty_ReturnsFalse()
        {
            CreatePalette();

            Assert.IsFalse(palette.TryGetRandomColor(null, out _));
        }

        [Test]
        public void TryGetRandomColor_WhenHasColors_ReturnsColorFromList()
        {
            CreatePalette(Color.red, Color.green, Color.blue);

            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(palette.TryGetRandomColor(null, out Color color));
                CollectionAssert.Contains(new[] { Color.red, Color.green, Color.blue }, color);
            }
        }

        [Test]
        public void TryGetRandomColor_WhenColorHasZeroAlpha_ReturnsOpaqueColor()
        {
            CreatePalette(new Color(1f, 0f, 0f, 0f));

            Assert.IsTrue(palette.TryGetRandomColor(null, out Color color));
            Assert.AreEqual(1f, color.a);
            Assert.AreEqual(1f, color.r);
        }

        [Test]
        public void TryGetRandomColor_WithExclude_ReturnsUnusedColor()
        {
            CreatePalette(Color.red, Color.green, Color.blue);
            var exclude = new List<Color> { Color.red, Color.blue };

            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(palette.TryGetRandomColor(exclude, out Color color));
                Assert.AreEqual(Color.green, color);
            }
        }

        [Test]
        public void TryGetRandomColor_WhenAllExcluded_ReturnsColorFromList()
        {
            CreatePalette(Color.red, Color.green);
            var exclude = new List<Color> { Color.red, Color.green };

            Assert.IsTrue(palette.TryGetRandomColor(exclude, out Color color));
            CollectionAssert.Contains(new[] { Color.red, Color.green }, color);
        }
    }
}
