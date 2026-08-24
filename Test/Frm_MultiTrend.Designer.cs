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
        private Label lblInfo;
        private Button btnFit;
        private Button btnSelectAll;
        private Panel panelBottom;
        private CheckBox chkRange;
        private CheckBox chkLock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.chart1 = new Chart();
            this.panelLeft = new Panel();
            this.chkStations = new CheckedListBox();
            this.lblInfo = new Label();
            this.btnFit = new Button();
            this.btnSelectAll = new Button();
            this.panelBottom = new Panel();
            this.chkRange = new CheckBox();
            this.chkLock = new CheckBox();
            this.panelLeft.SuspendLayout();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();

            // panelLeft
            this.panelLeft.Controls.Add(this.chkStations);
            this.panelLeft.Dock = DockStyle.Left;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(160, 550);
            this.panelLeft.TabIndex = 0;

            // chkStations
            this.chkStations.CheckOnClick = true;
            this.chkStations.Dock = DockStyle.Fill;
            this.chkStations.FormattingEnabled = true;
            this.chkStations.Location = new System.Drawing.Point(0, 0);
            this.chkStations.Name = "chkStations";
            this.chkStations.Size = new System.Drawing.Size(160, 514);
            this.chkStations.TabIndex = 0;

            // panelBottom
            this.panelBottom.Controls.Add(this.chkLock);
            this.panelBottom.Controls.Add(this.chkRange);
            this.panelBottom.Controls.Add(this.btnSelectAll);
            this.panelBottom.Controls.Add(this.btnFit);
            this.panelBottom.Controls.Add(this.lblInfo);
            this.panelBottom.Dock = DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 550);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(960, 36);
            this.panelBottom.TabIndex = 1;

            // btnSelectAll
            this.btnSelectAll.Location = new System.Drawing.Point(8, 6);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(80, 25);
            this.btnSelectAll.TabIndex = 0;
            this.btnSelectAll.Text = "全选/全不选";

            // btnFit
            this.btnFit.Location = new System.Drawing.Point(96, 6);
            this.btnFit.Name = "btnFit";
            this.btnFit.Size = new System.Drawing.Size(60, 25);
            this.btnFit.TabIndex = 1;
            this.btnFit.Text = "适应";

            // chkRange
            this.chkRange.AutoSize = true;
            this.chkRange.Location = new System.Drawing.Point(300, 10);
            this.chkRange.Name = "chkRange";
            this.chkRange.Size = new System.Drawing.Size(60, 19);
            this.chkRange.TabIndex = 2;
            this.chkRange.Text = "区域";

            // chkLock
            this.chkLock.AutoSize = true;
            this.chkLock.Location = new System.Drawing.Point(368, 10);
            this.chkLock.Name = "chkLock";
            this.chkLock.Size = new System.Drawing.Size(60, 19);
            this.chkLock.TabIndex = 3;
            this.chkLock.Text = "锁定";

            // lblInfo
            this.lblInfo.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.lblInfo.BorderStyle = BorderStyle.FixedSingle;
            this.lblInfo.Location = new System.Drawing.Point(165, 8);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(780, 24);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "游标";

            // chart1
            this.chart1.Dock = DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(160, 0);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(800, 550);
            this.chart1.TabIndex = 2;
            this.chart1.Text = "chart1";
            ChartArea ca = new ChartArea();
            this.chart1.ChartAreas.Add(ca);
            Legend leg = new Legend();
            this.chart1.Legends.Add(leg);

            // Frm_MultiTrend
            this.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(960, 586);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelBottom);
            this.Name = "Frm_MultiTrend";
            this.Text = "多工位趋势图";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelLeft.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}