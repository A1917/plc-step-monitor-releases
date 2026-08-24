using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Test
{
    /// <summary>
    /// 工位监控子窗体：以网格实时显示各工位当前流程步号（一个寄存器 = 一个工位）。
    /// 工位数由主界面配置，连接时通过 ApplyConfig 动态重建网格。
    /// </summary>
    public partial class Frm_StepInfo : Form
    {
        private const int PollIntervalMs = 100;            // 轮询周期：10 次/秒
        private const int GridColumns = 10;                // 网格固定 10 列

        private Thread _thdUpdate;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Label[] labels = new Label[PlcData.StepCount];
        private short[] lastStep = new short[PlcData.StepCount];

        public Frm_StepInfo()
        {
            InitializeComponent();
            RebuildGrid(PlcData.StepCount);
        }

        /// <summary>
        /// 应用主界面配置（工位数）：更新 PlcData 并重建数据数组与网格。
        /// 由主窗体在连接前调用。
        /// </summary>
        public void ApplyConfig(int stepCount)
        {
            PlcData.StepCount = stepCount;
            PlcData.ThdStep = new short[stepCount];
            RebuildGrid(stepCount);
        }

        /// <summary>按工位数重建网格（固定 10 列，行数自适应）</summary>
        private void RebuildGrid(int count)
        {
            int rows = (count + GridColumns - 1) / GridColumns;

            tblayout_Step.SuspendLayout();
            tblayout_Step.Controls.Clear();
            tblayout_Step.RowStyles.Clear();
            tblayout_Step.ColumnStyles.Clear();
            tblayout_Step.RowCount = rows;
            tblayout_Step.ColumnCount = GridColumns;

            float rowPercent = 100f / rows;
            for (int r = 0; r < rows; r++)
            {
                tblayout_Step.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
            }
            for (int c = 0; c < GridColumns; c++)
            {
                tblayout_Step.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / GridColumns));
            }

            labels = new Label[count];
            lastStep = new short[count];
            for (int i = 0; i < count; i++)
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
                int idx = i;   // 闭包捕获
                labels[i].DoubleClick += (s, e) => OpenTrend(idx);
                tblayout_Step.Controls.Add(labels[i], i % GridColumns, i / GridColumns);
            }
            tblayout_Step.ResumeLayout();
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
                        var result = PlcData.omronFinsNet.ReadInt16("D10002", (ushort)PlcData.StepCount);
                        if (result.IsSuccess)
                        {
                            short[] content = result.Content;
                            int simStation = PlcData.StepCount - 1;
                            if (Simulator.IsRunning && simStation >= 0 && simStation < content.Length)
                            {
                                content[simStation] = Simulator.CurrentValue;   // 保持模拟值（防事件冲突/显示跳变）
                            }
                            PlcData.ThdStep = content;
                            EventStore.Feed(content);   // 步号变化事件入缓冲（趋势图数据源）
                            UpdateLabels();
                        }
                    }
                    else if (Simulator.IsRunning)
                    {
                        // 未连接但模拟运行：模拟工位值写入共享数组并刷新网格
                        int sim = PlcData.StepCount - 1;
                        if (sim >= 0 && sim < PlcData.ThdStep.Length)
                        {
                            PlcData.ThdStep[sim] = Simulator.CurrentValue;
                        }
                        UpdateLabels();
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
            int count = Math.Min(lastStep.Length, PlcData.ThdStep.Length);
            for (int i = 0; i < count; i++)
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
                    int simStation = PlcData.StepCount - 1;
                    foreach (int idx in changed)
                    {
                        string prefix = (Simulator.IsRunning && idx == simStation) ? "模拟" + idx : "工位" + idx;
                        labels[idx].Text = prefix + ":\n" + PlcData.ThdStep[idx];
                    }
                });
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>双击工位格子：打开该工位实时趋势图（独立窗口，可自由排列/最大化）</summary>
        private void OpenTrend(int station)
        {
            var frm = new Frm_Records(station);
            frm.Show();   // 独立窗口（非 MDI，避免最大化/关闭影响主窗体布局）
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
