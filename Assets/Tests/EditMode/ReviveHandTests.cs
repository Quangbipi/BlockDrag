using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class ReviveHandTests
    {
        private readonly List<BlockShapeData> created = new List<BlockShapeData>();
        private List<BlockShapeData> pool;

        [SetUp]
        public void SetUp()
        {
            pool = new List<BlockShapeData>
            {
                Shape("1"),
                Shape("1", "1"),
                Shape("1", "1", "1"),
                Shape("1", "1", "1", "1"),
                Shape("1", "1", "1", "1", "1"),
                Shape("1,1", "1,1"),
                Shape("1,1,1", "1,1,1", "1,1,1"),
                Shape("1,0", "1,0", "1,1"),
                Shape("1,1,1", "1,0,0", "1,0,0"),
                Shape("1,1,1", "0,1,0"),
                Shape("0,1,1", "1,1,0"),
                Shape("1,1,0", "0,1,1"),
                Shape("1,1,1", "1,1,1"),
                Shape("0,1", "1,0"),
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (BlockShapeData data in created)
            {
                Object.DestroyImmediate(data);
            }
            created.Clear();
        }

        [Test]
        public void GenerateRevive_OnNearlyFullBoard_HandContainsBlockThatCompletesALine()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, SpawnSettings.CreateDefault(), null, new System.Random(seed));
                BlockBoard board = NearlyFullBoard(seed);

                List<ShapeVariant> hand = generator.GenerateRevive(board, 3);

                Assert.IsTrue(hand.Any(v => CompletesLineSomewhere(board, v)), $"seed {seed}");
            }
        }

        [Test]
        public void GenerateRevive_ReturnsThreeBlocksPlaceableInSequence()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, SpawnSettings.CreateDefault(), null, new System.Random(seed));
                BlockBoard board = NearlyFullBoard(seed);

                List<ShapeVariant> hand = generator.GenerateRevive(board, 3);

                Assert.AreEqual(3, hand.Count, $"seed {seed}");
                Assert.IsTrue(HandSolver.IsSolvable(board, hand.Cast<IShapeFootprint>().ToList(), 200000), $"seed {seed}");
            }
        }

        [Test]
        public void GenerateRevive_WhenNoLineCanComplete_FallsBackToAssistHand()
        {
            // Khối dài nhất 3 ô trên bàn 8x8 trống: không khối nào hoàn thành được hàng/cột
            List<BlockShapeData> shortPool = new List<BlockShapeData>
            {
                Shape("1,1", "1,1"),
                Shape("1", "1", "1"),
                Shape("1,0", "1,0", "1,1"),
            };
            HandGenerator generator = new HandGenerator(shortPool, SpawnSettings.CreateDefault(), null, new System.Random(1));

            LogAssert.Expect(LogType.Warning, new Regex("Không tìm được khối hồi sinh"));
            List<ShapeVariant> hand = generator.GenerateRevive(new BlockBoard(8, 8), 3);

            Assert.AreEqual(3, hand.Count);
        }

        /// <summary>
        /// Bàn 8x8 đầy ~75–90%, không có hàng/cột đầy sẵn (khoét 1 ô ngẫu nhiên thay vì xoá cả hàng), mô phỏng lúc hết nước đi
        /// </summary>
        private static BlockBoard NearlyFullBoard(int seed)
        {
            System.Random rng = new System.Random(seed * 7919 + 17);
            float fill = 0.75f + (float)rng.NextDouble() * 0.15f;
            BlockBoard board = new BlockBoard(8, 8);
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    board.SetOccupied(r, c, rng.NextDouble() < fill);
                }
            }

            for (int r = 0; r < 8; r++)
            {
                if (IsRowFull(board, r)) board.SetOccupied(r, rng.Next(8), false);
            }
            for (int c = 0; c < 8; c++)
            {
                if (IsColumnFull(board, c)) board.SetOccupied(rng.Next(8), c, false);
            }
            return board;
        }

        private static bool IsRowFull(BlockBoard board, int r)
        {
            for (int c = 0; c < board.Columns; c++)
            {
                if (!board.IsOccupied(r, c)) return false;
            }
            return true;
        }

        private static bool IsColumnFull(BlockBoard board, int c)
        {
            for (int r = 0; r < board.Rows; r++)
            {
                if (!board.IsOccupied(r, c)) return false;
            }
            return true;
        }

        private static bool CompletesLineSomewhere(BlockBoard board, ShapeVariant variant)
        {
            for (int r = 0; r <= board.Rows - variant.RowsCount; r++)
            {
                for (int c = 0; c <= board.Columns - variant.ColumnsCount; c++)
                {
                    if (board.FindLinesCompletedBy(variant, new Vector2Int(r, c)).HasLines) return true;
                }
            }
            return false;
        }

        private BlockShapeData Shape(params string[] rows)
        {
            BlockShapeData data = ScriptableObject.CreateInstance<BlockShapeData>();
            data.rows = rows;
            created.Add(data);
            return data;
        }
    }
}
