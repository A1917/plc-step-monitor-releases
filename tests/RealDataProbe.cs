using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// 真实数据回归测试：读取 604前氦检 CSV，验证关键工位周期识别。
    /// 数据特征：多工位混合事件、中途接入、异常 0 回落、0↔110 交替等。
    /// </summary>
    internal static class RealDataProbe
    {
        private static List<StepEvent> LoadCsv(string path)
        {
            var all = new List<StepEvent>();
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("时间")) continue;
                string[] p = line.Split(',');
                if (p.Length != 3) continue;
                if (DateTime.TryParseExact(p[0].Trim(), "yyyy-MM-dd HH:mm:ss.fff",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var t)
                    && int.TryParse(p[1].Trim(), out var st) && short.TryParse(p[2].Trim(), out var step))
                    all.Add(new StepEvent(t, st, step));
            }
            return all;
        }

        public static void Run()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "data", "604前氦检_20260828_10.csv");
            if (!File.Exists(path)) path = Path.Combine(Directory.GetCurrentDirectory(), "data", "604前氦检_20260828_10.csv");
            if (!File.Exists(path))
            {
                Console.WriteLine("  SKIP: 真实数据文件不存在");
                return;
            }
            var all = LoadCsv(path);
            Console.WriteLine("  真实数据: " + all.Count + " 事件, " + all.Select(e => e.Station).Distinct().Count() + " 工位");

            // 关键工位断言（真实设备节拍）
            Check(all, 0, "工位0 起点0", s => s == 0, c => c >= 20, "周期数≥20");
            Check(all, 2, "工位2 起点30(非异常0)", s => s == 30, c => c >= 20, "周期数≥20");
            Check(all, 3, "工位3 起点0(0↔110交替)", s => s == 0, c => c >= 10, "周期数≥10");
            Check(all, 4, "工位4 起点35", s => s == 35, c => c >= 20, "周期数≥20");
            Check(all, 26, "工位26 周期数≥20", s => true, c => c >= 20, "周期数≥20");
            Check(all, 29, "工位29 起点30", s => s == 30, c => c >= 20, "周期数≥20");
        }

        private static void Check(List<StepEvent> all, int station, string name,
            Func<short, bool> stepCheck, Func<int, bool> countCheck, string countName)
        {
            var list = all.Where(e => e.Station == station).OrderBy(e => e.Time).ToList();
            var c = CycleDetector.Analyze(list, station);
            bool stepOk = stepCheck(c.StartStep);
            bool countOk = countCheck(c.CycleCount);
            Program.Assert(stepOk && countOk, name + " (起点=" + c.StartStep + " 周期=" + c.CycleCount + ")");
        }
    }
}
