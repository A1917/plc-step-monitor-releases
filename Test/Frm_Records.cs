using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    /// <summary>
    /// 工位实时趋势图：某工位的步号-时间台阶曲线。
    /// 交互：分页浏览（每页 2 分钟）、鼠标锚点缩放（X/Y 双轴）、框选缩放、
    /// 单游标（可拖竖线，显示所停时刻的流程步号）、区域游标（双线高亮，显示区间时长）。
    /// </summary>
    public partial class Frm_Records : Form
    {
        private readonly int _station;
        private readonly Timer _timer;
        private readonly Series _series;
        private readonly List<StepEvent> _events;   // 已加载事件（时间升序）
        private DateTime _lastPointTime;
        private bool _followTail = true;

        private VerticalLineAnnotation _cursor;     // 单游标（橙）
        private VerticalLineAnnotation _curStart;   // 区域开始（蓝）
        private VerticalLineAnnotation _curEnd;     // 区域结束（蓝）
        private RectangleAnnotation _rangeRect;     // 区域高亮

        private const int RefreshIntervalMs = 500;
        private const double PageSizeMs = 2 * 60 * 1000;              // 每页 2 分钟
        private const double PageSizeDays = PageSizeMs / 86400000.0;  // OADate 单位

        public Frm_Records(int station)
        {
            InitializeComponent();
            _station = station;
            Text = "工位 " + station + " 实时趋势";

            // 全量加载环形缓冲
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
            area.AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            // 十字光标（跟随鼠标）
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;
            area.CursorY.IsUserEnabled = true;
            area.CursorY.IsUserSelectionEnabled = true;
            area.CursorX.LineColor = Color.FromArgb(120, 255, 0, 0);
            area.CursorY.LineColor = Color.FromArgb(120, 255, 0, 0);
            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;
            // 数据点提示
            _series.ToolTip = "#VALX{yyyy-MM-dd HH:mm:ss.fff}\n步号 #VALY";

            chart1.MouseWheel += OnMouseWheel;
            chart1.MouseMove += OnChartMouseMove;
            chart1.DoubleClick += (s, e) => FitWindow();

            InitAnnotations();
            btnPrev.Click += (s, e) => PageMove(-1);
            btnNext.Click += (s, e) => PageMove(+1);
            chkCursor.CheckedChanged += (s, e) =>
            {
                _cursor.Visible = chkCursor.Checked;
                if (chkCursor.Checked) UpdateCursorInfo();
            };
            chkRange.CheckedChanged += (s, e) =>
            {
                _curStart.Visible = _curEnd.Visible = _rangeRect.Visible = chkRange.Checked;
                if (chkRange.Checked) UpdateRangeInfo();
            };

            RebuildPoints();
            FitWindow();

            _timer = new Timer { Interval = RefreshIntervalMs };
            _timer.Tick += (s, e) =>
            {
                RefreshChart();
                if (chkCursor.Checked)
                {
                    UpdateCursorInfo();
                }
                if (chkRange.Checked)
                {
                    UpdateRangeInfo();
                }
            };
            _timer.Start();
            FormClosing += (s, e) => _timer.Stop();
        }

        /// <summary>初始化游标注释（默认隐藏，勾选显示）</summary>
        private void InitAnnotations()
        {
            var area = chart1.ChartAreas[0];
            _cursor = new VerticalLineAnnotation
            {
                AxisX = area.AxisX,
                IsInfinitive = true,
                ClipToChartArea = area.Name,
                AllowMoving = true,
                AllowSelecting = false,
                LineColor = Color.Orange,
                LineWidth = 2,
                Visible = false
            };

            _curStart = new VerticalLineAnnotation
            {
                AxisX = area.AxisX,
                IsInfinitive = true,
                ClipToChartArea = area.Name,
                AllowMoving = true,
                AllowSelecting = false,
                LineColor = Color.DeepSkyBlue,
                LineWidth = 2,
                Visible = false
            };
            _curEnd = new VerticalLineAnnotation
            {
                AxisX = area.AxisX,
                IsInfinitive = true,
                ClipToChartArea = area.Name,
                AllowMoving = true,
                AllowSelecting = false,
                LineColor = Color.DeepSkyBlue,
                LineWidth = 2,
                Visible = false
            };

            _rangeRect = new RectangleAnnotation
            {
                AxisX = area.AxisX,
                AxisY = area.AxisY,
                AllowMoving = false,
                AllowResizing = false,
                LineWidth = 0,
                BackColor = Color.FromArgb(50, 100, 180, 255),
                Visible = false
            };

            chart1.Annotations.Add(_cursor);
            chart1.Annotations.Add(_curStart);
            chart1.Annotations.Add(_curEnd);
            chart1.Annotations.Add(_rangeRect);
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

        /// <summary>默认视图：最近 2 分钟（最后一页），并初始化游标位置</summary>
        private void FitWindow()
        {
            DateTime end = _events.Count > 0 ? _events[_events.Count - 1].Time : DateTime.Now;
            DateTime start = end.AddMilliseconds(-PageSizeMs);
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            view.Position = start.ToOADate();
            view.Size = PageSizeDays;

            double mid = view.Position + PageSizeDays / 2;
            _cursor.X = mid;
            _curStart.X = view.Position + PageSizeDays * 0.1;
            _curEnd.X = view.Position + PageSizeDays * 0.9;
            SyncRangeRect();
            UpdateCursorInfo();
            UpdateRangeInfo();
            UpdatePageLabel();
            _followTail = true;
        }

        /// <summary>增量拉取新事件；跟随模式保持窗口贴最新</summary>
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
                view.Position = _lastPointTime.AddMilliseconds(-PageSizeMs).ToOADate();
            }
            chart1.ResumeLayout();
            UpdatePageLabel();
        }

        /// <summary>分页移动：dir = -1 上一页 / +1 下一页；到最新页恢复跟随</summary>
        private void PageMove(int dir)
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            view.Position += dir * PageSizeDays;
            _followTail = false;
            if (view.Position + view.Size >= _lastPointTime.ToOADate())
            {
                _followTail = true;
                view.Position = _lastPointTime.AddMilliseconds(-PageSizeMs).ToOADate();
            }
            SyncRangeRect();
            UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            DateTime s = DateTime.FromOADate(view.Position);
            DateTime e = DateTime.FromOADate(view.Position + view.Size);
            lblPage.Text = "窗口: " + s.ToString("HH:mm:ss") + " ~ " + e.ToString("HH:mm:ss");
        }

        /// <summary>滚轮缩放：X/Y 双轴均以鼠标位置为锚点</summary>
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var area = chart1.ChartAreas[0];
            double factor = e.Delta > 0 ? 0.8 : 1.25;
            var viewX = area.AxisX.ScaleView;
            double xSize = viewX.Size;
            double mouseX = area.AxisX.PixelPositionToValue(e.X);
            viewX.Position = mouseX - (mouseX - viewX.Position) * factor;
            viewX.Size = Math.Max(xSize * factor, 0.5 / 86400000.0);

            var viewY = area.AxisY.ScaleView;
            double ySize = double.IsNaN(viewY.Size) ? (area.AxisY.Maximum - area.AxisY.Minimum) : viewY.Size;
            double mouseY = area.AxisY.PixelPositionToValue(e.Y);
            viewY.Position = mouseY - (mouseY - viewY.Position) * factor;
            viewY.Size = Math.Max(ySize * factor, 1.0);
            _followTail = false;
        }

        /// <summary>单游标：显示所停时刻的流程步号</summary>
        private void UpdateCursorInfo()
        {
            if (!chkCursor.Checked)
            {
                return;
            }
            DateTime t = DateTime.FromOADate(_cursor.X);
            short step = FindStepAt(t);
            string info = "游标 " + t.ToString("HH:mm:ss.fff") + "  步号 " + step;
            if (chkRange.Checked)
            {
                DateTime t1 = DateTime.FromOADate(_curStart.X);
                DateTime t2 = DateTime.FromOADate(_curEnd.X);
                info += "   |   " + RangeText(t1, t2);
            }
            lblCursorInfo.Text = info;
        }

        /// <summary>区域游标：更新高亮矩形与区间时长</summary>
        private void UpdateRangeInfo()
        {
            SyncRangeRect();
            if (!chkRange.Checked)
            {
                return;
            }
            DateTime t1 = DateTime.FromOADate(_curStart.X);
            DateTime t2 = DateTime.FromOADate(_curEnd.X);
            lblCursorInfo.Text = "区域 " + RangeText(t1, t2);
        }

        private void SyncRangeRect()
        {
            var area = chart1.ChartAreas[0];
            double x1 = Math.Min(_curStart.X, _curEnd.X);
            double x2 = Math.Max(_curStart.X, _curEnd.X);
            _rangeRect.X = x1;
            _rangeRect.Width = x2 - x1;
            _rangeRect.Y = area.AxisY.Minimum;
            _rangeRect.Height = Math.Max(area.AxisY.Maximum - area.AxisY.Minimum, 1);
        }

        private string RangeText(DateTime t1, DateTime t2)
        {
            TimeSpan d = (t2 - t1).Duration();
            return t1.ToString("HH:mm:ss.fff") + " ~ " + t2.ToString("HH:mm:ss.fff")
                   + "  时长 " + d.TotalSeconds.ToString("F3") + " s";
        }

        /// <summary>二分查找：t 时刻所处的步号（最后一个 Time &lt;= t 的事件）</summary>
        private short FindStepAt(DateTime t)
        {
            int lo = 0, hi = _events.Count - 1, ans = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_events[mid].Time <= t)
                {
                    ans = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return ans >= 0 ? _events[ans].Step : (short)-1;
        }

        /// <summary>鼠标移动：底部信息栏显示光标对应的时间与步号（未启用游标时用）</summary>
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
                    return;
                }
                DateTime t = DateTime.FromOADate(xVal);
                lblCursorInfo.Text = "时间: " + t.ToString("HH:mm:ss.fff") + "    步号: " + (short)Math.Round(yVal);
            }
            catch
            {
            }
        }
    }
}
