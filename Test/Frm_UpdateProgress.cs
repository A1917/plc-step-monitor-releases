using System.Windows.Forms;

namespace Test
{
    public partial class Frm_UpdateProgress : Form
    {
        public void SetProgress(int percent, string status)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
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