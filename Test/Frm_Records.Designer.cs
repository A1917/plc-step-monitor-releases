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
        private CheckBox chkLockRange;
        private Label lblCursorInfo;
        private Label lblPageSec;
        private NumericUpDown nudPageSec;
        private Button btnFit;
        private Button btnLoad;
        private ComboBox cmbRefresh;
        private CheckBox chkCycle;
        private Panel panelCycle;
        private Button btnCycleBig;
        private Button btnCycleStep;
        private ListView lvCycle;

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
            this.chkLockRange = new System.Windows.Forms.CheckBox();
            this.lblCursorInfo = new System.Windows.Forms.Label();
            this.lblPageSec = new System.Windows.Forms.Label();
            this.nudPageSec = new System.Windows.Forms.NumericUpDown();
            this.btnFit = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.panelCycle = new System.Windows.Forms.Panel();
            this.btnCycleBig = new System.Windows.Forms.Button();
            this.btnCycleStep = new System.Windows.Forms.Button();
            this.lvCycle = new System.Windows.Forms.ListView();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPageSec)).BeginInit();
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
            this.panelBottom.Controls.Add(this.chkCycle);
            this.panelBottom.Controls.Add(this.cmbRefresh);
            this.panelBottom.Controls.Add(this.btnLoad);
            this.panelBottom.Controls.Add(this.btnFit);
            this.panelBottom.Controls.Add(this.nudPageSec);
            this.panelBottom.Controls.Add(this.lblPageSec);
            this.panelBottom.Controls.Add(this.lblCursorInfo);
            this.panelBottom.Controls.Add(this.chkLockRange);
            this.panelBottom.Controls.Add(this.chkRange);
            this.panelBottom.Controls.Add(this.chkCursor);
            this.panelBottom.Controls.Add(this.lblPage);
            this.panelBottom.Controls.Add(this.btnNext);
            this.panelBottom.Controls.Add(this.btnPrev);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 425);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(800, 90);   // 加高到两行
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
            this.chkLockRange.AutoSize = true;
            this.chkLockRange.Location = new System.Drawing.Point(432, 11);
            this.chkLockRange.Name = "chkLockRange";
            this.chkLockRange.Size = new System.Drawing.Size(60, 19);
            this.chkLockRange.TabIndex = 5;
            this.chkLockRange.Text = "锁定";
            this.chkLockRange.UseVisualStyleBackColor = true;
            this.lblCursorInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCursorInfo.Location = new System.Drawing.Point(12, 40);
            this.lblCursorInfo.Name = "lblCursorInfo";
            this.lblCursorInfo.Size = new System.Drawing.Size(500, 24);
            this.lblCursorInfo.TabIndex = 5;
            this.lblCursorInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCursorInfo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblPageSec.AutoSize = true;
            this.lblPageSec.Location = new System.Drawing.Point(505, 13);
            this.lblPageSec.Name = "lblPageSec";
            this.lblPageSec.Size = new System.Drawing.Size(60, 15);
            this.lblPageSec.TabIndex = 6;
            this.lblPageSec.Text = "每页(s):";
            this.nudPageSec.Location = new System.Drawing.Point(565, 8);
            this.nudPageSec.Name = "nudPageSec";
            this.nudPageSec.Size = new System.Drawing.Size(60, 25);
            this.nudPageSec.TabIndex = 7;
            this.nudPageSec.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPageSec.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            this.nudPageSec.Value = new decimal(new int[] { 60, 0, 0, 0 });
            this.btnFit.Location = new System.Drawing.Point(635, 7);
            this.btnFit.Name = "btnFit";
            this.btnFit.Size = new System.Drawing.Size(60, 25);
            this.btnFit.TabIndex = 8;
            this.btnFit.Text = "适应";
            this.btnFit.UseVisualStyleBackColor = true;
            this.btnFit.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnLoad.Location = new System.Drawing.Point(520, 38);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(60, 25);
            this.btnLoad.TabIndex = 9;
            this.btnLoad.Text = "加载";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.cmbRefresh.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRefresh.FormattingEnabled = true;
            this.cmbRefresh.Items.AddRange(new object[] { "60fps", "30fps", "10fps" });
            this.cmbRefresh.Location = new System.Drawing.Point(590, 40);
            this.cmbRefresh.Name = "cmbRefresh";
            this.cmbRefresh.Size = new System.Drawing.Size(90, 23);
            this.cmbRefresh.TabIndex = 10;
            this.cmbRefresh.SelectedIndex = 0;   // 默认 60fps（全局）
            this.cmbRefresh.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.chkCycle.AutoSize = true;
            this.chkCycle.Location = new System.Drawing.Point(690, 42);
            this.chkCycle.Name = "chkCycle";
            this.chkCycle.Size = new System.Drawing.Size(60, 19);
            this.chkCycle.TabIndex = 11;
            this.chkCycle.Text = "周期";
            this.chkCycle.UseVisualStyleBackColor = true;
            // ── 周期详情面板（点击周期线条后显示） ──
            this.panelCycle.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelCycle.Width = 240;
            this.panelCycle.Visible = false;
            this.btnCycleBig.Location = new System.Drawing.Point(8, 6);
            this.btnCycleBig.Size = new System.Drawing.Size(100, 26);
            this.btnCycleBig.Text = "从大到小";
            this.btnCycleBig.UseVisualStyleBackColor = true;
            this.btnCycleStep.Location = new System.Drawing.Point(116, 6);
            this.btnCycleStep.Size = new System.Drawing.Size(100, 26);
            this.btnCycleStep.Text = "按步数顺序";
            this.btnCycleStep.UseVisualStyleBackColor = true;
            this.lvCycle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvCycle.FullRowSelect = true;
            this.lvCycle.GridLines = true;
            this.lvCycle.View = System.Windows.Forms.View.Details;
            this.lvCycle.Columns.Add("步号", 80);
            this.lvCycle.Columns.Add("耗时", 140);
            this.panelCycle.Controls.Add(this.lvCycle);
            this.panelCycle.Controls.Add(this.btnCycleBig);
            this.panelCycle.Controls.Add(this.btnCycleStep);
            base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(800, 515);
            base.Controls.Add(this.panelCycle);
            base.Controls.Add(this.chart1);
            base.Controls.Add(this.panelBottom);
            base.Name = "Frm_Records";
            this.Text = "工位趋势图";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPageSec)).EndInit();
            base.ResumeLayout(false);
        }
    }
}