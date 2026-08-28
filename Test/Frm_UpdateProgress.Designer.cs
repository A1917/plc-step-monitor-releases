using System.ComponentModel;
using System.Windows.Forms;

namespace Test
{
    partial class Frm_UpdateProgress
    {
        private IContainer components = null;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnCancel;

        public bool Cancelled { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.progressBar = new ProgressBar();
            this.lblStatus = new Label();
            this.btnCancel = new Button();
            base.SuspendLayout();

            this.progressBar.Location = new System.Drawing.Point(12, 12);
            this.progressBar.Size = new System.Drawing.Size(360, 23);
            this.progressBar.Style = ProgressBarStyle.Marquee;

            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 45);
            this.lblStatus.Size = new System.Drawing.Size(200, 15);
            this.lblStatus.Text = "正在下载...";

            this.btnCancel.Location = new System.Drawing.Point(297, 42);
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += (s, e) => { Cancelled = true; Close(); };

            base.ClientSize = new System.Drawing.Size(384, 76);
            base.Controls.Add(this.btnCancel);
            base.Controls.Add(this.lblStatus);
            base.Controls.Add(this.progressBar);
            base.FormBorderStyle = FormBorderStyle.FixedDialog;
            base.MaximizeBox = false;
            base.MinimizeBox = false;
            base.StartPosition = FormStartPosition.CenterScreen;
            base.Text = "下载更新";
            base.ResumeLayout(false);
            base.PerformLayout();
        }
    }
}