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
        private static bool _checking;

        static UpdateChecker()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public static void CheckAsync(Action<bool, string, string> callback)
        {
            if (_checking) { callback(false, null, "正在检查中..."); return; }
            _checking = true;
            var bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                try
                {
                    string token = GetGitHubToken();
                    if (string.IsNullOrEmpty(token))
                    {
                        e.Result = new Tuple<bool, string, string>(false, null, "未找到 GitHub 凭据\n请手动到 GitHub Releases 页面下载更新");
                        return;
                    }
                    using (var wc = new WebClient())
                    {
                        wc.Headers.Add(HttpRequestHeader.UserAgent, "PLCStepMonitor");
                        wc.Headers.Add(HttpRequestHeader.Authorization, "token " + token);
                        string json = wc.DownloadString(ApiUrl);
                        string tag = ExtractJsonValue(json, "tag_name");
                        string url = ExtractJsonValue(json, "browser_download_url");
                        string cur = "v" + Application.ProductVersion;
                        if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(url) && tag != cur)
                            e.Result = new Tuple<bool, string, string>(true, tag, url);
                        else
                            e.Result = new Tuple<bool, string, string>(false, cur, null);
                    }
                }
                catch (Exception ex)
                {
                    e.Result = new Tuple<bool, string, string>(false, null, "网络异常：" + ex.Message);
                }
            };
            bw.RunWorkerCompleted += (s, e) =>
            {
                _checking = false;
                var r = (Tuple<bool, string, string>)e.Result;
                callback(r.Item1, r.Item2, r.Item3);
            };
            bw.RunWorkerAsync();
        }

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
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "PLCStepMonitor");
                    string token = GetGitHubToken();
                    if (!string.IsNullOrEmpty(token))
                        wc.Headers.Add(HttpRequestHeader.Authorization, "token " + token);
                    wc.DownloadFile(downloadUrl, zipPath);
                }

                string extractDir = Path.Combine(tempDir, "extracted");
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                string newExe = Path.Combine(extractDir, "PLCStepMonitor", "Test.exe");
                if (!File.Exists(newExe)) newExe = Path.Combine(extractDir, "Test.exe");
                if (!File.Exists(newExe))
                {
                    MessageBox.Show("更新包中未找到 Test.exe", "更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string batPath = Path.Combine(tempDir, "update.bat");
                string batContent =
                    "@echo off\r\ntimeout /t 3 /nobreak >nul\r\n" +
                    "copy /y \"" + newExe + "\" \"" + Path.Combine(exeDir, "Test.exe") + "\"\r\n" +
                    "start \"\" \"" + Path.Combine(exeDir, "Test.exe") + "\"\r\ndel \"%~f0\"\r\n";
                File.WriteAllText(batPath, batContent, Encoding.GetEncoding(936));

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe", Arguments = "/c \"" + batPath + "\"",
                    CreateNoWindow = true, UseShellExecute = false
                });
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新失败：" + ex.Message, "更新错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetGitHubToken()
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo
                    {
                        FileName = "git", Arguments = "credential fill",
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardInput = true, RedirectStandardOutput = true
                    };
                    p.Start();
                    p.StandardInput.WriteLine("protocol=https");
                    p.StandardInput.WriteLine("host=github.com");
                    p.StandardInput.WriteLine();
                    p.StandardInput.Close();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    foreach (string line in output.Split('\n'))
                        if (line.StartsWith("password="))
                            return line.Substring(9).Trim();
                }
            }
            catch { }
            return null;
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string s = "\"" + key + "\":";
            int i = json.IndexOf(s);
            if (i < 0) return null;
            i += s.Length;
            while (i < json.Length && (json[i] == ' ' || json[i] == '"')) i++;
            int e = json.IndexOf('"', i);
            return e < 0 ? null : json.Substring(i, e - i);
        }
    }
}