using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Test
{
    /// <summary>
    /// 步号事件持久化（CSV 按小时拆分版）：主界面「记录」开关每开启一次，新建/复用前缀子目录，
    /// 按小时自动切分文件。文件名 = {前缀}_日期_小时.csv，目录 = records/{前缀}/。
    /// 格式：时间(yyyy-MM-dd HH:mm:ss.fff),工位,步号（UTF-8 BOM）。
    /// </summary>
    public static class RecordStore
    {
        private static readonly object _lock = new object();
        private static string _currentHour = string.Empty;   // "yyyyMMdd_HH"
        private static StreamWriter _writer;                  // 当前小时文件写入器（AutoFlush）
        private static string _prefix = string.Empty;

        /// <summary>记录中</summary>
        public static bool Enabled { get; private set; }

        /// <summary>记录根目录 = exe 所在目录/records</summary>
        public static string RecordsDir
        {
            get
            {
                string dir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                return Path.Combine(dir ?? ".", "records");
            }
        }

        /// <summary>开始记录会话：新建前缀子目录，以当前小时为起点切分文件</summary>
        public static void Start(string prefix)
        {
            lock (_lock)
            {
                _prefix = string.IsNullOrWhiteSpace(prefix) ? "PLCStep" : prefix.Trim();
                string dir = Path.Combine(RecordsDir, _prefix);
                Directory.CreateDirectory(dir);
                RotateFile(dir);   // 打开当前小时文件
                Enabled = true;
            }
        }

        /// <summary>停止记录会话</summary>
        public static void Stop()
        {
            lock (_lock)
            {
                Enabled = false;
                try { _writer?.Close(); _writer?.Dispose(); } catch { }
                _writer = null;
                _currentHour = string.Empty;
            }
        }

        /// <summary>写入一条事件（EventStore 变化时调用，线程安全；到整点自动切新文件）</summary>
        public static void Write(int station, DateTime time, short step)
        {
            lock (_lock)
            {
                if (!Enabled || _writer == null) return;
                string hour = time.ToString("yyyyMMdd_HH");
                if (hour != _currentHour)
                {
                    string dir = Path.Combine(RecordsDir, _prefix);
                    RotateFile(dir);   // 到整点切新文件
                }
                try
                {
                    _writer.WriteLine(time.ToString("yyyy-MM-dd HH:mm:ss.fff") + "," + station + "," + step);
                }
                catch
                {
                    // 写盘失败不中断监控
                }
            }
        }

        /// <summary>打开 {前缀}_日期_小时.csv（写入表头，若文件已存在则追加）</summary>
        private static void RotateFile(string dir)
        {
            try { _writer?.Close(); _writer?.Dispose(); } catch { }
            _currentHour = DateTime.Now.ToString("yyyyMMdd_HH");
            string fileName = _prefix + "_" + _currentHour + ".csv";
            string path = Path.Combine(dir, fileName);
            bool exists = File.Exists(path);
            _writer = new StreamWriter(path, true, new UTF8Encoding(true)) { AutoFlush = true };
            if (!exists)
            {
                _writer.WriteLine("时间,工位,步号");   // 新文件写表头
            }
        }

        /// <summary>
        /// 从 CSV 文件加载事件列表（容错：跳过表头与坏行）。支持多文件合并加载。
        /// </summary>
        public static List<StepEvent> Load(string path)
        {
            var result = new List<StepEvent>();
            try
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("时间")) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length != 3) continue;
                    DateTime t;
                    int station;
                    short step;
                    if (!DateTime.TryParseExact(parts[0].Trim(), "yyyy-MM-dd HH:mm:ss.fff",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out t)) continue;
                    if (!int.TryParse(parts[1].Trim(), out station) || !short.TryParse(parts[2].Trim(), out step)) continue;
                    result.Add(new StepEvent(t, station, step));
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 加载多个文件合并（按时间排序）。用于主界面/趋势图多选历史文件。
        /// </summary>
        public static List<StepEvent> LoadMultiple(string[] paths)
        {
            var result = new List<StepEvent>();
            foreach (string p in paths)
            {
                result.AddRange(Load(p));
            }
            result.Sort((a, b) => a.Time.CompareTo(b.Time));
            return result;
        }
    }
}