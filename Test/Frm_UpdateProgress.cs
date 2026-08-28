using System.Windows.Forms;

namespace Test
{
    public partial class Frm_UpdateProgress : Form
    {
        public string DownloadUrl;
        public string VersionTag;

        public Frm_UpdateProgress()
        {
            InitializeComponent();
            Load += (s, e) =>
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                UpdateChecker.DownloadAndApplyAsync(DownloadUrl, VersionTag, this);
            };
        }

        public void SetProgress(int percent, string status)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (IsDisposed || progressBar == null || lblStatus == null) return;
                    if (percent <= 0 || percent >= 100)
                    {
                        progressBar.Style = ProgressBarStyle.Marquee;
                    }
                    else
                    {
                        progressBar.Style = ProgressBarStyle.Continuous;
                        progressBar.Value = percent;
                    }
                    lblStatus.Text = status;
                });
            }
            catch { }
        }
    }
}