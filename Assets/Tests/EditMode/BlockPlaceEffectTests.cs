using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class BlockPlaceEffectTests
    {
        [Test]
        public void CollectPerimeterEdges_SingleCell_ReturnsFourEdges()
        {
            var edges = BlockPlaceEffect.CollectPerimeterEdges(new[] { new Vector2Int(3, 3) });

            Assert.AreEqual(4, edges.Count);
        }

        [Test]
        public void CollectPerimeterEdges_SShape_ReturnsTenEdges()
        {
            // .XX
            // XX.
            var cells = new[]
            {
                new Vector2Int(0, 1), new Vector2Int(0, 2),
                new Vector2Int(1, 0), new Vector2Int(1, 1)
            };

            var edges = BlockPlaceEffect.CollectPerimeterEdges(cells);

            Assert.AreEqual(10, edges.Count);
        }

        [Test]
        public void CollectPerimeterEdges_Square2x2_ExcludesInnerEdges()
        {
            var cells = new[]
            {
                new Vector2Int(0, 0), new Vector2Int(0, 1),
                new Vector2Int(1, 0), new Vector2Int(1, 1)
            };

            var edges = BlockPlaceEffect.CollectPerimeterEdges(cells);

            Assert.AreEqual(8, edges.Count);
            foreach (var edge in edges)
            {
                Vector2Int neighbor = edge.Cell + edge.GridDirection;
                Assert.IsFalse(cells.Contains(neighbor), $"Cạnh trong bị tính là biên: {edge.Cell} -> {neighbor}");
            }
        }

        [Test]
        public void CollectPerimeterEdges_EdgeNormalUp_PointsToPreviousRow()
        {
            var edges = BlockPlaceEffect.CollectPerimeterEdges(new[] { new Vector2Int(2, 2) });

            var upEdge = edges.Single(e => e.GridDirection == new Vector2Int(-1, 0));
            var rightEdge = edges.Single(e => e.GridDirection == new Vector2Int(0, 1));

            Assert.AreEqual(Vector2.up, upEdge.LocalNormal);
            Assert.AreEqual(Vector2.right, rightEdge.LocalNormal);
        }

        [Test]
        public void CollectPerimeterEdges_EmptyOrNull_ReturnsEmpty()
        {
            Assert.AreEqual(0, BlockPlaceEffect.CollectPerimeterEdges(null).Count);
            Assert.AreEqual(0, BlockPlaceEffect.CollectPerimeterEdges(new List<Vector2Int>()).Count);
        }

        [Test]
        public void CalculateParticleCount_ClampsToMinAndMax()
        {
            Assert.AreEqual(6, BlockPlaceEffect.CalculateParticleCount(2, 1.3f, 6, 36));
            Assert.AreEqual(13, BlockPlaceEffect.CalculateParticleCount(10, 1.3f, 6, 36));
            Assert.AreEqual(36, BlockPlaceEffect.CalculateParticleCount(100, 1.3f, 6, 36));
            Assert.AreEqual(0, BlockPlaceEffect.CalculateParticleCount(0, 1.3f, 6, 36));
        }
    }
}
