using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    /// <summary>周期判定结果</summary>
    public class CycleInfo
    {
        public int CycleCount;        // 完成的周期数
        public double LastCycleMs;    // 最近一个周期耗时(ms)
        public double AvgCycleMs;     // 平均周期(ms)
        public double CurrentMs;      // 当前进行中的周期已耗时(ms)
        public short StartStep;       // 起始步号
        public bool HasCycle;         // 是否检测到周期
    }

    /// <summary>
    /// 周期判定：分析工位步号序列，检测循环（起始步号再次出现 = 一个周期完成）。
    /// 纯静态无副作用，不影响原有功能。
    /// </summary>
    public static class CycleDetector
    {
        /// <summary>
        /// 周期边界索引（单工位事件，时间有序）：
        /// 起点 = 出现间隔最稳定的步号（采集丢步时仍能正确切分，不依赖特定步号每周期都出现）；
        /// 过滤噪声：连续相同步号去重、边界间隔 ≥3 事件、周期内不同步号 ≥3（防回落抖动/首值重复误切）。
        /// </summary>
        public static List<int> GetBoundaries(List<StepEvent> ev)
        {
            var bounds = new List<int> { 0 };
            if (ev == null || ev.Count < 2) return bounds;
            // 去连续重复步号（首值/重复事件）
            var clean = new List<StepEvent>();
            foreach (var e in ev)
                if (clean.Count == 0 || clean[clean.Count - 1].Step != e.Step) clean.Add(e);
            if (clean.Count < 2) return bounds;
            // 起点步号 = 出现间隔最稳定的步号（出现 ≥3 次，间隔一致）
            short s0 = SelectStartStep(clean);
            if (s0 < 0) return bounds;   // 无稳定起点 → 不识别周期
            // 候选边界（起点再次出现），过滤短噪声段
            var valid = new List<int>();
            foreach (int c in CandidateIndexes(clean, s0))
            {
                int prev = valid.Count > 0 ? valid[valid.Count - 1] : 0;
                int span = c - prev;
                if (span < 3) continue;                              // 周期至少 3 个事件
                var set = new HashSet<short>();
                for (int i = prev; i < c; i++) set.Add(clean[i].Step);
                if (set.Count < 3) continue;                         // 周期内至少 3 个不同步号
                valid.Add(c);
            }
            foreach (int ci in valid)
            {
                int orig = ev.IndexOf(clean[ci]);   // 映射回原始索引（引用相等）
                if (orig > 0) bounds.Add(orig);
            }
            return bounds;
        }

        /// <summary>
        /// 选周期起点步号：出现 ≥3 次、间隔一致（方差最小）的步号。
        /// 采集丢步（如 0 步偶发缺失）时，稳定出现的步号自动成为起点，周期不合并。
        /// </summary>
        public static short SelectStartStep(List<StepEvent> clean)
        {
            if (clean == null || clean.Count < 2) return -1;
            var byStep = new Dictionary<short, List<int>>();
            for (int i = 0; i < clean.Count; i++)
            {
                if (!byStep.TryGetValue(clean[i].Step, out var idxs)) { idxs = new List<int>(); byStep[clean[i].Step] = idxs; }
                idxs.Add(i);
            }
            short best = -1;
            double bestVariance = double.MaxValue;
            int bestCount = 0;
            foreach (var kv in byStep)
            {
                var idxs = kv.Value;
                if (idxs.Count < 2) continue;   // 至少出现 2 次（≥1 个完整周期）
                var gaps = new List<int>();
                bool ok = true;
                for (int i = 1; i < idxs.Count; i++)
                {
                    int g = idxs[i] - idxs[i - 1];
                    if (g < 2) { ok = false; break; }   // 连续重复（不应出现）
                    gaps.Add(g);
                }
                if (!ok || gaps.Count == 0) continue;
                double mean = 0;
                foreach (int g in gaps) mean += g;
                mean /= gaps.Count;
                double variance = 0;
                foreach (int g in gaps) variance += (g - mean) * (g - mean);
                variance /= gaps.Count;
                // 方差最小优先；方差接近选出现次数多的；再平局选步号小的（流程起点通常最小）
                if (variance < bestVariance - 1e-9 ||
                    (Math.Abs(variance - bestVariance) <= 1e-9 &&
                     (idxs.Count > bestCount || (idxs.Count == bestCount && kv.Key < best))))
                {
                    best = kv.Key;
                    bestVariance = variance;
                    bestCount = idxs.Count;
                }
            }
            return best;
        }

        private static IEnumerable<int> CandidateIndexes(List<StepEvent> clean, short s0)
        {
            for (int i = 1; i < clean.Count; i++)
                if (clean[i].Step == s0) yield return i;
        }

        public static CycleInfo Analyze(List<StepEvent> events, int station)
        {
            var info = new CycleInfo { StartStep = -1 };
            if (events == null || events.Count == 0) return info;

            // 取该工位事件（时间有序）
            var list = events.Where(e => e.Station == station).OrderBy(e => e.Time).ToList();
            if (list.Count == 0) return info;

            // 起点步号 = 出现间隔最稳定的步号（采集丢步时自动切换，周期不合并）
            short startStep = SelectStartStep(list);
            if (startStep < 0)
            {
                info.HasCycle = false;
                info.CycleCount = 0;
                info.CurrentMs = (list[list.Count - 1].Time - list[0].Time).TotalMilliseconds;
                return info;
            }
            info.StartStep = startStep;

            var bounds = GetBoundaries(list);
            if (bounds.Count < 2)   // 无完整周期
            {
                info.HasCycle = false;
                info.CycleCount = 0;
                info.CurrentMs = (list[list.Count - 1].Time - list[0].Time).TotalMilliseconds;
                return info;
            }
            info.HasCycle = true;
            info.CycleCount = bounds.Count - 1;
            // 周期起点时间（原始事件索引）
            DateTime firstTime = list[bounds[0]].Time;
            // 平均周期 = 相邻周期起点间隔的平均
            double total = 0;
            DateTime prev = firstTime;
            for (int b = 1; b < bounds.Count; b++)
            {
                DateTime t = list[bounds[b]].Time;
                total += (t - prev).TotalMilliseconds;
                prev = t;
            }
            info.AvgCycleMs = total / info.CycleCount;
            // 最近周期 = 最后完成周期的耗时
            info.LastCycleMs = (list[bounds[bounds.Count - 1]].Time - list[bounds[bounds.Count - 2]].Time).TotalMilliseconds;
            // 当前进行中的周期 = 最后周期起点到现在（数据末尾）
            info.CurrentMs = (list[list.Count - 1].Time - prev).TotalMilliseconds;
            return info;
        }

        public static string FormatMs(double ms)
        {
            if (ms >= 1000) return (ms / 1000.0).ToString("F2") + "s";
            return ms.ToString("F0") + "ms";
        }
    }
}