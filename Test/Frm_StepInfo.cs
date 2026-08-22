using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Test
{
    /// <summary>
    /// 工位监控子窗体：以网格实时显示各工位当前流程步号（一个寄存器 = 一个工位）。
    /// </summary>
    public partial class Frm_StepInfo : Form
    {
        private const int StepCount = PlcData.StepCount;   // 工位数（与 PlcData 一致）
        private const int PollIntervalMs = 100;            // 轮询周期：10 次/秒

        private Thread _thdUpdate;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Label[] labels = new Label[StepCount];
        private readonly short[] lastStep = new short[StepCount];

        public Frm_StepInfo()
        {
            InitializeComponent();
            InitializeStepLayout();
        }

        /// <summary>按工位数初始化 5 行 x 10 列网格</summary>
        private void InitializeStepLayout()
        {
            tblayout_Step.RowCount = 5;
            tblayout_Step.ColumnCount = 10;
            tblayout_Step.RowStyles.Clear();
            tblayout_Step.ColumnStyles.Clear();
            for (int r = 0; r < 5; r++)
            {
                tblayout_Step.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            }
            for (int c = 0; c < 10; c++)
            {
                tblayout_Step.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
            }
            for (int i = 0; i < StepCount; i++)
            {
                labels[i] = new Label
                {
                    Margin = new Padding(2),
                    Font = new Font("微软雅黑", 12f),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle,
                    Text = "工位" + i + ":\n0"
                };
                tblayout_Step.Controls.Add(labels[i], i % 10, i / 10);
            }
        }

        /// <summary>轮询循环：批量读取步值寄存器 D10002 起连续 StepCount 个字</summary>
        private void PollLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    if (PlcData.IsConnected && PlcData.omronFinsNet != null)
                    {
                        var result = PlcData.omronFinsNet.ReadInt16("D10002", StepCount);
                        if (result.IsSuccess)
                        {
                            PlcData.ThdStep = result.Content;
                            UpdateLabels();
                        }
                    }
                }
                catch
                {
                    // 单次轮询失败忽略，下个周期重试
                }
                _cts.Token.WaitHandle.WaitOne(PollIntervalMs);
            }
        }

        /// <summary>差异刷新：仅更新变化的工位标签（批量读一次、UI 更新一次）</summary>
        private void UpdateLabels()
        {
            var changed = new List<int>();
            for (int i = 0; i < StepCount; i++)
            {
                if (lastStep[i] != PlcData.ThdStep[i])
                {
                    changed.Add(i);
                    lastStep[i] = PlcData.ThdStep[i];
                }
            }
            if (changed.Count == 0 || IsDisposed)
            {
                return;
            }
            try
            {
                BeginInvoke((Action)delegate
                {
                    if (IsDisposed)
                    {
                        return;
                    }
                    foreach (int idx in changed)
                    {
                        labels[idx].Text = "工位" + idx + ":\n" + PlcData.ThdStep[idx];
                    }
                });
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void Frm_StepInfo_Load(object sender, EventArgs e)
        {
            FormClosing += Frm_StepInfo_FormClosing;
            _thdUpdate = new Thread(PollLoop) { IsBackground = true };
            _thdUpdate.Start();
        }

        private void Frm_StepInfo_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cts.Cancel();
            if (_thdUpdate != null && _thdUpdate.IsAlive)
            {
                _thdUpdate.Join(2000);
            }
        }
    }
}
