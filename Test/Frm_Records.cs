using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    /// <summary>
    /// 工位实时趋势图：显示某工位的步号-时间台阶曲线，实时滚动（最近 5 分钟窗口）。
    /// 数据源 = EventStore 内存环形缓冲，非模态打开，不阻塞主界面监控。
    /// </summary>
    public partial class Frm_Records : Form
    {
        private readonly int _station;                // 工位号
        private readonly Timer _timer;                // 500ms 刷新
        private readonly Series _series;              // 步号台阶曲线
        private DateTime _lastPointTime = DateTime.MinValue;

        private const int RefreshIntervalMs = 500;    // 刷新周期
        private const int WindowMinutes = 5;          // 滚动窗口（分钟）

        public Frm_Records(int station)
        {
            InitializeComponent();
            _station = station;
            Text = "工位 " + station + " 实时趋势";

            _series = new Series("步号")
            {
                ChartType = SeriesChartType.StepLine,   // 台阶曲线（步号本来就是台阶）
                BorderWidth = 2,
                Color = Color.SteelBlue,
                XValueType = ChartValueType.DateTime
            };
            chart1.Series.Clear();
            chart1.Series.Add(_series);

            chart1.ChartAreas[0].AxisX.Title = "时间";
            chart1.ChartAreas[0].AxisY.Title = "步号";
            chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;

            _timer = new Timer { Interval = RefreshIntervalMs };
            _timer.Tick += (s, e) => RefreshChart();
            _timer.Start();
            FormClosing += (s, e) => _timer.Stop();
        }

        /// <summary>增量拉取新事件并刷新滚动窗口</summary>
        private void RefreshChart()
        {
            var events = EventStore.GetSince(_station, _lastPointTime);
            foreach (var e in events)
            {
                _series.Points.AddXY(e.Time, e.Step);
            }
            if (events.Count > 0)
            {
                _lastPointTime = events[events.Count - 1].Time;
            }
            if (_series.Points.Count == 0)
            {
                return;
            }
            // 滚动窗口：最近 WindowMinutes 分钟
            DateTime now = DateTime.Now;
            chart1.ChartAreas[0].AxisX.Minimum = now.AddMinutes(-WindowMinutes).ToOADate();
            chart1.ChartAreas[0].AxisX.Maximum = now.ToOADate();
        }
    }
}
