using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class AssistHandBuilderTests
    {
        private readonly List<BlockShapeData> created = new List<BlockShapeData>();

        private BlockShapeData single, i2, i3, i4, square, l, t, rec;
        private List<ShapeFamily> families;

        [SetUp]
        public void SetUp()
        {
            single = Shape("1");
            i2 = Shape("1", "1");
            i3 = Shape("1", "1", "1");
            i4 = Shape("1", "1", "1", "1");
            square = Shape("1,1", "1,1");
            l = Shape("1,0", "1,0", "1,1");
            t = Shape("1,1,1", "0,1,0");
            rec = Shape("1,1,1", "1,1,1");

            families = Families(single, i2, i3, i4, square, l, t, rec);
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
        public void PlacementScore_LineClear_ScoresHigherThanNoClear()
        {
            BlockBoard board = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) board.SetOccupied(7, c, true);
            board.SetOccupied(0, 0, true);
            AssistHandBuilder builder = new AssistHandBuilder(families, SpawnSettings.CreateDefault(), new System.Random(0));
            ShapeVariant horizontalI3 = new ShapeVariant(i3, BlockRotation.Rot_90);

            float clearing = builder.PlacementScore(board, horizontalI3, new Vector2Int(7, 0));
            float notClearing = builder.PlacementScore(board, horizontalI3, new Vector2Int(3, 3));

            Assert.Greater(clearing, notClearing + 50f);
        }

        [Test]
        public void PlacementScore_PerfectClear_ScoresHighest()
        {
            SpawnSettings settings = SpawnSettings.CreateDefault();
            AssistHandBuilder builder = new AssistHandBuilder(families, settings, new System.Random(0));
            ShapeVariant horizontalI3 = new ShapeVariant(i3, BlockRotation.Rot_90);

            BlockBoard onlyOneRow = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) onlyOneRow.SetOccupied(7, c, true);

            BlockBoard rowPlusLeftover = new BlockBoard(8, 8);
            rowPlusLeftover.CopyFrom(onlyOneRow);
            rowPlusLeftover.SetOccupied(0, 7, true);

            float perfect = builder.PlacementScore(onlyOneRow, horizontalI3, new Vector2Int(7, 0));
            float normalClear = builder.PlacementScore(rowPlusLeftover, horizontalI3, new Vector2Int(7, 0));

            Assert.GreaterOrEqual(perfect, settings.perfectClearScore);
            Assert.Greater(perfect, normalClear + settings.perfectClearScore * 0.5f);
        }

        [Test]
        public void Build_RowMissingThreeCells_ChainContainsClearingPiece()
        {
            BlockBoard board = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) board.SetOccupied(7, c, true);

            for (int seed = 0; seed < 50; seed++)
            {
                AssistHandBuilder builder = new AssistHandBuilder(families, SpawnSettings.CreateDefault(), new System.Random(seed));

                List<ShapeVariant> chain = builder.Build(board, 3, 0f, null);

                Assert.AreEqual(3, chain.Count, $"seed {seed}");
                Assert.IsTrue(HandSolver.CanClearAnyLine(board, chain[0]), $"seed {seed}");
            }
        }

        [Test]
        public void Build_TwoSeparateGaps_ChainClearsBoth()
        {
            // Hàng 7 thiếu 3 ô đầu, hàng 0 thiếu 2 ô cuối: không khối nào lấp cả hai cùng lúc
            BlockBoard board = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) board.SetOccupied(7, c, true);
            for (int c = 0; c < 6; c++) board.SetOccupied(0, c, true);

            for (int seed = 0; seed < 50; seed++)
            {
                AssistHandBuilder builder = new AssistHandBuilder(families, SpawnSettings.CreateDefault(), new System.Random(seed));

                List<ShapeVariant> chain = builder.Build(board, 3, 0f, null);

                Assert.GreaterOrEqual(ReplayMaxLines(board, chain), 2, $"seed {seed}");
            }
        }

        [Test]
        public void Build_ManySeeds_NeverDuplicatesFamily_AndAlwaysSolvable()
        {
            for (int seed = 0; seed < 300; seed++)
            {
                AssistHandBuilder builder = new AssistHandBuilder(families, SpawnSettings.CreateDefault(), new System.Random(seed));
                BlockBoard board = RandomBoard(seed, 0.2f, 0.5f);

                List<ShapeVariant> chain = builder.Build(board, 3, (seed % 10) / 9f, null);

                Assert.AreEqual(chain.Count, chain.Select(v => v.FamilyKey).Distinct().Count(), $"seed {seed}");
                Assert.IsTrue(HandSolver.IsSolvable(board, chain.Cast<IShapeFootprint>().ToList(), 200000), $"seed {seed}");
            }
        }

        [Test]
        public void Build_EmptyBoard_ReturnsCountShapes()
        {
            AssistHandBuilder builder = new AssistHandBuilder(families, SpawnSettings.CreateDefault(), new System.Random(3));

            Assert.AreEqual(3, builder.Build(new BlockBoard(8, 8), 3, 0f, null).Count);
        }

        [Test]
        public void Build_ExcludedFamilies_AreNeverUsed()
        {
            HashSet<string> excluded = new HashSet<string>
            {
                ShapeVariant.BuildDistinct(i3)[0].FamilyKey,
                ShapeVariant.BuildDistinct(rec)[0].FamilyKey,
            };
            BlockBoard board = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) board.SetOccupied(7, c, true);

            for (int seed = 0; seed < 50; seed++)
            {
                AssistHandBuilder builder = new AssistHandBuilder(families, SpawnSettings.CreateDefault(), new System.Random(seed));

                List<ShapeVariant> chain = builder.Build(board, 3, 0f, excluded);

                Assert.IsFalse(chain.Any(v => excluded.Contains(v.FamilyKey)), $"seed {seed}");
            }
        }

        // ---------- Helpers ----------

        private BlockShapeData Shape(params string[] rows)
        {
            BlockShapeData data = ScriptableObject.CreateInstance<BlockShapeData>();
            data.rows = rows;
            created.Add(data);
            return data;
        }

        private static List<ShapeFamily> Families(params BlockShapeData[] shapes)
        {
            List<ShapeFamily> result = new List<ShapeFamily>();
            foreach (BlockShapeData data in shapes)
            {
                List<ShapeVariant> variants = ShapeVariant.BuildDistinct(data);
                ShapeFamily family = new ShapeFamily { key = variants[0].FamilyKey, earlyWeight = 1f, lateWeight = 1f };
                family.variants.AddRange(variants);
                family.minCells = variants.Min(v => v.CellCount);
                result.Add(family);
            }
            return result;
        }

        /// <summary>
        /// Đặt chain theo thứ tự, mỗi khối chọn vị trí phá nhiều hàng nhất; trả tổng số hàng/cột phá được
        /// </summary>
        private static int ReplayMaxLines(BlockBoard source, List<ShapeVariant> chain)
        {
            BlockBoard board = new BlockBoard(source.Rows, source.Columns);
            board.CopyFrom(source);
            int total = 0;

            foreach (ShapeVariant v in chain)
            {
                Vector2Int bestPos = new Vector2Int(-1, -1);
                int bestLines = -1;
                for (int r = 0; r <= board.Rows - v.RowsCount; r++)
                {
                    for (int c = 0; c <= board.Columns - v.ColumnsCount; c++)
                    {
                        Vector2Int pos = new Vector2Int(r, c);
                        if (!board.CanPlace(v, pos)) continue;
                        LineClearResult result = board.FindLinesCompletedBy(v, pos);
                        int lines = result.Rows.Count + result.Columns.Count;
                        if (lines > bestLines) { bestLines = lines; bestPos = pos; }
                    }
                }
                if (bestLines < 0) break;

                board.Place(v, bestPos);
                total += board.ClearFullLinesInPlace();
            }
            return total;
        }

        private static BlockBoard RandomBoard(int seed, float minFill, float maxFill)
        {
            System.Random rng = new System.Random(seed * 7919 + 13);
            float fill = minFill + (float)rng.NextDouble() * (maxFill - minFill);
            BlockBoard board = new BlockBoard(8, 8);
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    board.SetOccupied(r, c, rng.NextDouble() < fill);
                }
            }
            board.ClearFullLinesInPlace();
            return board;
        }
    }
}
