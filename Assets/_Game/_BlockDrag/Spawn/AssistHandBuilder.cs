using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.BlockDrag
{
    /// <summary>
    /// Dựng hand theo chuỗi đặt đẹp trên bàn mô phỏng: mỗi bước chọn khối + vị trí điểm cao
    /// (dọn sạch bàn > phá hàng > làm hàng gần đầy > vừa khít), đặt lên bàn mô phỏng rồi tính bước tiếp.
    /// Hand trả về luôn đặt hết được theo đúng chuỗi đó.
    /// </summary>
    public sealed class AssistHandBuilder
    {
        private struct Candidate
        {
            public ShapeFamily family;
            public ShapeVariant variant;
            public Vector2Int position;
            public float score;
        }

        private readonly IReadOnlyList<ShapeFamily> families;
        private readonly SpawnSettings settings;
        private readonly System.Random rng;

        private BlockBoard sim;
        private bool[,] footprint;
        // Bản sao ô của sim để chấm điểm đọc mảng trực tiếp (Score gọi hàng nghìn lần mỗi hand)
        private bool[,] simCells;
        private bool[] rowTouched, colTouched, rowFull, colFull;
        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private readonly List<Candidate> candidates = new List<Candidate>();
        private readonly List<Candidate> pool = new List<Candidate>();
        private readonly HashSet<string> used = new HashSet<string>();

        public AssistHandBuilder(IReadOnlyList<ShapeFamily> families, SpawnSettings settings, System.Random rng)
        {
            this.families = families;
            this.settings = settings ?? SpawnSettings.CreateDefault();
            this.rng = rng ?? new System.Random();
        }

        /// <summary>
        /// Dựng tối đa count khối. Ít hơn count nếu hết family hợp lệ hoặc không còn chỗ đặt.
        /// inHand được đặt lên bàn mô phỏng trước để chuỗi tính tiếp từ bàn sau khi chúng nằm xuống.
        /// </summary>
        public List<ShapeVariant> Build(BlockBoard board, int count, float difficulty,
                                        ICollection<string> excludedFamilies, IReadOnlyList<ShapeVariant> inHand = null)
        {
            List<ShapeVariant> result = new List<ShapeVariant>();
            if (board == null || families == null || count <= 0) return result;

            EnsureScratch(board);
            sim.CopyFrom(board);
            SyncSimCells();
            used.Clear();

            if (inHand != null)
            {
                for (int i = 0; i < inHand.Count; i++)
                {
                    if (inHand[i] != null && TryFindBestPlacement(inHand[i], out Vector2Int pos, out _))
                    {
                        PlaceOnSim(inHand[i], pos);
                    }
                }
            }

            for (int step = 0; step < count; step++)
            {
                CollectCandidates(difficulty, excludedFamilies);
                if (candidates.Count == 0) break;

                Candidate chosen = PickFromTop(difficulty);
                PlaceOnSim(chosen.variant, chosen.position);
                used.Add(chosen.family.key);
                result.Add(chosen.variant);
            }

            return result;
        }

        /// <summary>
        /// Điểm "đẹp" của việc đặt variant tại position trên board (không thay đổi board)
        /// </summary>
        public float PlacementScore(BlockBoard board, ShapeVariant variant, Vector2Int position)
        {
            EnsureScratch(board);
            sim.CopyFrom(board);
            SyncSimCells();
            return board.CanPlace(variant, position) ? Score(variant, position) : float.NegativeInfinity;
        }

        private void CollectCandidates(float difficulty, ICollection<string> excludedFamilies)
        {
            candidates.Clear();
            for (int f = 0; f < families.Count; f++)
            {
                ShapeFamily family = families[f];
                if (used.Contains(family.key)) continue;
                if (excludedFamilies != null && excludedFamilies.Contains(family.key)) continue;
                if (family.Weight(difficulty) <= 0f) continue;

                Candidate best = default;
                bool found = false;
                for (int v = 0; v < family.variants.Count; v++)
                {
                    if (TryFindBestPlacement(family.variants[v], out Vector2Int pos, out float score)
                        && (!found || score > best.score))
                    {
                        best = new Candidate { family = family, variant = family.variants[v], position = pos, score = score };
                        found = true;
                    }
                }

                if (found) candidates.Add(best);
            }
        }

        private Candidate PickFromTop(float difficulty)
        {
            float top = float.NegativeInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].score > top) top = candidates[i].score;
            }

            float threshold = top - Mathf.Abs(top) * Mathf.Clamp01(settings.assistTolerance);
            pool.Clear();
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].score < threshold) continue;
                pool.Add(candidates[i]);
                total += candidates[i].family.Weight(difficulty);
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].family.Weight(difficulty);
                if (roll < 0) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        private bool TryFindBestPlacement(ShapeVariant variant, out Vector2Int bestPos, out float bestScore)
        {
            bestPos = default;
            bestScore = float.NegativeInfinity;
            bool found = false;

            int maxR = sim.Rows - variant.RowsCount;
            int maxC = sim.Columns - variant.ColumnsCount;
            for (int r = 0; r <= maxR; r++)
            {
                for (int c = 0; c <= maxC; c++)
                {
                    Vector2Int pos = new Vector2Int(r, c);
                    if (!sim.CanPlace(variant, pos)) continue;

                    float score = Score(variant, pos);
                    if (!found || score > bestScore)
                    {
                        bestScore = score;
                        bestPos = pos;
                        found = true;
                    }
                }
            }
            return found;
        }

        private void PlaceOnSim(ShapeVariant variant, Vector2Int pos)
        {
            sim.Place(variant, pos);
            sim.ClearFullLinesInPlace();
            SyncSimCells();
        }

        private void SyncSimCells()
        {
            for (int r = 0; r < sim.Rows; r++)
            {
                for (int c = 0; c < sim.Columns; c++)
                {
                    simCells[r, c] = sim.IsOccupied(r, c);
                }
            }
        }

        /// <summary>
        /// Tính điểm trên sim + footprint của khối, không copy bàn
        /// </summary>
        private float Score(ShapeVariant variant, Vector2Int pos)
        {
            int rows = sim.Rows;
            int cols = sim.Columns;

            cells.Clear();
            for (int r = 0; r < variant.RowsCount; r++)
            {
                for (int c = 0; c < variant.ColumnsCount; c++)
                {
                    if (!variant.HasBlockAt(r, c)) continue;
                    int tr = pos.x + r;
                    int tc = pos.y + c;
                    footprint[tr, tc] = true;
                    rowTouched[tr] = true;
                    colTouched[tc] = true;
                    cells.Add(new Vector2Int(tr, tc));
                }
            }

            int contact = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                contact += ContactAt(cells[i].x - 1, cells[i].y);
                contact += ContactAt(cells[i].x + 1, cells[i].y);
                contact += ContactAt(cells[i].x, cells[i].y - 1);
                contact += ContactAt(cells[i].x, cells[i].y + 1);
            }

            int lines = 0;
            float setup = 0f;
            for (int r = 0; r < rows; r++)
            {
                if (!rowTouched[r]) continue;
                int filled = 0;
                for (int c = 0; c < cols; c++)
                {
                    if (simCells[r, c] || footprint[r, c]) filled++;
                }
                if (filled == cols) { rowFull[r] = true; lines++; }
                else { float ratio = (float)filled / cols; setup += ratio * ratio; }
            }
            for (int c = 0; c < cols; c++)
            {
                if (!colTouched[c]) continue;
                int filled = 0;
                for (int r = 0; r < rows; r++)
                {
                    if (simCells[r, c] || footprint[r, c]) filled++;
                }
                if (filled == rows) { colFull[c] = true; lines++; }
                else { float ratio = (float)filled / rows; setup += ratio * ratio; }
            }

            float score = contact * settings.contactScore;
            if (lines > 0)
            {
                score += lines * settings.clearLineScore;
                if (IsBoardEmptyAfterClear()) score += settings.perfectClearScore;
            }
            else
            {
                score += setup;
            }

            // Trả scratch về trạng thái sạch
            for (int i = 0; i < cells.Count; i++)
            {
                footprint[cells[i].x, cells[i].y] = false;
                rowTouched[cells[i].x] = false;
                colTouched[cells[i].y] = false;
                rowFull[cells[i].x] = false;
                colFull[cells[i].y] = false;
            }

            return score;
        }

        private int ContactAt(int r, int c)
        {
            if (r < 0 || c < 0 || r >= sim.Rows || c >= sim.Columns) return 1;
            return simCells[r, c] ? 1 : 0;
        }

        private bool IsBoardEmptyAfterClear()
        {
            for (int r = 0; r < sim.Rows; r++)
            {
                for (int c = 0; c < sim.Columns; c++)
                {
                    bool occupied = simCells[r, c] || footprint[r, c];
                    if (occupied && !rowFull[r] && !colFull[c]) return false;
                }
            }
            return true;
        }

        private void EnsureScratch(BlockBoard board)
        {
            if (sim != null && sim.Rows == board.Rows && sim.Columns == board.Columns) return;

            sim = new BlockBoard(board.Rows, board.Columns);
            footprint = new bool[board.Rows, board.Columns];
            simCells = new bool[board.Rows, board.Columns];
            rowTouched = new bool[board.Rows];
            rowFull = new bool[board.Rows];
            colTouched = new bool[board.Columns];
            colFull = new bool[board.Columns];
        }
    }
}
