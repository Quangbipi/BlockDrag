using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Gameplay.BlockDrag;

namespace Tests.EditMode
{
    public class HandGeneratorTests
    {
        private readonly List<BlockShapeData> created = new List<BlockShapeData>();

        private BlockShapeData single, i2, i3, i4, i5, square, square3, l, l3, t, s, z, rec, diag2;
        private List<BlockShapeData> pool;

        [SetUp]
        public void SetUp()
        {
            single = Shape("1");
            i2 = Shape("1", "1");
            i3 = Shape("1", "1", "1");
            i4 = Shape("1", "1", "1", "1");
            i5 = Shape("1", "1", "1", "1", "1");
            square = Shape("1,1", "1,1");
            square3 = Shape("1,1,1", "1,1,1", "1,1,1");
            l = Shape("1,0", "1,0", "1,1");
            l3 = Shape("1,1,1", "1,0,0", "1,0,0");
            t = Shape("1,1,1", "0,1,0");
            s = Shape("0,1,1", "1,1,0");
            z = Shape("1,1,0", "0,1,1");
            rec = Shape("1,1,1", "1,1,1");
            diag2 = Shape("0,1", "1,0");

            pool = new List<BlockShapeData> { single, i2, i3, i4, i5, square, square3, l, l3, t, s, z, rec, diag2 };
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

        // ---------- ShapeVariant ----------

        [Test]
        public void BuildDistinct_Square_ReturnsSingleVariant()
        {
            Assert.AreEqual(1, ShapeVariant.BuildDistinct(square).Count);
        }

        [Test]
        public void BuildDistinct_IShape_ReturnsTwoVariants()
        {
            Assert.AreEqual(2, ShapeVariant.BuildDistinct(i3).Count);
        }

        [Test]
        public void BuildDistinct_LShape_ReturnsFourVariants()
        {
            Assert.AreEqual(4, ShapeVariant.BuildDistinct(l).Count);
        }

        [Test]
        public void FamilyKey_RotatedEquivalentAssets_AreEqual()
        {
            BlockShapeData l2 = Shape("1,1", "1,0");
            BlockShapeData l2Rotated = Shape("1,1", "0,1");

            Assert.AreEqual(ShapeVariant.BuildDistinct(l2)[0].FamilyKey, ShapeVariant.BuildDistinct(l2Rotated)[0].FamilyKey);
            Assert.AreNotEqual(ShapeVariant.BuildDistinct(s)[0].FamilyKey, ShapeVariant.BuildDistinct(z)[0].FamilyKey);
        }

        // ---------- DifficultyCurve ----------

        [Test]
        public void DifficultyCurve_BelowRecord_ScalesUpToBelowRecordMaxT()
        {
            SpawnSettings settings = SpawnSettings.CreateDefault();

            Assert.AreEqual(0f, DifficultyCurve.Evaluate(0, 4000, settings), 1e-5f);
            Assert.AreEqual(0.15f, DifficultyCurve.Evaluate(2000, 4000, settings), 1e-5f);
            Assert.AreEqual(0.3f, DifficultyCurve.Evaluate(4000, 4000, settings), 1e-5f);
        }

        [Test]
        public void DifficultyCurve_AboveRecordByRange_ReturnsOne()
        {
            SpawnSettings settings = SpawnSettings.CreateDefault();

            Assert.AreEqual(0.65f, DifficultyCurve.Evaluate(5000, 4000, settings), 1e-5f);
            Assert.AreEqual(1f, DifficultyCurve.Evaluate(6000, 4000, settings), 1e-5f);
            Assert.AreEqual(1f, DifficultyCurve.Evaluate(99999, 4000, settings), 1e-5f);
        }

        [Test]
        public void DifficultyCurve_NoRecord_UsesMinBaseline()
        {
            SpawnSettings settings = SpawnSettings.CreateDefault();

            Assert.AreEqual(0.15f, DifficultyCurve.Evaluate(500, 0, settings), 1e-5f);
            // Vượt mốc 1000, range = max(500, 1000 * 0.5) = 500
            Assert.AreEqual(1f, DifficultyCurve.Evaluate(1500, 0, settings), 1e-5f);
        }

        // ---------- HandSolver ----------

        [Test]
        public void IsSolvable_EmptyBoard_ReturnsTrue()
        {
            BlockBoard board = new BlockBoard(8, 8);
            List<IShapeFootprint> shapes = Variants(square3, i5, l3);

            Assert.IsTrue(HandSolver.IsSolvable(board, shapes, 20000));
        }

        [Test]
        public void IsSolvable_PieceNeedsLineClearFirst_ReturnsTrue()
        {
            // Bàn 4x4: hàng 0..2 đầy trừ (0,0); hàng 3 trống trừ (3,0). Single lấp (0,0) phá hàng 0
            // mới có chỗ cho I_4 nằm ngang ở hàng 0.
            BlockBoard board = new BlockBoard(4, 4);
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++) board.SetOccupied(r, c, true);
            }
            board.SetOccupied(0, 0, false);
            board.SetOccupied(3, 0, true);

