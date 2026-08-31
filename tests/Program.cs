using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// 逻辑类单元测试（自写断言，零依赖）：
    /// CycleDetector / EventStore / RecordStore。
    /// 运行：dotnet run --project tests（Windows dotnet，从 WSL 调 /mnt/c/Program Files/dotnet/dotnet.exe）
    /// 退出码：0 = 全部通过；1 = 有失败。
    /// </summary>
    internal static class Program
    {
        private static int _passed, _failed;

        private static void Assert(bool cond, string name)
        {
            if (cond) { _passed++; }
            else { _failed++; Console.WriteLine("  FAIL: " + name); }
        }

        private static void AssertEq<T>(T actual, T expected, string name)
        {
            if (Equals(actual, expected)) { _passed++; }
            else { _failed++; Console.WriteLine("  FAIL: " + name + " (got " + actual + ", want " + expected + ")"); }
        }

        private static List<StepEvent> MakeEvents(int station, params short[] steps)
        {
            var list = new List<StepEvent>();
            var t = new DateTime(2026, 8, 30, 10, 0, 0, 0);
            foreach (short s in steps)
            {
                list.Add(new StepEvent(t, station, s));
                t = t.AddMilliseconds(1000);
            }
            return list;
        }

        // ═══════════════ CycleDetector ═══════════════

        private static void TestCycleDetector()
        {
            Console.WriteLine("[CycleDetector]");
            // 完整周期：0→20→40→0→20→40→0 = 2 个完整周期（每周期 3s）
            var ev = MakeEvents(1, 0, 20, 40, 0, 20, 40, 0);
            var c1 = CycleDetector.Analyze(ev, 1);
            Assert(c1.HasCycle, "识别出周期");
            AssertEq(c1.CycleCount, 2, "周期数=2");
            AssertEq(c1.StartStep, (short)0, "起始步号=0");
            Assert(Math.Abs(c1.AvgCycleMs - 3000) < 1, "平均周期=3000ms (got " + c1.AvgCycleMs + ")");
            Assert(Math.Abs(c1.LastCycleMs - 3000) < 1, "最近周期=3000ms");
            // 无周期：单调递增
            var ev2 = MakeEvents(2, 0, 20, 40, 60);
            var c2 = CycleDetector.Analyze(ev2, 2);
            Assert(!c2.HasCycle, "单调序列无完整周期");
            // 中途接入：20→40→60→0→20→40→60→0（首段 3 事件完整 → 2 周期）
            var ev3 = MakeEvents(3, 20, 40, 60, 0, 20, 40, 60, 0);
            var c3 = CycleDetector.Analyze(ev3, 3);
            AssertEq(c3.StartStep, (short)0, "中途接入→起点为最小步号 0 (got " + c3.StartStep + ")");
            AssertEq(c3.CycleCount, 2, "中途接入周期数=2 (got " + c3.CycleCount + ")");
            // 采集丢步不合并：0 步偶发缺失 0,20,40,20,40,0,20,40,0（0 缺失时 20/40 稳定出现 → 自动切换起点）
            var ev7 = MakeEvents(7, 0, 20, 40, 20, 40, 0, 20, 40, 0);
            var c7 = CycleDetector.Analyze(ev7, 7);
            Assert(c7.StartStep == 20 || c7.StartStep == 40, "丢步时起点为稳定步号 (got " + c7.StartStep + ")");
            Assert(c7.CycleCount >= 2, "丢步不合并周期 (got " + c7.CycleCount + ")");
            // 空数据
            var c4 = CycleDetector.Analyze(new List<StepEvent>(), 1);
            Assert(!c4.HasCycle && c4.CycleCount == 0, "空数据安全");
            // 抖动不误切：0→20→0→40→0→20→0（回落抖动被吸收，不切成 3 个短周期）
            var ev5 = MakeEvents(5, 0, 20, 0, 40, 0, 20, 0);
            var c5 = CycleDetector.Analyze(ev5, 5);
            Assert(c5.CycleCount <= 1, "抖动不切成多个短周期 (got " + c5.CycleCount + ")");
            // 首值重复不误切：0,0,20,40,0,20,40,0（连续重复步号去重后 = 2 周期）
            var ev6 = MakeEvents(6, 1000, 0, 0, 20, 40, 0, 20, 40, 0);
            var c6 = CycleDetector.Analyze(ev6, 6);
            AssertEq(c6.CycleCount, 2, "首值重复去重后周期数=2 (got " + c6.CycleCount + ")");
            // 边界过滤后 GetBoundaries 与 Analyze 一致
            var b6 = CycleDetector.GetBoundaries(ev6);
            AssertEq(b6.Count - 1, c6.CycleCount, "GetBoundaries 与 Analyze 一致");
        }

        // ═══════════════ EventStore ═══════════════

        private static void TestEventStore()
        {
            Console.WriteLine("[EventStore]");
            EventStore.Clear();
            // Feed 生成事件（含首值）
            EventStore.FeedSingle(0, 10);
            var all = EventStore.GetAll(0);
            AssertEq(all.Count, 1, "首值事件=1");
            AssertEq(all[0].Step, (short)10, "首值步号=10");
            // 步号不变不生成事件
            EventStore.FeedSingle(0, 10);
            AssertEq(EventStore.GetAll(0).Count, 1, "步号不变不新增");
            // 变化生成事件
            EventStore.FeedSingle(0, 20);
            AssertEq(EventStore.GetAll(0).Count, 2, "步号变化新增");
            // GetSince 增量
            var t0 = DateTime.Now;
            EventStore.FeedSingle(1, 5);
            var since = EventStore.GetSince(1, t0);
            Assert(since.Count >= 1, "GetSince 返回增量");
            // 环形缓冲上限
            EventStore.Clear();
            for (int i = 0; i < EventStore.BufferCapacity + 100; i++)
            {
                EventStore.FeedSingle(2, (short)(i % 30000));
            }
            Assert(EventStore.GetAll(2).Count <= EventStore.BufferCapacity, "环形缓冲不超上限 (got " + EventStore.GetAll(2).Count + ")");
        }

        // ═══════════════ RecordStore ═══════════════

        private static void TestRecordStore()
        {
            Console.WriteLine("[RecordStore]");
            // Load：CSV 解析（表头/坏行容错/时间格式）
            string tmp = Path.Combine(Path.GetTempPath(), "pcmtest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                string csv = Path.Combine(tmp, "sample.csv");
                File.WriteAllText(csv,
                    "时间,工位,步号\r\n" +
                    "2026-08-30 10:00:00.000,0,10\r\n" +
                    "坏行\r\n" +
                    "2026-08-30 10:00:01.000,1,20\r\n" +
                    ",,,\r\n",
                    new UTF8Encoding(true));
                var list = RecordStore.Load(csv);
                AssertEq(list.Count, 2, "CSV 解析 2 条有效（跳过表头/坏行）");
                AssertEq(list[0].Step, (short)10, "首条步号=10");
                AssertEq(list[1].Station, 1, "次条工位=1");
                // LoadMultiple 排序合并
                string csv2 = Path.Combine(tmp, "sample2.csv");
                File.WriteAllText(csv2, "2026-08-30 09:59:00.000,2,5\r\n", new UTF8Encoding(true));
                var merged = RecordStore.LoadMultiple(new[] { csv, csv2 });
                AssertEq(merged.Count, 3, "多文件合并=3");
                Assert(merged[0].Time < merged[1].Time, "合并按时间排序");
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        private static int Main()
        {
            Console.WriteLine("== 单元测试 ==");
            TestCycleDetector();
            TestEventStore();
            TestRecordStore();
            Console.WriteLine("== 结果: " + _passed + " 通过, " + _failed + " 失败 ==");
            return _failed == 0 ? 0 : 1;
        }
    }
}