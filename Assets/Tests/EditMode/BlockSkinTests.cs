using Gameplay.BlockDrag;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class BlockSkinTests
    {
        private Texture2D texture;
        private Sprite spriteA;
        private Sprite spriteB;

        [SetUp]
        public void SetUp()
        {
            texture = new Texture2D(4, 4);
            spriteA = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            spriteB = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(spriteA);
            Object.DestroyImmediate(spriteB);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void HasSprite_WhenSpriteNull_ReturnsFalse()
        {
            Assert.IsFalse(BlockSkin.FromColor(Color.red).HasSprite);
            Assert.IsTrue(new BlockSkin { sprite = spriteA }.HasSprite);
        }

        [Test]
        public void SameAs_WithSameSprite_ReturnsTrue()
        {
            var a = new BlockSkin { sprite = spriteA, color = Color.red };
            var b = new BlockSkin { sprite = spriteA, color = Color.blue };

            Assert.IsTrue(a.SameAs(b));
        }

        [Test]
        public void SameAs_WithDifferentSprite_ReturnsFalse()
        {
            var a = new BlockSkin { sprite = spriteA, color = Color.red };
            var b = new BlockSkin { sprite = spriteB, color = Color.red };

            Assert.IsFalse(a.SameAs(b));
            Assert.IsFalse(a.SameAs(BlockSkin.FromColor(Color.red)));
        }

        [Test]
        public void SameAs_ColorMode_ComparesRgbOnly()
        {
            var opaque = BlockSkin.FromColor(new Color(1f, 0f, 0f, 1f));
            var transparent = BlockSkin.FromColor(new Color(1f, 0f, 0f, 0f));

            Assert.IsTrue(opaque.SameAs(transparent));
            Assert.IsFalse(opaque.SameAs(BlockSkin.FromColor(Color.green)));
        }
    }
}
