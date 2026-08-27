using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    public partial class Frm_MultiTrend : Form
    {
        private readonly List<StepEvent> _data;
        private readonly Dictionary<int, List<StepEvent>> _byStation;   // 每工位时间有序事件
        private readonly Timer _timer;
        private VerticalLineAnnotation _cursor;
        private VerticalLineAnnotation _curStart, _curEnd;   // 区域开始/结束
        private RectangleAnnotation _rangeRect;              // 区域高亮
        private double _lastRangeStart, _lastRangeEnd;       // 锁定联动基线
        private bool _isPanning;
        private Point _panStart;
        private double _panViewStart, _panYViewStart, _panYSizeStart;
        private string _highlightName;                                  // 高亮工位（Series 名）

        public Frm_MultiTrend()
        {
            InitializeComponent();
            _data = EventStore.LoadedHistory ?? new List<StepEvent>();
            _byStation = _data.GroupBy(ev => ev.Station)
                              .ToDictionary(g => g.Key, g => g.OrderBy(ev => ev.Time).ToList());
            FillStationList();
            InitChart();
            RebuildSeries();
            Text = "多工位趋势图（点击图例高亮，双击改色）";
            _timer = new Timer { Interval = 100 };
            _timer.Tick += (s, e) =>
            {
                UpdateCursorInfo();
                UpdateRangeInfo();
            };
            _timer.Start();
            FormClosing += (s, e) => _timer.Stop();
        }

        private void FillStationList()
        {
            int maxStation = _data.Count > 0 ? _data.Max(ev => ev.Station) : PlcData.StepCount - 1;
            for (int i = 0; i <= maxStation; i++)
            {
                int cnt = _byStation.TryGetValue(i, out var list) ? list.Count : 0;
                chkStations.Items.Add("工位 " + i + " (" + cnt + ")", cnt > 0);
            }
            // 默认选前 10 个
            for (int i = 0; i < chkStations.Items.Count && i < 10; i++)
            {
                chkStations.SetItemChecked(i, true);
            }
            chkStations.SelectedIndexChanged += (s, e) => RebuildSeries();
        }

        private void InitChart()
        {
            var area = chart1.ChartAreas[0];
            area.AxisX.Title = "时间";
            area.AxisY.Title = "步号";
            area.AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            area.AxisY.IsStartedFromZero = false;
            area.AxisY.IntervalAutoMode = IntervalAutoMode.FixedCount;   // 固定刻度数量
            area.AxisY.Interval = 6;
            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;
            area.CursorX.IsUserEnabled = true;
            area.CursorX.LineColor = Color.FromArgb(160, 255, 0, 0);
            area.CursorY.IsUserEnabled = true;
            area.CursorY.LineColor = Color.FromArgb(160, 255, 0, 0);
            chart1.Legends[0].Docking = Docking.Right;

            // 游标竖线（可拖动）
            _cursor = new VerticalLineAnnotation
            {
                AxisX = area.AxisX,
                IsInfinitive = true,
                ClipToChartArea = area.Name,
                AllowMoving = true,
                AllowSelecting = false,
                LineColor = Color.Orange,
                LineWidth = 2,
                Visible = true
            };
            chart1.Annotations.Add(_cursor);

            // 区域游标（开始/结束线 + 高亮）
            _curStart = new VerticalLineAnnotation
            {
                AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name,
                AllowMoving = true, AllowSelecting = false,
                LineColor = Color.DeepSkyBlue, LineWidth = 2, Visible = false
            };
            _curEnd = new VerticalLineAnnotation
            {
                AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name,
                AllowMoving = true, AllowSelecting = false,
                LineColor = Color.DeepSkyBlue, LineWidth = 2, Visible = false
            };
            _rangeRect = new RectangleAnnotation
            {
                AxisX = area.AxisX, AxisY = area.AxisY,
                AllowMoving = false, AllowResizing = false, LineWidth = 0,
                BackColor = Color.FromArgb(70, 25, 25, 112), Visible = false
            };
            chart1.Annotations.Add(_curStart);
            chart1.Annotations.Add(_curEnd);
            chart1.Annotations.Add(_rangeRect);

            chart1.MouseWheel += OnMouseWheel;
            chart1.MouseDown += OnChartMouseDown;
            chart1.MouseMove += OnChartMouseMove;
            chart1.MouseUp += (s, e) =>
            {
                _isPanning = false;
                _lastRangeStart = _curStart.X;
                _lastRangeEnd = _curEnd.X;
                UpdateCursorInfo();
                UpdateRangeInfo();
            };

            // 区域开关
            chkRange.CheckedChanged += (s, e) =>
            {
                _curStart.Visible = _curEnd.Visible = _rangeRect.Visible = chkRange.Checked;
                if (chkRange.Checked)
                {
                    PlaceRangeInView();
                }
            };
            chkLock.CheckedChanged += (s, e) =>
            {
                _lastRangeStart = _curStart.X;
                _lastRangeEnd = _curEnd.X;
            };

            // 点击图例高亮对应工位折线
            chart1.MouseDown += OnLegendClickHighlight;
            // 双击图例改色
            chart1.MouseDoubleClick += (s, e) =>
            {
                var hit = chart1.HitTest(e.X, e.Y);
                if (hit.ChartElementType == ChartElementType.LegendItem && hit.Series != null)
                {
                    using (var cd = new ColorDialog())
                    {
                        cd.Color = hit.Series.Color;
                        if (cd.ShowDialog() == DialogResult.OK)
                        {
                            hit.Series.Color = cd.Color;
                            if (_highlightName == hit.Series.Name)
                            {
                                hit.Series.BorderWidth = 4;
                            }
                        }
                    }
                }
            };

            btnFit.Click += (s, e) => FitData();
            btnSelectAll.Click += (s, e) =>
            {
                bool allChecked = chkStations.CheckedItems.Count == chkStations.Items.Count;
                for (int i = 0; i < chkStations.Items.Count; i++)
                {
                    chkStations.SetItemChecked(i, !allChecked);
                }
                RebuildSeries();
            };
        }

        /// <summary>点击图例：高亮该工位折线（加粗，其他降透明度）</summary>
        private void OnLegendClickHighlight(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var hit = chart1.HitTest(e.X, e.Y);
            if (hit.ChartElementType == ChartElementType.LegendItem && hit.Series != null)
            {
                _highlightName = (_highlightName == hit.Series.Name) ? null : hit.Series.Name;
                ApplyHighlight();
            }
        }

        private void ApplyHighlight()
        {
            foreach (var ser in chart1.Series)
            {
                if (ser.Name == _highlightName)
                {
                    ser.BorderWidth = 4;
                    ser.Color = Color.FromArgb(255, ser.Color);
                }
                else
                {
                    ser.BorderWidth = 1;
                    ser.Color = Color.FromArgb(130, ser.Color);
                }
            }
        }

        private void RebuildSeries()
        {
            chart1.Series.Clear();
            _highlightName = null;
            int n = chkStations.CheckedItems.Count;
            if (n == 0)
            {
                lblInfo.Text = "请勾选左侧工位";
                return;
            }
            int idx = 0;
            short gMinY = short.MaxValue, gMaxY = short.MinValue;
            double gStart = double.MaxValue, gEnd = double.MinValue;
            foreach (int i in chkStations.CheckedIndices)
            {
                if (!_byStation.TryGetValue(i, out var events) || events.Count == 0) continue;
                double hue = 360.0 * idx / n;
                var c = ColorFromHSV(hue, 1.0, 1.0);
                var ser = new Series("工位 " + i)
                {
                    ChartType = SeriesChartType.StepLine,
                    BorderWidth = 2,
                    Color = c
                };
                ser.Points.DataBindXY(events.Select(ev => ev.Time).ToArray(),
                                       events.Select(ev => (double)ev.Step).ToArray());
                chart1.Series.Add(ser);
                foreach (var ev in events)
                {
                    if (ev.Step < gMinY) gMinY = ev.Step;
                    if (ev.Step > gMaxY) gMaxY = ev.Step;
                    if (ev.Time.ToOADate() < gStart) gStart = ev.Time.ToOADate();
                    if (ev.Time.ToOADate() > gEnd) gEnd = ev.Time.ToOADate();
                }
                idx++;
            }
            if (gMinY != short.MaxValue)
            {
                double span = Math.Max(gMaxY - gMinY, 1);
                chart1.ChartAreas[0].AxisY.ScaleView.Position = gMinY - span * 0.1;
                chart1.ChartAreas[0].AxisY.ScaleView.Size = span * 1.2 + 1;
            }
            if (gStart != double.MaxValue)
            {
                chart1.ChartAreas[0].AxisX.ScaleView.Position = gStart;
                chart1.ChartAreas[0].AxisX.ScaleView.Size = Math.Max(gEnd - gStart, 0.5 / 86400000.0);
            }
            // 游标置于视野中间
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            _cursor.X = view.Position + view.Size / 2;
            PlaceRangeInView();
            UpdateCursorInfo();
            UpdateRangeInfo();
        }

        /// <summary>区域线置于视野中间两侧（间隔 30%）</summary>
        private void PlaceRangeInView()
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            double center = view.Position + view.Size / 2;
            _curStart.X = center - view.Size * 0.15;
            _curEnd.X = center + view.Size * 0.15;
            _lastRangeStart = _curStart.X;
            _lastRangeEnd = _curEnd.X;
            SyncRangeRect();
        }

        /// <summary>区域高亮矩形覆盖绘图区 + 时长显示</summary>
        private void SyncRangeRect()
        {
            if (double.IsNaN(_curStart.X) || double.IsNaN(_curEnd.X)) return;
            var area = chart1.ChartAreas[0];
            var viewY = area.AxisY.ScaleView;
            double yMin = viewY.Position;
            double yMax = viewY.Position + viewY.Size;
            if (double.IsNaN(yMin) || double.IsInfinity(yMin)) yMin = area.AxisY.Minimum;
            if (double.IsNaN(yMax) || double.IsInfinity(yMax)) yMax = area.AxisY.Maximum;
            if (double.IsNaN(yMin) || double.IsInfinity(yMin)) yMin = 0;
            if (double.IsNaN(yMax) || double.IsInfinity(yMax)) yMax = 100;
            double x1 = Math.Min(_curStart.X, _curEnd.X);
            double x2 = Math.Max(_curStart.X, _curEnd.X);
            _rangeRect.X = x1;
            _rangeRect.Width = x2 - x1;
            _rangeRect.Y = yMin;
            _rangeRect.Height = Math.Max(yMax - yMin, 1);
        }

        /// <summary>区域信息：时长（ms 单位）</summary>
        private void UpdateRangeInfo()
        {
            if (!chkRange.Checked) return;
            SyncRangeRect();
            double x1 = Math.Min(_curStart.X, _curEnd.X);
            double x2 = Math.Max(_curStart.X, _curEnd.X);
            double durMs = (x2 - x1) * 86400000.0;
            lblInfo.Text = "区域 " + DateTime.FromOADate(x1).ToString("HH:mm:ss.fff")
                           + " ~ " + DateTime.FromOADate(x2).ToString("HH:mm:ss.fff")
                           + "  时长 " + durMs.ToString("F0") + " ms";
        }

        /// <summary>适应：X/Y 轴覆盖全部选中数据（含 10% 留白）</summary>
        private void FitData()
        {
            if (chart1.Series.Count == 0) return;
            var area = chart1.ChartAreas[0];
            short minY = short.MaxValue, maxY = short.MinValue;
            double gStart = double.MaxValue, gEnd = double.MinValue;
            foreach (var ser in chart1.Series)
            {
                foreach (var pt in ser.Points)
                {
                    double y = pt.YValues[0];
                    if (y < minY) minY = (short)y;
                    if (y > maxY) maxY = (short)y;
                    if (pt.XValue < gStart) gStart = pt.XValue;
                    if (pt.XValue > gEnd) gEnd = pt.XValue;
                }
            }
            if (minY == short.MaxValue) return;
            double ySpan = Math.Max(maxY - minY, 1);
            area.AxisY.ScaleView.Position = minY - ySpan * 0.1;
            area.AxisY.ScaleView.Size = ySpan * 1.2 + 1;
            area.AxisX.ScaleView.Position = gStart;
            area.AxisX.ScaleView.Size = Math.Max(gEnd - gStart, 0.5 / 86400000.0);
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var area = chart1.ChartAreas[0];
            double mouseX, mouseY;
            try
            {
                mouseX = area.AxisX.PixelPositionToValue(e.X);
                mouseY = area.AxisY.PixelPositionToValue(e.Y);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            if (double.IsNaN(mouseX) || double.IsNaN(mouseY)) return;

            double factor = e.Delta > 0 ? 0.8 : 1.25;
            var viewX = area.AxisX.ScaleView;
            double xSize = viewX.Size;
            double newXSize = Math.Max(xSize * factor, 0.5 / 86400000.0);
            double fX = newXSize / xSize;
            viewX.Position = mouseX - (mouseX - viewX.Position) * fX;
            viewX.Size = newXSize;

            var viewY = area.AxisY.ScaleView;
            double ySize = double.IsNaN(viewY.Size) ? (area.AxisY.Maximum - area.AxisY.Minimum) : viewY.Size;
            double yPos = double.IsNaN(viewY.Position) ? area.AxisY.Minimum : viewY.Position;
            double newYSize = Math.Max(ySize * factor, 1.0);
            double fY = newYSize / ySize;
            viewY.Position = mouseY - (mouseY - yPos) * fY;
            viewY.Size = newYSize;
            ClampView();
        }

        private void OnChartMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var hit = chart1.HitTest(e.X, e.Y);
            if (hit.ChartElementType == ChartElementType.Annotation)
            {
                return;   // 游标拖动交给 Chart
            }
            // 绘图区内拖拽平移
            var ca = chart1.ChartAreas[0];
            var outer = ca.Position;
            var inner = ca.InnerPlotPosition;
            var plotRect = new Rectangle(
                (int)(chart1.Width * (outer.X + outer.Width * inner.X / 100) / 100),
                (int)(chart1.Height * (outer.Y + outer.Height * inner.Y / 100) / 100),
                (int)(chart1.Width * outer.Width * inner.Width / 10000),
                (int)(chart1.Height * outer.Height * inner.Height / 10000));
            if (!plotRect.Contains(e.Location)) return;
            _isPanning = true;
            _panStart = e.Location;
            _panViewStart = ca.AxisX.ScaleView.Position;
            var viewY = ca.AxisY.ScaleView;
            _panYViewStart = double.IsNaN(viewY.Position) ? ca.AxisY.Minimum : viewY.Position;
            _panYSizeStart = double.IsNaN(viewY.Size) ? (ca.AxisY.Maximum - ca.AxisY.Minimum) : viewY.Size;
        }

        private void OnChartMouseMove(object sender, MouseEventArgs e)
        {
            // 锁定区域：拖动一根线时另一根同步（保持区域长度，整体平移）
            if (chkLock.Checked && chkRange.Checked)
            {
                double dStart = _curStart.X - _lastRangeStart;
                double dEnd = _curEnd.X - _lastRangeEnd;
                if (Math.Abs(dStart) > 1e-12 && Math.Abs(dEnd) < Math.Abs(dStart) * 0.1)
                {
                    _curEnd.X += dStart;
                }
                else if (Math.Abs(dEnd) > 1e-12 && Math.Abs(dStart) < Math.Abs(dEnd) * 0.1)
                {
                    _curStart.X += dEnd;
                }
                _lastRangeStart = _curStart.X;
                _lastRangeEnd = _curEnd.X;
                SyncRangeRect();
            }
            if (!_isPanning) return;
            var area = chart1.ChartAreas[0];
            double dx = -(e.X - _panStart.X) / (double)chart1.Width * area.AxisX.ScaleView.Size;
            area.AxisX.ScaleView.Position = _panViewStart + dx;
            double dy = (e.Y - _panStart.Y) / (double)chart1.Height * _panYSizeStart;
            area.AxisY.ScaleView.Position = _panYViewStart + dy;
            area.AxisY.ScaleView.Size = _panYSizeStart;
            ClampView();
        }

        /// <summary>视图 clamp：不滑出数据范围（左右）</summary>
        private void ClampView()
        {
            if (chart1.Series.Count == 0) return;
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            if (double.IsNaN(view.Position) || double.IsNaN(view.Size)) return;
            double gStart = double.MaxValue, gEnd = double.MinValue;
            foreach (var ser in chart1.Series)
            {
                if (ser.Points.Count > 0)
                {
                    if (ser.Points[0].XValue < gStart) gStart = ser.Points[0].XValue;
                    if (ser.Points[ser.Points.Count - 1].XValue > gEnd) gEnd = ser.Points[ser.Points.Count - 1].XValue;
                }
            }
            if (gStart == double.MaxValue) return;
            if (view.Position < gStart - 0.0001) view.Position = gStart;
            if (view.Position + view.Size > gEnd + 0.0001) view.Position = gEnd - view.Size;
        }

        /// <summary>游标信息：显示所有选中工位在游标时刻的步号</summary>
        private void UpdateCursorInfo()
        {
            if (double.IsNaN(_cursor.X)) return;
            DateTime t = DateTime.FromOADate(_cursor.X);
            var parts = new List<string>();
            parts.Add("游标 " + t.ToString("HH:mm:ss.fff"));
            foreach (int i in chkStations.CheckedIndices)
            {
                short step = StepAt(i, t);
                parts.Add("工位" + i + ": " + (step < 0 ? "--" : step.ToString()));
            }
            lblInfo.Text = string.Join("  ", parts);
        }

        private short StepAt(int station, DateTime t)
        {
            if (!_byStation.TryGetValue(station, out var list) || list.Count == 0) return -1;
            int lo = 0, hi = list.Count - 1, ans = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (list[mid].Time <= t) { ans = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return ans >= 0 ? list[ans].Step : (short)-1;
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = ((int)(hue / 60)) % 6;
            double f = hue / 60 - hi;
            double p = value * (1 - saturation);
            double q = value * (1 - f * saturation);
            double t = value * (1 - (1 - f) * saturation);
            int r, g, b;
            switch (hi)
            {
                case 0: r = (int)(value * 255); g = (int)(t * 255); b = (int)(p * 255); break;
                case 1: r = (int)(q * 255); g = (int)(value * 255); b = (int)(p * 255); break;
                case 2: r = (int)(p * 255); g = (int)(value * 255); b = (int)(t * 255); break;
                case 3: r = (int)(p * 255); g = (int)(q * 255); b = (int)(value * 255); break;
                case 4: r = (int)(t * 255); g = (int)(p * 255); b = (int)(value * 255); break;
                default: r = (int)(value * 255); g = (int)(p * 255); b = (int)(q * 255); break;
            }
            return Color.FromArgb(r, g, b);
        }
    }
}