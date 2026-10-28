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
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        private readonly List<Object> createdObjects = new List<Object>();

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(4, 4);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            createdObjects.Add(texture);
            createdObjects.Add(sprite);
            return sprite;
        }

        private void SetSprites(params Sprite[] sprites)
        {
            var so = new SerializedObject(palette);
            SerializedProperty list = so.FindProperty("sprites");
            list.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("sprite").objectReferenceValue = sprites[i];
                entry.FindPropertyRelative("color").colorValue = Color.white;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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

        [Test]
        public void TryGetRandomSkin_WhenBothEmpty_ReturnsFalse()
        {
            CreatePalette();

            Assert.IsFalse(palette.TryGetRandomSkin(null, out _));
        }

        [Test]
        public void TryGetRandomSkin_WhenSpritesEmpty_FallsBackToColors()
        {
            CreatePalette(Color.red, Color.green);

            Assert.IsFalse(palette.UseSprites);
            Assert.IsTrue(palette.TryGetRandomSkin(null, out BlockSkin skin));
            Assert.IsFalse(skin.HasSprite);
            CollectionAssert.Contains(new[] { Color.red, Color.green }, skin.color);
        }

        [Test]
        public void TryGetRandomSkin_WhenHasSprites_ReturnsSpriteSkin()
        {
            CreatePalette(Color.red);
            Sprite a = CreateSprite();
            Sprite b = CreateSprite();
            SetSprites(a, b);

            Assert.IsTrue(palette.UseSprites);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(palette.TryGetRandomSkin(null, out BlockSkin skin));
                Assert.IsTrue(skin.sprite == a || skin.sprite == b);
            }
        }

        [Test]
        public void TryGetRandomSkin_WhenEntryMissingSprite_SkipsIt()
        {
            CreatePalette();
            Sprite a = CreateSprite();
            SetSprites(null, a);

            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(palette.TryGetRandomSkin(null, out BlockSkin skin));
                Assert.AreEqual(a, skin.sprite);
            }
        }

        [Test]
        public void TryGetRandomSkin_WithExclude_ReturnsUnusedSprite()
        {
            CreatePalette();
            Sprite a = CreateSprite();
            Sprite b = CreateSprite();
            Sprite c = CreateSprite();
            SetSprites(a, b, c);
            var exclude = new List<BlockSkin> { new BlockSkin { sprite = a }, new BlockSkin { sprite = c } };

            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(palette.TryGetRandomSkin(exclude, out BlockSkin skin));
                Assert.AreEqual(b, skin.sprite);
            }
        }

        [Test]
        public void TryGetRandomSkin_WhenAllExcluded_ReturnsSpriteFromList()
        {
            CreatePalette();
            Sprite a = CreateSprite();
            Sprite b = CreateSprite();
            SetSprites(a, b);
            var exclude = new List<BlockSkin> { new BlockSkin { sprite = a }, new BlockSkin { sprite = b } };

            Assert.IsTrue(palette.TryGetRandomSkin(exclude, out BlockSkin skin));
            Assert.IsTrue(skin.sprite == a || skin.sprite == b);
            Assert.AreEqual(1f, skin.color.a);
        }
    }
}
