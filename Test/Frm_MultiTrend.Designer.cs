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
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.panelLeft.SuspendLayout();
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
            this.chkStations.Size = new System.Drawing.Size(160, 524);
            this.chkStations.TabIndex = 0;

            // chart1
            this.chart1.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.chart1.Location = new System.Drawing.Point(160, 0);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(640, 524);
            this.chart1.TabIndex = 1;
            this.chart1.Text = "chart1";
            ChartArea ca = new ChartArea();
            this.chart1.ChartAreas.Add(ca);
            Legend leg = new Legend();
            this.chart1.Legends.Add(leg);

            // lblInfo
            this.lblInfo.BorderStyle = BorderStyle.FixedSingle;
            this.lblInfo.Dock = DockStyle.Bottom;
            this.lblInfo.Location = new System.Drawing.Point(0, 524);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(800, 22);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "勾选左侧工位查看趋势";

            // Frm_MultiTrend
            this.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 546);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.lblInfo);
            this.Name = "Frm_MultiTrend";
            this.Text = "多工位趋势图";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panelLeft.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}