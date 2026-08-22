namespace Test
{
    partial class StepHistoryItem
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.lbl_Step = new System.Windows.Forms.Label();
            this.tb_Desc = new System.Windows.Forms.TextBox();
            this.lbl_Duration = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbl_Step
            // 
            this.lbl_Step.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_Step.Location = new System.Drawing.Point(3, 0);
            this.lbl_Step.Name = "lbl_Step";
            this.lbl_Step.Size = new System.Drawing.Size(80, 45);
            this.lbl_Step.TabIndex = 0;
            this.lbl_Step.Text = "label1";
            this.lbl_Step.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tb_Desc
            // 
            this.tb_Desc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tb_Desc.Location = new System.Drawing.Point(89, 3);
            this.tb_Desc.Multiline = true;
            this.tb_Desc.Name = "tb_Desc";
            this.tb_Desc.Size = new System.Drawing.Size(274, 39);
            this.tb_Desc.TabIndex = 1;
            this.tb_Desc.WordWrap = false;
            // 
            // lbl_Duration
            // 
            this.lbl_Duration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_Duration.Location = new System.Drawing.Point(369, 0);
            this.lbl_Duration.Name = "lbl_Duration";
            this.lbl_Duration.Size = new System.Drawing.Size(110, 45);
            this.lbl_Duration.TabIndex = 2;
            this.lbl_Duration.Text = "label2";
            this.lbl_Duration.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.01386F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58.25603F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.74768F));
            this.tableLayoutPanel1.Controls.Add(this.lbl_Step, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lbl_Duration, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.tb_Desc, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(482, 45);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // UserControl1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UserControl1";
            this.Size = new System.Drawing.Size(482, 45);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbl_Step;
        private System.Windows.Forms.TextBox tb_Desc;
        private System.Windows.Forms.Label lbl_Duration;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
