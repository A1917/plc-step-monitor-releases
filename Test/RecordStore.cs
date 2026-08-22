using System;
using System.IO;
using System.Text;

namespace Test
{
    /// <summary>
    /// 步号事件持久化：主界面「记录」开关 ON 时，把步号变化事件追加写入
    /// records/events_YYYYMMDD.csv（UTF-8 BOM，按天分文件）。
    /// 格式：时间(yyyy-MM-dd HH:mm:ss.fff),工位,步号
    /// </summary>
    public static class RecordStore
    {
        private static readonly object _lock = new object();
        private static string _currentFile = string.Empty;

        /// <summary>记录开关（主界面控制）</summary>
        public static bool Enabled { get; set; }

        /// <summary>记录目录 = exe 所在目录/records</summary>
        public static string RecordsDir
        {
            get
            {
                string dir = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                return System.IO.Path.Combine(dir ?? ".", "records");
            }
        }

        /// <summary>写入一条事件（EventStore 变化时调用，线程安全）</summary>
        public static void Write(int station, DateTime time, short step)
        {
            if (!Enabled)
            {
                return;
            }
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(RecordsDir);
                    string file = Path.Combine(RecordsDir, "events_" + time.ToString("yyyyMMdd") + ".csv");
                    bool needHeader = !File.Exists(file);
                    using (var sw = new StreamWriter(file, true, new UTF8Encoding(true)))
                    {
                        if (needHeader)
                        {
                            sw.WriteLine("时间,工位,步号");
                        }
                        sw.WriteLine(time.ToString("yyyy-MM-dd HH:mm:ss.fff") + "," + station + "," + step);
                    }
                }
                catch
                {
                    // 写盘失败不中断监控
                }
            }
        }

        /// <summary>
        /// 从 CSV 文件加载事件列表（容错：跳过表头与坏行）。
        /// 用于趋势图「加载」历史记录。
        /// </summary>
        public static System.Collections.Generic.List<StepEvent> Load(string path)
        {
            var result = new System.Collections.Generic.List<StepEvent>();
            try
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("时间"))
                    {
                        continue;   // 空行 / 表头
                    }
                    string[] parts = line.Split(',');
                    if (parts.Length != 3)
                    {
                        continue;
                    }
                    DateTime t;
                    int station;
                    short step;
                    if (!DateTime.TryParseExact(parts[0].Trim(), "yyyy-MM-dd HH:mm:ss.fff",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out t))
                    {
                        continue;
                    }
                    if (!int.TryParse(parts[1].Trim(), out station) || !short.TryParse(parts[2].Trim(), out step))
                    {
                        continue;
                    }
                    result.Add(new StepEvent(t, station, step));
                }
            }
            catch
            {
                // 加载失败返回空列表
            }
            return result;
        }
    }
}
