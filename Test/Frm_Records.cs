using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    /// <summary>
    /// 工位实时趋势图：某工位的步号-时间台阶曲线。
    /// 打开时全量加载环形缓冲（拖拽查看历史零延迟），定时增量追加；
    /// X 轴绝对时间（毫秒精度），支持滚轮缩放 / Ctrl 拖拽框选缩放 / 滚动条拖拽。
    /// </summary>
    public partial class Frm_Records : Form
    {
        private readonly int _station;                  // 工位号
        private readonly Timer _timer;                  // 500ms 增量刷新
        private readonly Series _series;                // 步号台阶曲线
        private readonly List<StepEvent> _events;       // 已加载事件（全量+增量）
        private DateTime _lastPointTime;                // 增量游标
        private bool _followTail = true;                // 是否跟随最新（实时滚动）

        private const int RefreshIntervalMs = 500;
        private const double DefaultWindowMs = 5 * 60 * 1000;   // 默认可视窗口：5 分钟

        public Frm_Records(int station)
        {
            InitializeComponent();
            _station = station;
            Text = "工位 " + station + " 实时趋势";

            // 全量加载环形缓冲，趋势图可拖拽查看缓冲内任意历史区间
            _events = EventStore.GetAll(station);
            if (_events.Count > 0)
            {
                _lastPointTime = _events[_events.Count - 1].Time;
            }

            _series = new Series("步号")
            {
                ChartType = SeriesChartType.StepLine,
                BorderWidth = 2,
                Color = Color.SteelBlue,
                XValueType = ChartValueType.DateTime,
                IsXValueIndexed = false
            };
            chart1.Series.Clear();
            chart1.Series.Add(_series);

            var area = chart1.ChartAreas[0];
            area.AxisX.Title = "时间";
            area.AxisY.Title = "步号";
            area.AxisY.IsStartedFromZero = false;
            // 时间轴毫秒精度显示
            area.AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            // 鼠标交互：框选缩放 + 缩放后滚动条拖拽
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;
            area.CursorY.IsUserEnabled = true;
            area.CursorY.IsUserSelectionEnabled = true;
            // 十字光标（跟随鼠标，对标工业趋势控件 HslCurve 的交互）
            area.CursorX.LineColor = Color.FromArgb(160, 255, 0, 0);
            area.CursorY.LineColor = Color.FromArgb(160, 255, 0, 0);
            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;
            // 数据点提示：悬停显示 时间(ms) + 步号
            _series.ToolTip = "#VALX{yyyy-MM-dd HH:mm:ss.fff}\n步号 #VALY";
            chart1.MouseMove += OnChartMouseMove;

            chart1.MouseWheel += OnMouseWheel;
            chart1.AxisViewChanged += OnAxisViewChanged;
            chart1.MouseDown += (s, e) => _followTail = false;   // 用户手动操作后停止跟随
            chart1.DoubleClick += (s, e) => FitWindow();         // 双击回到最新实时视图

            RebuildPoints();
            FitWindow();

            _timer = new Timer { Interval = RefreshIntervalMs };
            _timer.Tick += (s, e) => RefreshChart();
            _timer.Start();
            FormClosing += (s, e) => _timer.Stop();
        }

        /// <summary>重建曲线点（全量）</summary>
        private void RebuildPoints()
        {
            chart1.SuspendLayout();
            _series.Points.Clear();
            foreach (var e in _events)
            {
                _series.Points.AddXY(e.Time, e.Step);
            }
            chart1.ResumeLayout();
        }

        /// <summary>默认视图：最近 DefaultWindowMs 毫秒</summary>
        private void FitWindow()
        {
            DateTime end = _events.Count > 0 ? _events[_events.Count - 1].Time : DateTime.Now;
            DateTime start = end.AddMilliseconds(-DefaultWindowMs);
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            view.Position = start.ToOADate();
            view.Size = DefaultWindowMs / 86400000.0;   // OADate 单位：天
            _followTail = true;
        }

        /// <summary>增量拉取新事件并追加；跟随模式下保持窗口贴着最新</summary>
        private void RefreshChart()
        {
            var fresh = EventStore.GetSince(_station, _lastPointTime);
            if (fresh.Count == 0)
            {
                return;
            }
            _lastPointTime = fresh[fresh.Count - 1].Time;
            _events.AddRange(fresh);

            chart1.SuspendLayout();
            foreach (var e in fresh)
            {
                _series.Points.AddXY(e.Time, e.Step);
            }
            if (_followTail)
            {
                var view = chart1.ChartAreas[0].AxisX.ScaleView;
                view.Position = _lastPointTime.AddMilliseconds(-DefaultWindowMs).ToOADate();
            }
            chart1.ResumeLayout();
        }

        /// <summary>滚轮缩放：基于鼠标位置，1.2x 步进；Ctrl+滚轮 或 纯滚轮均生效</summary>
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var area = chart1.ChartAreas[0];
            double zoomFactor = e.Delta > 0 ? 0.8 : 1.25;   // 上滚放大
            var view = area.AxisX.ScaleView;
            double currentSize = view.Size;
            double newSize = Math.Max(currentSize * zoomFactor, 0.5 / 86400000.0);   // 最小 0.5ms
            // 保持鼠标位置下的数据点不动（缩放锚点）
            double mouseTime = area.AxisX.PixelPositionToValue(e.X);
            double left = view.Position;
            double ratio = (mouseTime - left) / currentSize;
            double newLeft = mouseTime - ratio * newSize;
            view.Position = newLeft;
            view.Size = newSize;
            _followTail = false;
        }

        /// <summary>视图变化：用户拖拽滚动条/缩放时停止跟随最新（不重新加载，数据已在内存）</summary>
        private void OnAxisViewChanged(object sender, ViewEventArgs e)
        {
            if (e.Axis.AxisName == AxisName.X && _followTail)
            {
                // 程序主动更新视图（跟随模式）也会触发此事件，忽略
                return;
            }
        }

        /// <summary>鼠标移动：底部信息栏实时显示光标对应的时间(ms)与步号</summary>
        private void OnChartMouseMove(object sender, MouseEventArgs e)
        {
            if (lblCursorInfo == null)
            {
                return;
            }
            var area = chart1.ChartAreas[0];
            try
            {
                double xVal = area.AxisX.PixelPositionToValue(e.X);
                double yVal = area.AxisY.PixelPositionToValue(e.Y);
                if (double.IsNaN(xVal) || double.IsNaN(yVal))
                {
                    lblCursorInfo.Text = string.Empty;
                    return;
                }
                DateTime t = DateTime.FromOADate(xVal);
                lblCursorInfo.Text = "时间: " + t.ToString("HH:mm:ss.fff") + "    步号: " + (short)Math.Round(yVal);
            }
            catch
            {
                lblCursorInfo.Text = string.Empty;
            }
        }
    }
}
