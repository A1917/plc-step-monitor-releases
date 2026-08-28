using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Test
{
    /// <summary>
    /// 自动更新：从 GitHub Release 检查/下载/替换/重启。
    /// </summary>
    public static class UpdateChecker
    {
        private const string ApiUrl = "https://api.github.com/repos/A1917/plc-step-monitor/releases/latest";
        private const string UserAgent = "PLCStepMonitor-Updater/1.0";

        /// <summary>
        /// 检查最新 Release（异步，防止阻塞 UI）。返回 (有新版本, 版本号, 下载URL)。
        /// </summary>
        public static void CheckAsync(Action<bool, string, string> callback)
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                try
                {
                    var req = WebRequest.CreateHttp(ApiUrl);
                    req.UserAgent = UserAgent;
                    req.Timeout = 15000;
                    using (var resp = req.GetResponse())
                    using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        // 简单解析 JSON（不依赖外部库）
                        string tag = ExtractJsonValue(json, "tag_name");
                        string url = ExtractJsonValue(json, "browser_download_url");
                        string currentVer = "v" + Application.ProductVersion;

                        if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(url) && tag != currentVer)
                        {
                            e.Result = new Tuple<bool, string, string>(true, tag, url);
                        }
                        else
                        {
                            e.Result = new Tuple<bool, string, string>(false, currentVer, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    e.Result = new Tuple<bool, string, string>(false, null, ex.Message);
                }
            };
            bw.RunWorkerCompleted += (s, e) =>
            {
                var result = (Tuple<bool, string, string>)e.Result;
                callback(result.Item1, result.Item2, result.Item3);
            };
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// 下载更新并执行替换 + 重启。
        /// </summary>
        public static void DownloadAndApply(string downloadUrl, string versionTag)
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string tempDir = Path.Combine(Path.GetTempPath(), "PLCStepUpdate_" + versionTag);
            string zipPath = Path.Combine(tempDir, "update.zip");

            try
            {
                // 下载
                Directory.CreateDirectory(tempDir);
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(downloadUrl, zipPath);
                }

                // 解压
                string extractDir = Path.Combine(tempDir, "extracted");
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                // 找新 exe
                string newExe = Path.Combine(extractDir, "PLCStepMonitor", "Test.exe");
                if (!File.Exists(newExe))
                {
                    // 兼容旧版 zip 结构（exe 可能直接在根目录）
                    newExe = Path.Combine(extractDir, "Test.exe");
                    if (!File.Exists(newExe))
                    {
                        MessageBox.Show("更新包中未找到 Test.exe", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // 写 bat 脚本：等待旧进程退出 → 复制新 exe → 启动 → 自删
                string batPath = Path.Combine(tempDir, "update.bat");
                string batContent =
                    "@echo off\r\n" +
                    "timeout /t 3 /nobreak >nul\r\n" +
                    "copy /y \"" + newExe + "\" \"" + Path.Combine(exeDir, "Test.exe") + "\"\r\n" +
                    "start \"\" \"" + Path.Combine(exeDir, "Test.exe") + "\"\r\n" +
                    "del \"%~f0\"\r\n";
                File.WriteAllText(batPath, batContent, Encoding.GetEncoding(936));   // GBK 编码，cmd 兼容

                // 启动 bat 并退出
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"" + batPath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新失败：" + ex.Message, "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>从简单 JSON 中提取指定键的字符串值（限一层）</summary>
        private static string ExtractJsonValue(string json, string key)
        {
            string search = "\"" + key + "\":";
            int idx = json.IndexOf(search);
            if (idx < 0) return null;
            idx += search.Length;
            while (idx < json.Length && (json[idx] == ' ' || json[idx] == '"')) idx++;
            int end = json.IndexOf('"', idx);
            if (end < 0) return null;
            return json.Substring(idx, end - idx);
        }
    }
}