            IShapeFootprint horizontalI4 = new ShapeVariant(i4, BlockRotation.Rot_90);
            Assert.IsFalse(board.CanPlaceAnywhere(horizontalI4));

            List<IShapeFootprint> shapes = new List<IShapeFootprint> { horizontalI4, new ShapeVariant(single, BlockRotation.Rot_0) };
            Assert.IsTrue(HandSolver.IsSolvable(board, shapes, 20000));
        }

        [Test]
        public void IsSolvable_NoRoom_ReturnsFalse()
        {
            BlockBoard board = new BlockBoard(4, 4);
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++) board.SetOccupied(r, c, (r + c) % 2 == 0);
            }

            Assert.IsFalse(HandSolver.IsSolvable(board, Variants(square), 20000));
        }

        [Test]
        public void IsSolvable_DoesNotModifyBoard()
        {
            BlockBoard board = new BlockBoard(4, 4);
            for (int c = 0; c < 3; c++) board.SetOccupied(0, c, true);

            HandSolver.IsSolvable(board, Variants(single, square), 20000);

            Assert.AreEqual(3, board.OccupiedCount);
        }

        [Test]
        public void CanClearAnyLine_RowMissingThreeCells_TrueForI3()
        {
            BlockBoard board = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) board.SetOccupied(7, c, true);

            Assert.IsTrue(HandSolver.CanClearAnyLine(board, new ShapeVariant(i3, BlockRotation.Rot_90)));
            Assert.IsFalse(HandSolver.CanClearAnyLine(board, new ShapeVariant(single, BlockRotation.Rot_0)));
        }

        // ---------- HandGenerator ----------

        [Test]
        public void Generate_ManySeeds_NeverDuplicatesFamily()
        {
            for (int seed = 0; seed < 500; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, SpawnSettings.CreateDefault(), null, new System.Random(seed));
                BlockBoard board = RandomBoard(seed, 0f, 0.4f);

                List<ShapeVariant> hand = generator.Generate(board, 3, (seed % 10) / 9f);

                Assert.AreEqual(3, hand.Count, $"seed {seed}");
                Assert.AreEqual(3, hand.Select(v => v.FamilyKey).Distinct().Count(), $"seed {seed}");
            }
        }

        [Test]
        public void Generate_ManySeeds_HandAlwaysSolvable()
        {
            for (int seed = 0; seed < 500; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, SpawnSettings.CreateDefault(), null, new System.Random(seed));
                BlockBoard board = RandomBoard(seed, 0.3f, 0.5f);

                List<ShapeVariant> hand = generator.Generate(board, 3, (seed % 10) / 9f);

                Assert.IsTrue(HandSolver.IsSolvable(board, hand.Cast<IShapeFootprint>().ToList(), 200000), $"seed {seed}");
            }
        }

        [Test]
        public void Generate_HelpChanceOne_ContainsClearingPiece()
        {
            SpawnSettings settings = SpawnSettings.CreateDefault();
            settings.helpChanceByFill = AnimationCurve.Constant(0f, 1f, 1f);
            settings.lateHelpMultiplier = 1f;

            BlockBoard board = new BlockBoard(8, 8);
            for (int c = 3; c < 8; c++) board.SetOccupied(7, c, true);

            for (int seed = 0; seed < 100; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, settings, null, new System.Random(seed));

                List<ShapeVariant> hand = generator.Generate(board, 3, 1f);

                Assert.IsTrue(hand.Any(v => HandSolver.CanClearAnyLine(board, v)), $"seed {seed}");
            }
        }

        [Test]
        public void Generate_InHandFamilies_AreExcluded()
        {
            List<ShapeVariant> inHand = new List<ShapeVariant>
            {
                new ShapeVariant(square, BlockRotation.Rot_0),
                new ShapeVariant(l, BlockRotation.Rot_90),
            };

            for (int seed = 0; seed < 200; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, SpawnSettings.CreateDefault(), null, new System.Random(seed));

                List<ShapeVariant> hand = generator.Generate(new BlockBoard(8, 8), 1, 0f, inHand);

                Assert.AreEqual(1, hand.Count);
                Assert.AreNotEqual(inHand[0].FamilyKey, hand[0].FamilyKey);
                Assert.AreNotEqual(inHand[1].FamilyKey, hand[0].FamilyKey);
            }
        }

        [Test]
        public void Generate_ZeroWeightShape_NeverSpawns()
        {
            List<ShapeWeight> weights = new List<ShapeWeight>
            {
                new ShapeWeight { shape = square3, earlyWeight = 0f, lateWeight = 0f },
            };

            for (int seed = 0; seed < 300; seed++)
            {
                HandGenerator generator = new HandGenerator(pool, SpawnSettings.CreateDefault(), weights, new System.Random(seed));

                List<ShapeVariant> hand = generator.Generate(new BlockBoard(8, 8), 3, 0.5f);

                Assert.IsFalse(hand.Any(v => v.Data == square3), $"seed {seed}");
            }
        }

        [Test]
        public void Generate_HelpChanceZeroAndEmptyBoard_ReturnsCountShapes()
        {
            SpawnSettings settings = SpawnSettings.CreateDefault();
            settings.helpChanceByFill = AnimationCurve.Constant(0f, 1f, 0f);
            HandGenerator generator = new HandGenerator(pool, settings, null, new System.Random(1));

            Assert.AreEqual(3, generator.Generate(new BlockBoard(8, 8), 3, 0f).Count);
        }

        [Test]
        public void Generate_BoardAlmostFull_FallsBackToSmallPieces()
        {
            // Chỉ còn 4 ô trống rời nhau, đặt 1 Single không lấp đầy hàng/cột nào → chỉ Single đặt được,
            // không hand 3 khối khác hình nào giải được → phải dùng fallback
            BlockBoard board = new BlockBoard(8, 8);
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++) board.SetOccupied(r, c, true);
            }
            board.SetOccupied(0, 0, false);
            board.SetOccupied(0, 2, false);
            board.SetOccupied(2, 0, false);
            board.SetOccupied(2, 2, false);

            List<BlockShapeData> smallPool = new List<BlockShapeData> { square, i2, single };
            HandGenerator generator = new HandGenerator(smallPool, SpawnSettings.CreateDefault(), null, new System.Random(7));

            List<ShapeVariant> hand = generator.Generate(board, 3, 0f);

            Assert.AreEqual(3, hand.Count);
            Assert.IsTrue(hand.Any(v => v.Data == single));
            Assert.AreEqual(3, hand.Select(v => v.FamilyKey).Distinct().Count());
        }

        [Test]
        public void Generator_RotatedDuplicateAssets_MergeIntoOneFamily()
        {
            BlockShapeData l2 = Shape("1,1", "1,0");
            BlockShapeData l2Rotated = Shape("1,1", "0,1");

            HandGenerator generator = new HandGenerator(new List<BlockShapeData> { l2, l2Rotated, single, null }, null, null, new System.Random(0));

            Assert.AreEqual(2, generator.FamilyCount);
        }

        // ---------- Helpers ----------

        private BlockShapeData Shape(params string[] rows)
        {
            BlockShapeData data = ScriptableObject.CreateInstance<BlockShapeData>();
            data.rows = rows;
            created.Add(data);
            return data;
        }

        private static List<IShapeFootprint> Variants(params BlockShapeData[] shapes)
        {
            return shapes.Select(d => (IShapeFootprint)new ShapeVariant(d, BlockRotation.Rot_0)).ToList();
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
