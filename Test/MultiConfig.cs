using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// 总览图配置持久化：勾选工位/颜色/窗口位置，存为 records/multi_config.txt。
    /// 格式：每行 key: value。
    /// </summary>
    public static class MultiConfig
    {
        public static string ConfigPath => Path.Combine(RecordStore.RecordsDir, "multi_config.txt");

        public static void Save(int[] stations, Dictionary<int, Color> colors, Rectangle? window)
        {
            var sb = new StringBuilder();
            if (stations.Length > 0)
                sb.AppendLine("stations: " + string.Join(",", stations));
            foreach (var kv in colors)
                sb.AppendLine($"color_{kv.Key}: #{kv.Value.R:X2}{kv.Value.G:X2}{kv.Value.B:X2}");
            if (window.HasValue)
                sb.AppendLine($"window: {window.Value.X},{window.Value.Y},{window.Value.Width},{window.Value.Height}");
            try { File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8); } catch { }
        }

        public static (int[] stations, Dictionary<int, Color> colors, Rectangle? window) Load()
        {
            if (!File.Exists(ConfigPath))
                return (new int[0], new Dictionary<int, Color>(), null);
            try
            {
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
                var stations = new List<int>();
                var colors = new Dictionary<int, Color>();
                Rectangle? win = null;
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    int colon = line.IndexOf(':');
                    if (colon < 0) continue;
                    string key = line.Substring(0, colon).Trim();
                    string val = line.Substring(colon + 1).Trim();
                    if (key == "stations")
                    {
                        foreach (string s in val.Split(','))
                            if (int.TryParse(s.Trim(), out int st)) stations.Add(st);
                    }
                    else if (key.StartsWith("color_"))
                    {
                        if (int.TryParse(key.Substring(6), out int st) && val.Length >= 7)
                        {
                            colors[st] = Color.FromArgb(
                                int.Parse(val.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                                int.Parse(val.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                                int.Parse(val.Substring(5, 2), System.Globalization.NumberStyles.HexNumber));
                        }
                    }
                    else if (key == "window")
                    {
                        var parts = val.Split(',');
                        if (parts.Length == 4)
                            win = new Rectangle(int.Parse(parts[0]), int.Parse(parts[1]),
                                int.Parse(parts[2]), int.Parse(parts[3]));
                    }
                }
                return (stations.ToArray(), colors, win);
            }
            catch { return (new int[0], new Dictionary<int, Color>(), null); }
        }
    }
}