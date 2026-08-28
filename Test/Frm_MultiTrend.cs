using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    /// <summary>
    /// 多工位总览趋势图：实时/历史双模式，叠加曲线，支持分页/缩放/拖拽/游标/区域/步号标签自绘/配置持久化。
    /// </summary>
    public partial class Frm_MultiTrend : Form
    {
        // ── 数据 ──
        private List<StepEvent> _data = new List<StepEvent>();          // 当前数据源（历史或实时合并）
        private Dictionary<int, List<StepEvent>> _byStation = new Dictionary<int, List<StepEvent>>();    // 每工位时间有序事件（二分查找用）
        private readonly List<int> _stations = new List<int>();                   // 当前选中工位
        private readonly Dictionary<int, Color> _colors = new Dictionary<int, Color>();        // 工位颜色映射
        private bool _useRealtime = true;                               // 实时模式（未加载历史）

        // ── 控件 ──
        private readonly Timer _timer;
        private VerticalLineAnnotation _cursor;
        private VerticalLineAnnotation _curStart, _curEnd;
        private RectangleAnnotation _rangeRect;
        private double _lastRangeStart, _lastRangeEnd;

        // ── 交互 ──
        private bool _followTail = true;
        private bool _isPanning;
        private Point _panStart;
        private double _panViewStart, _panYViewStart, _panYSizeStart;
        private int _panPlotHeight = 1;
        private string _highlightName;

        // ── 分页 ──
        private int _pageSizeSec = 60;
        private double PageSizeDays => _pageSizeSec / 86400.0;
        private DateTime _lastPointTime;

        // ── 标签自绘 ──
        private readonly Font _labelFont = new Font("微软雅黑", 9f);

        public Frm_MultiTrend()
        {
            InitializeComponent();

            // 加载配置
            var (cfgStations, cfgColors, cfgWin) = MultiConfig.Load();
            if (cfgWin.HasValue)
            {
                StartPosition = FormStartPosition.Manual;
                DesktopBounds = new Rectangle(cfgWin.Value.Location, cfgWin.Value.Size);
            }

            // 数据源：优先全局历史，否则实时
            if (EventStore.LoadedHistory != null && EventStore.LoadedHistory.Count > 0)
            {
                _data = EventStore.LoadedHistory;
                _useRealtime = EventStore.HistoryMode;
            }
            _byStation = _data.GroupBy(e => e.Station).ToDictionary(g => g.Key, g => g.OrderBy(e => e.Time).ToList());

            // Chart 初始化
            var area = chart1.ChartAreas[0];
            area.AxisX.Title = "时间";
            area.AxisY.Title = "步号";
            area.AxisX.LabelStyle.Format = "HH:mm:ss.fff";
            area.AxisY.IsStartedFromZero = false;
            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;
            area.CursorX.IsUserEnabled = true;
            area.CursorX.LineColor = Color.FromArgb(160, 255, 0, 0);
            area.CursorY.IsUserEnabled = true;
            area.CursorY.LineColor = Color.FromArgb(160, 255, 0, 0);
            chart1.Legends[0].Docking = Docking.Right;

            // 游标/区域注释
            _cursor = new VerticalLineAnnotation { AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name, AllowMoving = true, AllowSelecting = false, LineColor = Color.Orange, LineWidth = 2, Visible = true };
            _curStart = new VerticalLineAnnotation { AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name, AllowMoving = true, AllowSelecting = false, LineColor = Color.DeepSkyBlue, LineWidth = 2, Visible = false };
            _curEnd = new VerticalLineAnnotation { AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name, AllowMoving = true, AllowSelecting = false, LineColor = Color.DeepSkyBlue, LineWidth = 2, Visible = false };
            _rangeRect = new RectangleAnnotation { AxisX = area.AxisX, AxisY = area.AxisY, AllowMoving = false, AllowResizing = false, LineWidth = 0, BackColor = Color.FromArgb(70, 25, 25, 112), Visible = false };
            chart1.Annotations.Add(_cursor);
            chart1.Annotations.Add(_curStart);
            chart1.Annotations.Add(_curEnd);
            chart1.Annotations.Add(_rangeRect);

            // 事件
            chart1.MouseWheel += OnMouseWheel;
            chart1.MouseDown += OnMouseDown;
            chart1.MouseMove += OnMouseMove;
            chart1.MouseUp += (s, e) => { _isPanning = false; _lastRangeStart = _curStart.X; _lastRangeEnd = _curEnd.X; UpdateCursorInfo(); UpdateRangeInfo(); };
            chart1.Paint += OnChartPaint;
            chart1.MouseDoubleClick += (s, e) =>
            {
                var hit = chart1.HitTest(e.X, e.Y);
                if (hit.ChartElementType == ChartElementType.LegendItem && hit.Object is Series ser)
                {
                    using (var cd = new ColorDialog { Color = ser.Color })
                    if (cd.ShowDialog() == DialogResult.OK) { ser.Color = cd.Color; var st = GetStationFromSeries(ser); if (st >= 0) _colors[st] = cd.Color; }
                }
            };
            chart1.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var hit = chart1.HitTest(e.X, e.Y);
                    if (hit.ChartElementType == ChartElementType.LegendItem && hit.Object is Series ser)
                    {
                        _highlightName = (ser.Name == _highlightName) ? null : ser.Name;
                        foreach (var s2 in chart1.Series) { s2.BorderWidth = (s2.Name == _highlightName) ? 4 : 1; s2.Color = s2.Name == _highlightName ? s2.Color : Color.FromArgb(120, s2.Color); }
                    }
                }
            };

            chkRange.CheckedChanged += (s, e) => { _curStart.Visible = _curEnd.Visible = _rangeRect.Visible = chkRange.Checked; if (chkRange.Checked) PlaceRangeInView(); };
            chkLock.CheckedChanged += (s, e) => { _lastRangeStart = _curStart.X; _lastRangeEnd = _curEnd.X; };
            btnSelectAll.Click += (s, e) =>
            {
                bool all = chkStations.CheckedItems.Count == chkStations.Items.Count;
                for (int i = 0; i < chkStations.Items.Count; i++) chkStations.SetItemChecked(i, !all);
                RebuildSeries();
            };
            btnFit.Click += (s, e) => FitData();
            btnPrev.Click += (s, e) => PageMove(-1);
            btnNext.Click += (s, e) => PageMove(1);
            nudPageSec.ValueChanged += (s, e) =>
            {
                _pageSizeSec = (int)nudPageSec.Value;
                var view = chart1.ChartAreas[0].AxisX.ScaleView;
                if (!double.IsNaN(view.Position)) { view.Size = PageSizeDays; view.Position = Math.Max(view.Position, view.Position); }
                UpdatePageLabel();
            };
            cmbRefresh.SelectedIndexChanged += (s, e) =>
            {
                int[] intervals = { 16, 33, 100 };
                _timer.Interval = intervals[Math.Max(0, cmbRefresh.SelectedIndex)];
            };

            FillStationList(cfgStations, cfgColors);
            RebuildSeries();
            FitData();
            _timer = new Timer { Interval = 16 };
            _timer.Tick += (s, e) =>
            {
                if (_useRealtime) RefreshRealtime();
                UpdateCursorInfo();
                UpdateRangeInfo();
            };
            _timer.Start();
            FormClosing += (s, e) =>
            {
                _timer.Stop();
                _labelFont.Dispose();
                MultiConfig.Save(_stations.ToArray(), _colors,
                    WindowState == FormWindowState.Normal ? DesktopBounds : RestoreBounds);
            };
        }

        // ══════════════════════════════════════════════ 数据与配置 ══════════════════════════════════════════════

        private void FillStationList(int[] cfgStations, Dictionary<int, Color> cfgColors)
        {
            int maxSt = _data.Count > 0 ? _data.Max(e => e.Station) : PlcData.StepCount - 1;
            for (int i = 0; i <= maxSt; i++)
            {
                int cnt = _byStation.TryGetValue(i, out var list) ? list.Count : 0;
                chkStations.Items.Add("工位 " + i + " (" + cnt + " 点)", cfgStations.Contains(i));
            }
            foreach (var kv in cfgColors) _colors[kv.Key] = kv.Value;
        }

        private void RebuildSeries()
        {
            chart1.Series.Clear();
            _stations.Clear();
            var area = chart1.ChartAreas[0];
            int n = Math.Min(chkStations.CheckedItems.Count, 50);
            if (n == 0) { area.AxisX.ScaleView.Position = 0; area.AxisX.ScaleView.Size = PageSizeDays; return; }
            int idx = 0;
            foreach (int i in chkStations.CheckedIndices)
            {
                if (idx >= 50) break;
                _stations.Add(i);
                Color c = _colors.TryGetValue(i, out var cc) ? cc : HslColor(360.0 * idx / n);
                _colors[i] = c;
                var ser = new Series("工位 " + i) { ChartType = SeriesChartType.StepLine, BorderWidth = 2, Color = c };
                if (_byStation.TryGetValue(i, out var list))
                    foreach (var ev in list) ser.Points.AddXY(ev.Time, ev.Step);
                chart1.Series.Add(ser);
                idx++;
            }
            if (_data.Count > 0) _lastPointTime = _data.Max(e => e.Time);
            if (_highlightName != null) { /* 高亮恢复 */ }
            FitData();
        }

        // ══════════════════════════════════════════════ 实时刷新 ══════════════════════════════════════════════

        private void RefreshRealtime()
        {
            if (_stations.Count == 0) return;
            bool dataActive = PlcData.IsConnected || Simulator.IsRunning;
            bool changed = false;
            foreach (int st in _stations)
            {
                var fresh = EventStore.GetSince(st, _lastPointTime);
                if (fresh.Count == 0) continue;
                changed = true;
                if (!_byStation.ContainsKey(st)) _byStation[st] = new List<StepEvent>();
                _byStation[st].AddRange(fresh);
                _data.AddRange(fresh);
                var ser = chart1.Series.FirstOrDefault(s => s.Name == "工位 " + st);
                if (ser != null) foreach (var ev in fresh) ser.Points.AddXY(ev.Time, ev.Step);
            }
            if (changed)
            {
                _lastPointTime = _data.Max(e => e.Time);
                _followTail = true;
            }
            if (_followTail && dataActive)
            {
                var view = chart1.ChartAreas[0].AxisX.ScaleView;
                view.Position = DateTime.Now.ToOADate() - view.Size;
            }
        }

        // ══════════════════════════════════════════════ 分页 ══════════════════════════════════════════════

        private void PageMove(int dir)
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            if (_data.Count == 0) return;
            double start = _data[0].Time.ToOADate();
            double end = _data[_data.Count - 1].Time.ToOADate();
            if (dir < 0) { view.Position = Math.Max(start, view.Position - PageSizeDays); _followTail = false; }
            else { double p = view.Position + PageSizeDays; if (p + view.Size >= end) { _followTail = true; view.Position = end - PageSizeDays; } else { view.Position = p; _followTail = false; } }
            SyncRangeRect(); UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            try
            {
                var view = chart1.ChartAreas[0].AxisX.ScaleView;
                lblPage.Text = "窗口: " + DateTime.FromOADate(view.Position).ToString("HH:mm:ss") + " ~ " + DateTime.FromOADate(view.Position + view.Size).ToString("HH:mm:ss");
            }
            catch { lblPage.Text = "--:--:-- ~ --:--:--"; }
        }

        // ══════════════════════════════════════════════ 缩放/拖拽 ══════════════════════════════════════════════

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var area = chart1.ChartAreas[0];
            double mouseX, mouseY;
            try { mouseX = area.AxisX.PixelPositionToValue(e.X); mouseY = area.AxisY.PixelPositionToValue(e.Y); }
            catch (InvalidOperationException) { return; }
            if (double.IsNaN(mouseX) || double.IsNaN(mouseY)) return;
            double factor = e.Delta > 0 ? 0.8 : 1.25;
            var vx = area.AxisX.ScaleView;
            double xSize = double.IsNaN(vx.Size) || vx.Size <= 0 ? 0.5 / 86400000.0 : vx.Size;
            double newX = Math.Max(xSize * factor, 0.5 / 86400000.0);
            vx.Position = mouseX - (mouseX - vx.Position) * (newX / xSize); vx.Size = newX;
            var vy = area.AxisY.ScaleView;
            double ySize = double.IsNaN(vy.Size) ? area.AxisY.Maximum - area.AxisY.Minimum : vy.Size;
            double yPos = double.IsNaN(vy.Position) ? area.AxisY.Minimum : vy.Position;
            double newY = Math.Max(ySize * factor, 1.0);
            vy.Position = mouseY - (mouseY - yPos) * (newY / ySize); vy.Size = newY;
            _followTail = false;
            ClampView();
            UpdateYTicks();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            var hit = chart1.HitTest(e.X, e.Y);
            if (hit.ChartElementType == ChartElementType.Annotation && hit.Object is VerticalLineAnnotation va)
            {
                double x = va.X; var view = chart1.ChartAreas[0].AxisX.ScaleView;
                if (x < view.Position || x > view.Position + view.Size) { view.Position = x - PageSizeDays / 2; _followTail = false; ClampView(); UpdatePageLabel(); }
                return;
            }
            var ca = chart1.ChartAreas[0]; var outer = ca.Position; var inner = ca.InnerPlotPosition;
            var plot = new Rectangle((int)(chart1.Width * (outer.X + outer.Width * inner.X / 100) / 100), (int)(chart1.Height * (outer.Y + outer.Height * inner.Y / 100) / 100), (int)(chart1.Width * outer.Width * inner.Width / 10000), (int)(chart1.Height * outer.Height * inner.Height / 10000));
            if (!plot.Contains(e.Location)) return;
            _isPanning = true; _panStart = e.Location; _panPlotHeight = plot.Height;
            _panViewStart = ca.AxisX.ScaleView.Position;
            var vy = ca.AxisY.ScaleView;
            _panYViewStart = double.IsNaN(vy.Position) ? ca.AxisY.Minimum : vy.Position;
            _panYSizeStart = double.IsNaN(vy.Size) ? ca.AxisY.Maximum - ca.AxisY.Minimum : vy.Size;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (chkLock.Checked && chkRange.Checked)
            {
                double dS = _curStart.X - _lastRangeStart, dE = _curEnd.X - _lastRangeEnd;
                if (Math.Abs(dS) > 1e-12 && Math.Abs(dE) < Math.Abs(dS) * 0.1) _curEnd.X += dS;
                else if (Math.Abs(dE) > 1e-12 && Math.Abs(dS) < Math.Abs(dE) * 0.1) _curStart.X += dE;
                _lastRangeStart = _curStart.X; _lastRangeEnd = _curEnd.X; SyncRangeRect();
            }
            if (!_isPanning) return;
            var area = chart1.ChartAreas[0];
            area.AxisX.ScaleView.Position = _panViewStart - (e.X - _panStart.X) / (double)chart1.Width * area.AxisX.ScaleView.Size;
            area.AxisY.ScaleView.Position = _panYViewStart + (e.Y - _panStart.Y) / (double)Math.Max(_panPlotHeight, 1) * _panYSizeStart;
            area.AxisY.ScaleView.Size = _panYSizeStart;
            _followTail = false; ClampView(); UpdateYTicks(); UpdatePageLabel();
        }

        private void ClampView()
        {
            if (chart1.Series.Count == 0) return;
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            if (double.IsNaN(view.Position) || double.IsNaN(view.Size)) return;
            double gStart = double.MaxValue, gEnd = double.MinValue;
            foreach (var ser in chart1.Series) { if (ser.Points.Count > 0) { if (ser.Points[0].XValue < gStart) gStart = ser.Points[0].XValue; if (ser.Points[ser.Points.Count - 1].XValue > gEnd) gEnd = ser.Points[ser.Points.Count - 1].XValue; } }
            if (gStart == double.MaxValue) return;
            if (view.Position < gStart - 0.0001) view.Position = gStart;
            if (view.Position + view.Size > gEnd + 0.0001) view.Position = gEnd - view.Size;
        }

        private void UpdateYTicks()
        {
            var area = chart1.ChartAreas[0]; var vy = area.AxisY.ScaleView;
            if (double.IsNaN(vy.Position) || double.IsNaN(vy.Size) || vy.Size <= 0) return;
            double raw = vy.Size / 5.0, nice = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(raw, 1e-9))));
            double m = raw / nice; if (m >= 5) nice *= 5; else if (m >= 2) nice *= 2;
            if (nice < 1) nice = 1;
            var axis = area.AxisY; axis.Interval = nice;
            double off = (nice - (vy.Position % nice)) % nice;
            axis.IntervalOffset = off;
        }

        // ══════════════════════════════════════════════ 游标/区域 + 步号标签 ══════════════════════════════════════════════

        private void PlaceRangeInView() { var view = chart1.ChartAreas[0].AxisX.ScaleView; double c = view.Position + view.Size / 2; _curStart.X = c - view.Size * 0.15; _curEnd.X = c + view.Size * 0.15; _lastRangeStart = _curStart.X; _lastRangeEnd = _curEnd.X; SyncRangeRect(); }

        private void SyncRangeRect()
        {
            if (double.IsNaN(_curStart.X) || double.IsNaN(_curEnd.X)) return;
            var area = chart1.ChartAreas[0]; var vy = area.AxisY.ScaleView;
            double yMin = vy.Position, yMax = vy.Position + vy.Size;
            if (double.IsNaN(yMin) || double.IsInfinity(yMin)) yMin = area.AxisY.Minimum;
            if (double.IsNaN(yMax) || double.IsInfinity(yMax)) yMax = area.AxisY.Maximum;
            if (double.IsNaN(yMin) || double.IsInfinity(yMin)) yMin = 0;
            if (double.IsNaN(yMax) || double.IsInfinity(yMax)) yMax = 100;
            double x1 = Math.Min(_curStart.X, _curEnd.X), x2 = Math.Max(_curStart.X, _curEnd.X);
            _rangeRect.X = x1; _rangeRect.Width = x2 - x1; _rangeRect.Y = yMin; _rangeRect.Height = Math.Max(yMax - yMin, 1);
        }

        private void UpdateCursorInfo()
        {
            if (double.IsNaN(_cursor.X)) return;
            DateTime t = SafeFromOADate(_cursor.X);
            var parts = new List<string> { "游标 " + t.ToString("HH:mm:ss.fff") };
            foreach (int i in _stations)
            {
                short step = StepAt(i, t);
                parts.Add("工位" + i + ": " + (step < 0 ? "--" : step.ToString()));
            }
            if (chkRange.Checked) { var t1 = SafeFromOADate(_curStart.X); var t2 = SafeFromOADate(_curEnd.X); parts.Add("区域 " + t1.ToString("HH:mm:ss.fff") + " ~ " + t2.ToString("HH:mm:ss.fff") + " " + ((_curEnd.X - _curStart.X) * 86400000.0).ToString("F0") + " ms"); }
            lblInfo.Text = string.Join("  |  ", parts);
        }

        private void UpdateRangeInfo()
        {
            if (!chkRange.Checked) return;
            SyncRangeRect();
            double x1 = Math.Min(_curStart.X, _curEnd.X), x2 = Math.Max(_curStart.X, _curEnd.X);
            double durMs = (x2 - x1) * 86400000.0;
            DateTime t1 = SafeFromOADate(x1), t2 = SafeFromOADate(x2);
            lblInfo.Text = "区域 " + t1.ToString("HH:mm:ss.fff") + " ~ " + t2.ToString("HH:mm:ss.fff") + "  时长 " + durMs.ToString("F0") + " ms";
        }

        private short StepAt(int station, DateTime t)
        {
            if (!_byStation.TryGetValue(station, out var list) || list.Count == 0) return -1;
            int lo = 0, hi = list.Count - 1, ans = -1;
            while (lo <= hi) { int mid = (lo + hi) / 2; if (list[mid].Time <= t) { ans = mid; lo = mid + 1; } else hi = mid - 1; }
            return ans >= 0 ? list[ans].Step : (short)-1;
        }

        private static DateTime SafeFromOADate(double v) { if (double.IsNaN(v) || v < 0 || v > 2958465.99) return DateTime.MinValue; return DateTime.FromOADate(v); }

        private int GetStationFromSeries(Series s) { if (s == null) return -1; string n = s.Name; if (n.StartsWith("工位 ")) return int.TryParse(n.Substring(3), out var st) ? st : -1; return -1; }

        // ══════════════════════════════════════════════ 步号标签自绘 ══════════════════════════════════════════════

        private void OnChartPaint(object sender, PaintEventArgs e)
        {
            try
            {
                var area = chart1.ChartAreas[0];
                if (_stations.Count == 0) return;
                var tags = new List<TagItem>();
                if (!double.IsNaN(_cursor.X))
                {
                    foreach (int st in _stations)
                    {
                        short step = StepAt(st, SafeFromOADate(_cursor.X));
                        if (step >= 0) tags.Add(new TagItem(_cursor.X, st + ":" + step, Color.Orange, -1));
                    }
                }
                if (chkRange.Checked)
                {
                    if (!double.IsNaN(_curStart.X)) foreach (int st in _stations) { short s = StepAt(st, SafeFromOADate(_curStart.X)); if (s >= 0) tags.Add(new TagItem(_curStart.X, st + ":" + s, Color.DeepSkyBlue, -1)); }
                    if (!double.IsNaN(_curEnd.X)) foreach (int st in _stations) { short s = StepAt(st, SafeFromOADate(_curEnd.X)); if (s >= 0) tags.Add(new TagItem(_curEnd.X, st + ":" + s, Color.DeepSkyBlue, -1)); }
                }
                if (tags.Count > 0) DrawStepTags(e.Graphics, area, tags);
            }
            catch { }
        }

        private void DrawStepTags(Graphics g, ChartArea area, List<TagItem> tags)
        {
            float topBase = (float)(area.Position.Y / 100 * chart1.Height + 2);
            double plotLeft = area.Position.X / 100 * chart1.Width;
            var outer = area.Position; var inner = area.InnerPlotPosition;
            float plotTop = (float)(chart1.Height * (outer.Y + outer.Height * inner.Y / 100) / 100);
            float plotBottom = (float)(chart1.Height * (outer.Y + outer.Height * (inner.Y + inner.Height) / 100) / 100);
            float maxBase = plotBottom - 24;

            var items = new List<TagLayout>();
            foreach (var t in tags)
            {
                float xPix = (float)area.AxisX.ValueToPixelPosition(t.X);
                SizeF sz = g.MeasureString(t.Text, _labelFont);
                float l = xPix - sz.Width / 2 - 4; if (l < plotLeft) l = (float)plotLeft;
                float baseY = t.BaseY >= 0 ? t.BaseY : topBase;
                baseY = Math.Min(Math.Max(baseY, plotTop), maxBase);
                items.Add(new TagLayout(l, l + sz.Width + 8, t.Text, t.Color, baseY));
            }
            items.Sort((a, b) => a.Left.CompareTo(b.Left));
            var placed = new List<(float left, float right, int layer)>();
            foreach (var it in items)
            {
                int layer = 0;
                foreach (var p in placed) if (it.Left <= p.right + 4 && it.Right >= p.left - 4) layer = Math.Max(layer, p.layer + 1);
                float y = it.BaseY + layer * 30;   // 层间距 30px（防多工位标签重叠）
                using (var bg = new SolidBrush(it.Color))
                {
                    var rect = new RectangleF(it.Left, y, it.Right - it.Left, 20);
                    g.FillRectangle(bg, rect);
                    g.DrawString(it.Text, _labelFont, Brushes.White, rect.X + 4, rect.Y + 2);
                }
                placed.Add((it.Left, it.Right, layer));
            }
        }

        private class TagItem { public double X; public string Text; public Color Color; public float BaseY; public TagItem(double x, string t, Color c, float b) { X = x; Text = t; Color = c; BaseY = b; } }
        private class TagLayout { public float Left, Right; public string Text; public Color Color; public float BaseY; public TagLayout(float l, float r, string t, Color c, float b) { Left = l; Right = r; Text = t; Color = c; BaseY = b; } }

        // ══════════════════════════════════════════════ 适应 ══════════════════════════════════════════════

        private void FitData()
        {
            if (_data.Count == 0) return;
            var area = chart1.ChartAreas[0];
            double start = _data[0].Time.ToOADate(), end = _data[_data.Count - 1].Time.ToOADate();
            area.AxisX.ScaleView.Position = start; area.AxisX.ScaleView.Size = Math.Max(end - start, 0.5 / 86400000.0);
            short minY = short.MaxValue, maxY = short.MinValue;
            foreach (var ev in _data) { if (ev.Step < minY) minY = ev.Step; if (ev.Step > maxY) maxY = ev.Step; }
            if (minY == short.MaxValue) return;
            double ySpan = Math.Max(maxY - minY, 1);
            area.AxisY.ScaleView.Position = minY - ySpan * 0.1; area.AxisY.ScaleView.Size = ySpan * 1.2 + 1;
            UpdateYTicks(); UpdatePageLabel();
        }

        private static Color HslColor(double hue)
        {
            int hi = (int)(hue / 60) % 6; double f = hue / 60 - hi;
            double v = 1.0, s = 1.0, p = v * (1 - s), q = v * (1 - f * s), t = v * (1 - (1 - f) * s);
            int V = (int)(v * 255), Q = (int)(q * 255), T = (int)(t * 255), P = (int)(p * 255);
            switch (hi)
            {
                case 0: return Color.FromArgb(V, T, P);
                case 1: return Color.FromArgb(Q, V, P);
                case 2: return Color.FromArgb(P, V, T);
                case 3: return Color.FromArgb(P, Q, V);
                case 4: return Color.FromArgb(T, P, V);
                default: return Color.FromArgb(V, P, Q);
            }
        }
    }
}