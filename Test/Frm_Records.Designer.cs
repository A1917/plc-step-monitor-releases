using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    partial class Frm_Records
    {
        private IContainer components = null;
        private Chart chart1;
        private Panel panelBottom;
        private Button btnPrev;
        private Button btnNext;
        private Label lblPage;
        private CheckBox chkCursor;
        private CheckBox chkRange;
        private Label lblCursorInfo;
        private Label lblPageMin;
        private NumericUpDown nudPageMin;
        private Button btnFit;

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
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.chkCursor = new System.Windows.Forms.CheckBox();
            this.chkRange = new System.Windows.Forms.CheckBox();
            this.lblCursorInfo = new System.Windows.Forms.Label();
            this.lblPageMin = new System.Windows.Forms.Label();
            this.nudPageMin = new System.Windows.Forms.NumericUpDown();
            this.btnFit = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPageMin)).BeginInit();
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
            this.panelBottom.Controls.Add(this.btnFit);
            this.panelBottom.Controls.Add(this.nudPageMin);
            this.panelBottom.Controls.Add(this.lblPageMin);
            this.panelBottom.Controls.Add(this.lblCursorInfo);
            this.panelBottom.Controls.Add(this.chkRange);
            this.panelBottom.Controls.Add(this.chkCursor);
            this.panelBottom.Controls.Add(this.lblPage);
            this.panelBottom.Controls.Add(this.btnNext);
            this.panelBottom.Controls.Add(this.btnPrev);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 425);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(800, 60);
            this.panelBottom.TabIndex = 1;
            this.btnPrev.Location = new System.Drawing.Point(12, 8);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(40, 25);
            this.btnPrev.TabIndex = 0;
            this.btnPrev.Text = "◀";
            this.btnPrev.UseVisualStyleBackColor = true;
            this.btnNext.Location = new System.Drawing.Point(58, 8);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(40, 25);
            this.btnNext.TabIndex = 1;
            this.btnNext.Text = "▶";
            this.btnNext.UseVisualStyleBackColor = true;
            this.lblPage.AutoSize = true;
            this.lblPage.Location = new System.Drawing.Point(108, 13);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(180, 15);
            this.lblPage.TabIndex = 2;
            this.lblPage.Text = "窗口: --:--:-- ~ --:--:--";
            this.chkCursor.AutoSize = true;
            this.chkCursor.Location = new System.Drawing.Point(300, 11);
            this.chkCursor.Name = "chkCursor";
            this.chkCursor.Size = new System.Drawing.Size(60, 19);
            this.chkCursor.TabIndex = 3;
            this.chkCursor.Text = "游标";
            this.chkCursor.UseVisualStyleBackColor = true;
            this.chkRange.AutoSize = true;
            this.chkRange.Location = new System.Drawing.Point(368, 11);
            this.chkRange.Name = "chkRange";
            this.chkRange.Size = new System.Drawing.Size(60, 19);
            this.chkRange.TabIndex = 4;
            this.chkRange.Text = "区域";
            this.chkRange.UseVisualStyleBackColor = true;
            this.lblCursorInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCursorInfo.Location = new System.Drawing.Point(12, 36);
            this.lblCursorInfo.Name = "lblCursorInfo";
            this.lblCursorInfo.Size = new System.Drawing.Size(776, 20);
            this.lblCursorInfo.TabIndex = 5;
            this.lblCursorInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPageMin.AutoSize = true;
            this.lblPageMin.Location = new System.Drawing.Point(440, 13);
            this.lblPageMin.Name = "lblPageMin";
            this.lblPageMin.Size = new System.Drawing.Size(45, 15);
            this.lblPageMin.TabIndex = 6;
            this.lblPageMin.Text = "每页:";
            this.nudPageMin.Location = new System.Drawing.Point(488, 8);
            this.nudPageMin.Name = "nudPageMin";
            this.nudPageMin.Size = new System.Drawing.Size(50, 25);
            this.nudPageMin.TabIndex = 7;
            this.nudPageMin.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPageMin.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudPageMin.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.btnFit.Location = new System.Drawing.Point(550, 7);
            this.btnFit.Name = "btnFit";
            this.btnFit.Size = new System.Drawing.Size(60, 25);
            this.btnFit.TabIndex = 8;
            this.btnFit.Text = "适应";
            this.btnFit.UseVisualStyleBackColor = true;
            base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(800, 485);
            base.Controls.Add(this.chart1);
            base.Controls.Add(this.panelBottom);
            base.Name = "Frm_Records";
            this.Text = "工位趋势图";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPageMin)).EndInit();
            base.ResumeLayout(false);
        }
    }
}