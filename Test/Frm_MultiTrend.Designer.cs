using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    partial class Frm_MultiTrend
    {
        private IContainer components = null;
        private Panel panelLeft;
        private CheckedListBox chkStations;
        private Chart chart1;
        private Panel panelBottom;
        private Label lblInfo;
        private Button btnFit;
        private Button btnSelectAll;
        private Button btnPrev;
        private Button btnNext;
        private Label lblPage;
        private Label lblPageSec;
        private NumericUpDown nudPageSec;
        private ComboBox cmbRefresh;
        private CheckBox chkRange;
        private CheckBox chkCursor;
        private CheckBox chkLock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.chart1 = new Chart();
            this.panelLeft = new Panel();
            this.chkStations = new CheckedListBox();
            this.panelBottom = new Panel();
            this.btnSelectAll = new Button();
            this.btnFit = new Button();
            this.chkRange = new CheckBox();
            this.chkCursor = new CheckBox();
            this.chkLock = new CheckBox();
            this.btnPrev = new Button();
            this.btnNext = new Button();
            this.lblPage = new Label();
            this.lblPageSec = new Label();
            this.nudPageSec = new NumericUpDown();
            this.cmbRefresh = new ComboBox();
            this.lblInfo = new Label();
            this.panelLeft.SuspendLayout();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPageSec)).BeginInit();
            base.SuspendLayout();

            ChartArea ca = new ChartArea();
            ca.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(ca);
            Legend leg = new Legend();
            leg.Name = "Legend1";
            this.chart1.Legends.Add(leg);
            this.chart1.Dock = DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(160, 0);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(740, 435);
            this.chart1.TabIndex = 0;

            this.panelLeft.Dock = DockStyle.Left;
            this.panelLeft.Width = 160;
            this.panelLeft.Controls.Add(this.chkStations);
            this.chkStations.Dock = DockStyle.Fill;
            this.chkStations.CheckOnClick = true;
            this.chkStations.TabIndex = 0;

            this.panelBottom.Dock = DockStyle.Bottom;
            this.panelBottom.Height = 65;
            this.panelBottom.Controls.Add(this.lblInfo);
            this.panelBottom.Controls.Add(this.cmbRefresh);
            this.panelBottom.Controls.Add(this.nudPageSec);
            this.panelBottom.Controls.Add(this.lblPageSec);
            this.panelBottom.Controls.Add(this.lblPage);
            this.panelBottom.Controls.Add(this.btnNext);
            this.panelBottom.Controls.Add(this.btnPrev);
            this.panelBottom.Controls.Add(this.chkLock);
            this.panelBottom.Controls.Add(this.chkCursor);
            this.panelBottom.Controls.Add(this.chkRange);
            this.panelBottom.Controls.Add(this.btnFit);
            this.panelBottom.Controls.Add(this.btnSelectAll);

            this.btnSelectAll.Location = new System.Drawing.Point(12, 5);
            this.btnSelectAll.Size = new System.Drawing.Size(70, 23);
            this.btnSelectAll.TabIndex = 0;
            this.btnSelectAll.Text = "全选/不选";

            this.btnFit.Location = new System.Drawing.Point(90, 5);
            this.btnFit.Size = new System.Drawing.Size(60, 23);
            this.btnFit.TabIndex = 1;
            this.btnFit.Text = "适应";

            this.chkRange.AutoSize = true;
            this.chkRange.Location = new System.Drawing.Point(162, 7);
            this.chkRange.Size = new System.Drawing.Size(60, 19);
            this.chkRange.Text = "区域";
            this.chkCursor.AutoSize = true;
            this.chkCursor.Checked = true;
            this.chkCursor.Location = new System.Drawing.Point(100, 7);
            this.chkCursor.Size = new System.Drawing.Size(60, 19);
            this.chkCursor.Text = "游标";

            this.chkLock.AutoSize = true;
            this.chkLock.Location = new System.Drawing.Point(228, 7);
            this.chkLock.Size = new System.Drawing.Size(60, 19);
            this.chkLock.Text = "锁定";

            this.btnPrev.Location = new System.Drawing.Point(300, 35);
            this.btnPrev.Size = new System.Drawing.Size(40, 23);
            this.btnPrev.TabIndex = 4;
            this.btnPrev.Text = "◀";

            this.btnNext.Location = new System.Drawing.Point(346, 35);
            this.btnNext.Size = new System.Drawing.Size(40, 23);
            this.btnNext.TabIndex = 5;
            this.btnNext.Text = "▶";

            this.lblPage.AutoSize = true;
            this.lblPage.Location = new System.Drawing.Point(396, 39);
            this.lblPage.Size = new System.Drawing.Size(180, 15);
            this.lblPage.Text = "--:--:-- ~ --:--:--";

            this.lblPageSec.AutoSize = true;
            this.lblPageSec.Location = new System.Drawing.Point(580, 39);
            this.lblPageSec.TabIndex = 6;
            this.lblPageSec.Text = "每页(s):";

            this.nudPageSec.Location = new System.Drawing.Point(640, 35);
            this.nudPageSec.Size = new System.Drawing.Size(60, 25);
            this.nudPageSec.Minimum = 1;
            this.nudPageSec.Maximum = 3600;
            this.nudPageSec.Value = 60;

            this.cmbRefresh.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRefresh.Items.AddRange(new object[] { "60fps", "30fps", "10fps" });
            this.cmbRefresh.Location = new System.Drawing.Point(710, 35);
            this.cmbRefresh.Size = new System.Drawing.Size(90, 23);
            this.cmbRefresh.SelectedIndex = 0;

            this.lblInfo.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.lblInfo.BorderStyle = BorderStyle.FixedSingle;
            this.lblInfo.Location = new System.Drawing.Point(12, 5);
            this.lblInfo.Size = new System.Drawing.Size(0, 0);   // 在代码中动态设置

            base.ClientSize = new System.Drawing.Size(900, 500);
            base.Controls.Add(this.chart1);
            base.Controls.Add(this.panelBottom);
            base.Controls.Add(this.panelLeft);
            base.Name = "Frm_MultiTrend";
            this.Text = "多工位总览趋势图";
            this.panelLeft.ResumeLayout(false);
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPageSec)).EndInit();
            base.ResumeLayout(false);
        }
    }
}