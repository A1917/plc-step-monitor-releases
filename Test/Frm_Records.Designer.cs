using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    partial class Frm_Records
    {
        private IContainer components = null;
        private Chart chart1;
        private Label lblCursorInfo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.lblCursorInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            base.SuspendLayout();
            ChartArea chartArea1 = new ChartArea();
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            Legend legend1 = new Legend();
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(800, 450);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            this.lblCursorInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCursorInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCursorInfo.Location = new System.Drawing.Point(0, 425);
            this.lblCursorInfo.Name = "lblCursorInfo";
            this.lblCursorInfo.Size = new System.Drawing.Size(800, 25);
            this.lblCursorInfo.TabIndex = 1;
            this.lblCursorInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(800, 450);
            base.Controls.Add(this.lblCursorInfo);
            base.Controls.Add(this.chart1);
            base.Name = "Frm_Records";
            this.Text = "工位趋势图";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            base.ResumeLayout(false);
        }
    }
}
