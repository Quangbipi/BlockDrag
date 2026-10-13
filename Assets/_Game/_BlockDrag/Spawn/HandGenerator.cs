using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Sinh khối cho khay chờ: chọn theo trọng số, không trùng hình (kể cả xoay khác hướng),
    /// có xác suất ép 1 khối phá hàng, và luôn đảm bảo đặt hết được trên bàn hiện tại.
    /// </summary>
    public sealed class HandGenerator
    {
        private readonly List<ShapeFamily> families = new List<ShapeFamily>();
        private readonly SpawnSettings settings;
        private readonly System.Random rng;
        private readonly AssistHandBuilder assistBuilder;

        // Buffer dùng lại giữa các lần Generate
        private readonly List<ShapeVariant> hand = new List<ShapeVariant>();
        private readonly List<IShapeFootprint> solverInput = new List<IShapeFootprint>();
        private readonly HashSet<string> excludedFamilies = new HashSet<string>();
        private readonly Dictionary<ShapeFamily, List<ShapeVariant>> clearingByFamily = new Dictionary<ShapeFamily, List<ShapeVariant>>();
        private readonly List<ShapeFamily> candidates = new List<ShapeFamily>();

        public int FamilyCount => families.Count;

        public HandGenerator(IReadOnlyList<BlockShapeData> shapes, SpawnSettings settings, IReadOnlyList<ShapeWeight> weights, System.Random rng)
        {
            this.settings = settings ?? SpawnSettings.CreateDefault();
            this.rng = rng ?? new System.Random();
            BuildFamilies(shapes, weights);
            assistBuilder = new AssistHandBuilder(families, this.settings, this.rng);
        }

        /// <summary>
        /// Sinh count khối mới. inHand là các khối còn trên khay (loại trùng hình và cùng tham gia kiểm tra đặt hết).
        /// belowRecord = chưa vượt kỷ lục đầu ván → luôn dựng assist hand; sau đó theo DifficultyCurve.AssistChance.
        /// </summary>
        public List<ShapeVariant> Generate(BlockBoard board, int count, float difficulty,
                                           IReadOnlyList<ShapeVariant> inHand = null, bool belowRecord = false)
        {
            List<ShapeVariant> result = new List<ShapeVariant>();
            if (board == null || count <= 0 || families.Count == 0) return result;

            difficulty = Mathf.Clamp01(difficulty);

            excludedFamilies.Clear();
            if (inHand != null)
            {
                for (int i = 0; i < inHand.Count; i++)
                {
                    if (inHand[i] != null) excludedFamilies.Add(inHand[i].FamilyKey);
                }
            }

            if (rng.NextDouble() < DifficultyCurve.AssistChance(belowRecord, difficulty, settings)
                && TryBuildAssistHand(board, count, difficulty, inHand, result))
            {
                return result;
            }

            float helpChance = Mathf.Clamp01(settings.helpChanceByFill.Evaluate(board.FilledRatio))
                               * Mathf.Lerp(1f, settings.lateHelpMultiplier, difficulty);
            bool wantHelp = rng.NextDouble() < helpChance;
            if (wantHelp)
            {
                CollectClearingVariants(board);
            }

            for (int attempt = 0; attempt < settings.maxAttempts; attempt++)
            {
                hand.Clear();

                if (wantHelp && clearingByFamily.Count > 0)
                {
                    ShapeFamily helper = PickWeighted(clearingByFamily.Keys, difficulty);
                    if (helper != null)
                    {
                        List<ShapeVariant> options = clearingByFamily[helper];
                        hand.Add(options[rng.Next(options.Count)]);
                    }
                }

                while (hand.Count < count)
                {
                    ShapeFamily family = PickWeighted(families, difficulty);
                    if (family == null) break;
                    hand.Add(family.variants[rng.Next(family.variants.Count)]);
                }

                if (hand.Count == count && IsSolvable(board, inHand, hand))
                {
                    Shuffle(hand);
                    result.AddRange(hand);
                    return result;
                }
            }

            result.AddRange(BuildFallbackHand(board, count, inHand));
            return result;
        }

        /// <summary>
        /// Dựng hand theo chuỗi phá hàng; thiếu khối thì bù random (khác hình) sao cho cả hand vẫn đặt hết được
        /// </summary>
        private bool TryBuildAssistHand(BlockBoard board, int count, float difficulty, IReadOnlyList<ShapeVariant> inHand, List<ShapeVariant> result)
        {
            List<ShapeVariant> chain = assistBuilder.Build(board, count, difficulty, excludedFamilies, inHand);
            if (chain.Count == 0) return false;

            for (int attempt = 0; attempt < settings.maxAttempts; attempt++)
            {
                hand.Clear();
                hand.AddRange(chain);
                while (hand.Count < count)
                {
                    ShapeFamily family = PickWeighted(families, difficulty);
                    if (family == null) break;
                    hand.Add(family.variants[rng.Next(family.variants.Count)]);
                }

                // Chuỗi đủ khối thì chắc chắn đặt hết được, khỏi chạy solver
                if (hand.Count == count && (chain.Count == count || IsSolvable(board, inHand, hand)))
                {
                    Shuffle(hand);
                    result.AddRange(hand);
                    return true;
                }
            }
            return false;
        }

        private void BuildFamilies(IReadOnlyList<BlockShapeData> shapes, IReadOnlyList<ShapeWeight> weights)
        {
            if (shapes == null) return;

            Dictionary<string, ShapeFamily> byKey = new Dictionary<string, ShapeFamily>();
            for (int i = 0; i < shapes.Count; i++)
            {
                BlockShapeData data = shapes[i];
                List<ShapeVariant> variants = ShapeVariant.BuildDistinct(data);
                if (variants.Count == 0) continue;

                ShapeWeight weight = FindWeight(weights, data);
                float early = weight != null ? weight.earlyWeight : 1f;
                float late = weight != null ? weight.lateWeight : 1f;

                string familyKey = variants[0].FamilyKey;
                if (!byKey.TryGetValue(familyKey, out ShapeFamily family))
                {
                    family = new ShapeFamily { key = familyKey, earlyWeight = early, lateWeight = late };
                    byKey.Add(familyKey, family);
                    families.Add(family);
                }
                else
                {
                    // Nhiều asset cùng hình: lấy weight lớn nhất để hình không bị ra gấp đôi
                    family.earlyWeight = Mathf.Max(family.earlyWeight, early);
                    family.lateWeight = Mathf.Max(family.lateWeight, late);
                }

                for (int v = 0; v < variants.Count; v++)
                {
                    if (!family.variants.Exists(existing => existing.Key == variants[v].Key))
                    {
                        family.variants.Add(variants[v]);
                        family.minCells = Mathf.Min(family.minCells, variants[v].CellCount);
                    }
                }
            }
        }

        private static ShapeWeight FindWeight(IReadOnlyList<ShapeWeight> weights, BlockShapeData data)
        {
            if (weights == null) return null;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] != null && weights[i].shape == data) return weights[i];
            }
            return null;
        }

        private void CollectClearingVariants(BlockBoard board)
        {
            clearingByFamily.Clear();
            for (int f = 0; f < families.Count; f++)
            {
                ShapeFamily family = families[f];
                if (excludedFamilies.Contains(family.key)) continue;

                List<ShapeVariant> clearing = null;
                for (int v = 0; v < family.variants.Count; v++)
                {
                    if (HandSolver.CanClearAnyLine(board, family.variants[v]))
                    {
                        (clearing ??= new List<ShapeVariant>()).Add(family.variants[v]);
                    }
                }

                if (clearing != null) clearingByFamily.Add(family, clearing);
            }
        }

        /// <summary>
        /// Chọn family theo trọng số, bỏ qua family đã có trên khay hoặc trong hand đang sinh
        /// </summary>
        private ShapeFamily PickWeighted(IEnumerable<ShapeFamily> source, float difficulty)
        {
            candidates.Clear();
            float total = 0f;
            foreach (ShapeFamily family in source)
            {
                if (excludedFamilies.Contains(family.key) || HandContains(family.key)) continue;
                float w = family.Weight(difficulty);
                if (w <= 0f) continue;
                candidates.Add(family);
                total += w;
            }

            if (candidates.Count == 0) return null;

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Weight(difficulty);
                if (roll < 0) return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        private bool HandContains(string familyKey)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].FamilyKey == familyKey) return true;
            }
            return false;
        }

        private bool IsSolvable(BlockBoard board, IReadOnlyList<ShapeVariant> inHand, List<ShapeVariant> newShapes)
        {
            solverInput.Clear();
            if (inHand != null)
            {
                for (int i = 0; i < inHand.Count; i++)
                {
                    if (inHand[i] != null) solverInput.Add(inHand[i]);
                }
            }
            for (int i = 0; i < newShapes.Count; i++)
            {
                solverInput.Add(newShapes[i]);
            }
            return HandSolver.IsSolvable(board, solverInput, settings.maxSolverNodes);
        }

        /// <summary>
        /// Ưu tiên khối nhỏ: thêm dần family nhỏ nhất mà hand vẫn đặt hết được.
        /// Không đủ thì bù bằng khối nhỏ nhất còn lại (bàn gần kín, Game Over là hợp lý).
        /// </summary>
        private List<ShapeVariant> BuildFallbackHand(BlockBoard board, int count, IReadOnlyList<ShapeVariant> inHand)
        {
            hand.Clear();

            List<ShapeFamily> sorted = new List<ShapeFamily>(families);
            sorted.Sort((a, b) => a.minCells.CompareTo(b.minCells));

            for (int f = 0; f < sorted.Count && hand.Count < count; f++)
            {
                ShapeFamily family = sorted[f];
                if (excludedFamilies.Contains(family.key)) continue;

                for (int v = 0; v < family.variants.Count; v++)
                {
                    hand.Add(family.variants[v]);
                    if (IsSolvable(board, inHand, hand)) break;
                    hand.RemoveAt(hand.Count - 1);
                }
            }

            for (int f = 0; f < sorted.Count && hand.Count < count; f++)
            {
                ShapeFamily family = sorted[f];
                if (excludedFamilies.Contains(family.key) || HandContains(family.key)) continue;
                hand.Add(SmallestVariant(family));
            }

            Shuffle(hand);
            return new List<ShapeVariant>(hand);
        }

        private static ShapeVariant SmallestVariant(ShapeFamily family)
        {
            ShapeVariant best = family.variants[0];
            for (int i = 1; i < family.variants.Count; i++)
            {
                if (family.variants[i].CellCount < best.CellCount) best = family.variants[i];
            }
            return best;
        }

        private void Shuffle(List<ShapeVariant> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
