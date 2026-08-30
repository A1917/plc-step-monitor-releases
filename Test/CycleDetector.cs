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
        public static CycleInfo Analyze(List<StepEvent> events, int station)
        {
            var info = new CycleInfo { StartStep = -1 };
            if (events == null || events.Count == 0) return info;

            // 取该工位事件（时间有序）
            var list = events.Where(e => e.Station == station).OrderBy(e => e.Time).ToList();
            if (list.Count == 0) return info;

            info.StartStep = list[0].Step;
            DateTime firstTime = list[0].Time;
            // 最小步号 = 流程起点（数据中途接入时周期边界更自然）
            short startStep = list[0].Step;
            foreach (var e2 in list) if (e2.Step < startStep) startStep = e2.Step;
            info.StartStep = startStep;
            // 以最小步号再次出现为周期完成点
            var cycleStarts = new List<DateTime>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Step == startStep && i > 0)
                    cycleStarts.Add(list[i].Time);
            }

            info.CycleCount = cycleStarts.Count;
            if (cycleStarts.Count > 0)
            {
                info.HasCycle = true;
                info.LastCycleMs = (cycleStarts[cycleStarts.Count - 1] - firstTime).TotalMilliseconds / cycleStarts.Count;
                // 平均周期 = 相邻周期起点间隔的平均
                double total = 0;
                DateTime prev = firstTime;
                foreach (var t in cycleStarts) { total += (t - prev).TotalMilliseconds; prev = t; }
                info.AvgCycleMs = total / cycleStarts.Count;
                // 当前进行中的周期 = 最后一个周期起点到现在（数据末尾）
                info.CurrentMs = (list[list.Count - 1].Time - prev).TotalMilliseconds;
            }
            else
            {
                // 无完整周期：当前进行中 = 首事件到现在
                info.HasCycle = false;
                info.CurrentMs = (list[list.Count - 1].Time - firstTime).TotalMilliseconds;
            }
            return info;
        }

        public static string FormatMs(double ms)
        {
            if (ms >= 1000) return (ms / 1000.0).ToString("F2") + "s";
            return ms.ToString("F0") + "ms";
        }
    }
}