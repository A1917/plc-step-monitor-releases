using System.ComponentModel;
using System.Windows.Forms;

namespace Test
{
    partial class Frm_StepInfo
    {
        private IContainer components = null;
        private TableLayoutPanel tblayout_Step;

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
            this.tblayout_Step = new System.Windows.Forms.TableLayoutPanel();
            base.SuspendLayout();
            this.tblayout_Step.BackColor = System.Drawing.Color.FromArgb(192, 255, 192);
            this.tblayout_Step.ColumnCount = 2;
            this.tblayout_Step.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
            this.tblayout_Step.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
            this.tblayout_Step.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblayout_Step.Location = new System.Drawing.Point(0, 0);
            this.tblayout_Step.Margin = new System.Windows.Forms.Padding(4);
            this.tblayout_Step.Name = "tblayout_Step";
            this.tblayout_Step.RowCount = 2;
            this.tblayout_Step.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
            this.tblayout_Step.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
            this.tblayout_Step.Size = new System.Drawing.Size(1067, 562);
            this.tblayout_Step.TabIndex = 5;
            base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 15f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(1067, 562);
            base.Controls.Add(this.tblayout_Step);
            base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            base.Margin = new System.Windows.Forms.Padding(4);
            base.Name = "Frm_StepInfo";
            base.ShowInTaskbar = false;
            this.Text = "Frm_StepInfo";
            base.Load += new System.EventHandler(Frm_StepInfo_Load);
            base.ResumeLayout(false);
        }
    }
}
