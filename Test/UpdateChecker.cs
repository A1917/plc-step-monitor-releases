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

        static UpdateChecker()
        {
            // 全局启用 TLS 1.2 / 1.3（.NET 4.7.2 默认未启用，GitHub 要求）
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        private static bool _checking;   // 防重复点击

        /// <summary>
        /// 检查最新 Release（异步）。回调：hasUpdate, versionTag, downloadUrl(或错误信息)。
        /// </summary>
        public static void CheckAsync(Action<bool, string, string> callback)
        {
            if (_checking)
            {
                callback(false, null, "正在检查中，请稍候...");
                return;
            }
            _checking = true;
            using (var wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.UserAgent, "PLCStepMonitor");
                wc.DownloadStringCompleted += (s, e) =>
                {
                    _checking = false;
                    if (e.Error != null)
                    {
                        callback(false, null, "网络异常：" + e.Error.Message);
                        return;
                    }
                    try
                    {
                        string json = e.Result;
                        string tag = ExtractJsonValue(json, "tag_name");
                        string url = ExtractDownloadUrl(json);
                        string currentVer = "v" + Application.ProductVersion;

                        if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(url) && tag != currentVer)
                            callback(true, tag, url);
                        else
                            callback(false, currentVer, null);
                    }
                    catch (Exception ex)
                    {
                        callback(false, null, "解析失败：" + ex.Message);
                    }
                };
                wc.DownloadStringAsync(new Uri(ApiUrl));
            }
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
                Directory.CreateDirectory(tempDir);
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(downloadUrl, zipPath);
                }

                string extractDir = Path.Combine(tempDir, "extracted");
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                string newExe = Path.Combine(extractDir, "PLCStepMonitor", "Test.exe");
                if (!File.Exists(newExe))
                    newExe = Path.Combine(extractDir, "Test.exe");
                if (!File.Exists(newExe))
                {
                    MessageBox.Show("更新包中未找到 Test.exe", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string batPath = Path.Combine(tempDir, "update.bat");
                string batContent =
                    "@echo off\r\n" +
                    "timeout /t 3 /nobreak >nul\r\n" +
                    "copy /y \"" + newExe + "\" \"" + Path.Combine(exeDir, "Test.exe") + "\"\r\n" +
                    "start \"\" \"" + Path.Combine(exeDir, "Test.exe") + "\"\r\n" +
                    "del \"%~f0\"\r\n";
                File.WriteAllText(batPath, batContent, Encoding.GetEncoding(936));

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

        private static string ExtractDownloadUrl(string json)
        {
            return ExtractJsonValue(json, "browser_download_url");
        }
    }
}