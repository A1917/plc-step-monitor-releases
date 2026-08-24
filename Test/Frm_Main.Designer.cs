using System.ComponentModel;
using System.Windows.Forms;

namespace Test
{
    partial class Frm_Main
    {
        private IContainer components = null;
        private Button btnConnect;
        private Label lbConnectState;
        private System.Windows.Forms.Timer timer1;
        private Label label1;
        private Panel panel1;
        private Label lbl_MachineState;
        private Label label2;
        private TextBox textBox1;
        private Label lblPlcIp;
        private TextBox txtPlcIp;
        private CheckBox chkSimulate;
        private CheckBox chkRecord;
        private Button btnLoadHistory;
        private Button btnMultiTrend;

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
            this.components = new System.ComponentModel.Container();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lbConnectState = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbl_MachineState = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblPlcIp = new System.Windows.Forms.Label();
            this.txtPlcIp = new System.Windows.Forms.TextBox();
            this.chkSimulate = new System.Windows.Forms.CheckBox();
            this.chkRecord = new System.Windows.Forms.CheckBox();
            this.btnLoadHistory = new System.Windows.Forms.Button();
            this.btnMultiTrend = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            base.SuspendLayout();
            this.btnConnect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnConnect.Location = new System.Drawing.Point(1081, 19);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(4);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(129, 26);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(btnConnect_Click);
            this.lbConnectState.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lbConnectState.Location = new System.Drawing.Point(1232, 22);
            this.lbConnectState.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbConnectState.Name = "lbConnectState";
            this.lbConnectState.Size = new System.Drawing.Size(100, 26);
            this.lbConnectState.TabIndex = 1;
            this.lbConnectState.Text = "未连接";
            this.lbConnectState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.timer1.Tick += new System.EventHandler(CheckHeartBeat);
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.label1.BackColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(1267, 1);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "心跳";
            this.panel1.Controls.Add(this.btnMultiTrend);
            this.panel1.Controls.Add(this.btnLoadHistory);
            this.panel1.Controls.Add(this.chkRecord);
            this.panel1.Controls.Add(this.chkSimulate);
            this.panel1.Controls.Add(this.txtPlcIp);
            this.panel1.Controls.Add(this.lblPlcIp);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.textBox1);
            this.panel1.Controls.Add(this.lbl_MachineState);
            this.panel1.Controls.Add(this.lbConnectState);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnConnect);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1336, 53);
            this.panel1.TabIndex = 4;
            this.lbl_MachineState.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lbl_MachineState.Location = new System.Drawing.Point(938, 19);
            this.lbl_MachineState.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_MachineState.Name = "lbl_MachineState";
            this.lbl_MachineState.Size = new System.Drawing.Size(100, 26);
            this.lbl_MachineState.TabIndex = 5;
            this.lbl_MachineState.Text = "未连接";
            this.lbl_MachineState.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.textBox1.Location = new System.Drawing.Point(858, 19);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(64, 25);
            this.textBox1.TabIndex = 6;
            this.textBox1.Text = "50";
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(785, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "工位数:";
            this.lblPlcIp.AutoSize = true;
            this.lblPlcIp.Location = new System.Drawing.Point(15, 24);
            this.lblPlcIp.Name = "lblPlcIp";
            this.lblPlcIp.Size = new System.Drawing.Size(59, 15);
            this.lblPlcIp.TabIndex = 8;
            this.lblPlcIp.Text = "PLC IP:";
            this.txtPlcIp.Location = new System.Drawing.Point(78, 19);
            this.txtPlcIp.Name = "txtPlcIp";
            this.txtPlcIp.Size = new System.Drawing.Size(130, 25);
            this.txtPlcIp.TabIndex = 9;
            this.txtPlcIp.Text = "192.168.1.50";
            this.chkSimulate.AutoSize = true;
            this.chkSimulate.Location = new System.Drawing.Point(228, 23);
            this.chkSimulate.Name = "chkSimulate";
            this.chkSimulate.Size = new System.Drawing.Size(89, 19);
            this.chkSimulate.TabIndex = 10;
            this.chkSimulate.Text = "模拟数据";
            this.chkSimulate.UseVisualStyleBackColor = true;
            this.chkSimulate.CheckedChanged += new System.EventHandler(chkSimulate_CheckedChanged);
            this.chkRecord.AutoSize = true;
            this.chkRecord.Location = new System.Drawing.Point(320, 23);
            this.chkRecord.Name = "chkRecord";
            this.chkRecord.Size = new System.Drawing.Size(60, 19);
            this.chkRecord.TabIndex = 11;
            this.chkRecord.Text = "记录";
            this.chkRecord.UseVisualStyleBackColor = true;
            this.chkRecord.CheckedChanged += new System.EventHandler(chkRecord_CheckedChanged);
            this.btnLoadHistory.Location = new System.Drawing.Point(390, 21);
            this.btnLoadHistory.Name = "btnLoadHistory";
            this.btnLoadHistory.Size = new System.Drawing.Size(60, 25);
            this.btnLoadHistory.TabIndex = 12;
            this.btnLoadHistory.Text = "加载";
            this.btnLoadHistory.UseVisualStyleBackColor = true;
            this.btnLoadHistory.Click += new System.EventHandler(btnLoadHistory_Click);
            this.btnMultiTrend.Location = new System.Drawing.Point(460, 21);
            this.btnMultiTrend.Name = "btnMultiTrend";
            this.btnMultiTrend.Size = new System.Drawing.Size(80, 25);
            this.btnMultiTrend.TabIndex = 13;
            this.btnMultiTrend.Text = "总览趋势";
            this.btnMultiTrend.UseVisualStyleBackColor = true;
            this.btnMultiTrend.Click += new System.EventHandler(btnMultiTrend_Click);
            base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(1336, 778);
            base.Controls.Add(this.panel1);
            base.IsMdiContainer = true;
            base.Margin = new System.Windows.Forms.Padding(4);
            base.Name = "Frm_Main";
            this.Text = "Form1";
            base.Load += new System.EventHandler(Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            base.ResumeLayout(false);
        }
    }
}
