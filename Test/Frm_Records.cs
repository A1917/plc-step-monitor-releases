using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Test
{
    /// <summary>
    /// 工位实时趋势图：步号-时间台阶曲线。
    /// 分页浏览（可设分钟数）、双轴锚点缩放、拖拽平移、单/区域游标、适应数据。
    /// </summary>
    public partial class Frm_Records : Form
    {
        private readonly int _station;
        private readonly Timer _timer;
        private readonly Series _series;
        private readonly List<StepEvent> _events;       // 已加载事件（时间升序）
        private DateTime _lastPointTime;
        private bool _followTail = true;

        private VerticalLineAnnotation _cursor;         // 单游标（橙）
        private VerticalLineAnnotation _curStart;       // 区域开始（蓝）
        private VerticalLineAnnotation _curEnd;         // 区域结束（蓝）
        private RectangleAnnotation _rangeRect;         // 区域高亮

        private bool _isPanning;                        // 拖拽平移中
        private Point _panStart;
        private double _panViewStart;
        private double _panYViewStart;
        private double _panYSizeStart;
        private double _lastRangeStart, _lastRangeEnd;   // 区域线位置基线（锁定模式联动用）
        private Point _mousePos = new Point(-1, -1);     // 鼠标在图表上的位置（-1 = 不在图表）
        private bool _loadingFromFile;                    // 历史加载模式（暂停实时追加）
        private int _draggingWhich = -1;                  // 拖动的线：0=游标 1=区域start 2=区域end -1=无
        private float _cursorLabelY = -1;                 // 各线标签 Y（-1=顶部），独立记忆
        private float _startLabelY = -1;
        private float _endLabelY = -1;

        private const int MaxEvents = 60000;                // 窗体事件上限（防长时间累积卡顿）
        private readonly Font _labelFont = new Font("微软雅黑", 9f);   // 自绘标签字体（缓存防频繁创建）
        private double PageSizeMs => (double)nudPageSec.Value * 1000;
        private double PageSizeDays => PageSizeMs / 86400000.0;

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
            area.AxisY.LabelStyle.Format = "0";                    // Y 轴整数显示（步号无小数）
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = false;           // 取消左键框选（左键=拖拽平移）
            area.CursorY.IsUserEnabled = true;
            area.CursorY.IsUserSelectionEnabled = false;
            area.CursorX.LineColor = Color.FromArgb(120, 255, 0, 0);
            area.CursorY.LineColor = Color.FromArgb(120, 255, 0, 0);
            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;
            _series.ToolTip = "#VALX{yyyy-MM-dd HH:mm:ss.fff}\n步号 #VALY";

            chart1.MouseWheel += OnMouseWheel;
            chart1.DoubleClick += (s, e) => FitWindow();
            chart1.MouseDown += OnChartMouseDown;
            chart1.MouseMove += OnChartMouseMove;
            chart1.MouseUp += (s, e) =>
            {
                _isPanning = false;
                _draggingWhich = -1;   // 松开：各线标签保持当前位置（独立记忆）
                _lastRangeStart = _curStart.X;   // 刷新锁定联动基线
                _lastRangeEnd = _curEnd.X;
                // 拖到最右端 → 进入跟随模式
                var view = chart1.ChartAreas[0].AxisX.ScaleView;
                if (_events.Count > 0 && view.Position + view.Size >= _lastPointTime.ToOADate())
                {
                    _followTail = true;
                    DateTime tail = _lastPointTime > DateTime.Now ? _lastPointTime : DateTime.Now;
                    view.Position = tail.AddMilliseconds(-PageSizeMs).ToOADate();
                }
            };
            chart1.MouseLeave += (s, e) =>
            {
                _mousePos = new Point(-1, -1);
                chart1.Invalidate();
            };
            chart1.Paint += OnChartPaint;   // 自绘游标步号标签

            InitAnnotations();
            btnPrev.Click += (s, e) => PageMove(-1);
            btnNext.Click += (s, e) => PageMove(+1);
            btnFit.Click += (s, e) => FitToData();
            btnLoad.Click += (s, e) => LoadHistory();
            chkCursor.CheckedChanged += (s, e) =>
            {
                _cursor.Visible = chkCursor.Checked;
                chart1.Invalidate();
                if (chkCursor.Checked)
                {
                    PlaceCursorInView();   // 只重置游标，不影响区域
                }
            };
            chkRange.CheckedChanged += (s, e) =>
            {
                _curStart.Visible = _curEnd.Visible = _rangeRect.Visible = chkRange.Checked;
                if (chkRange.Checked)
                {
                    PlaceRangeInView();   // 只重置区域，不影响游标
                }
            };
            nudPageSec.ValueChanged += (s, e) =>
            {
                var view = area.AxisX.ScaleView;
                if (double.IsNaN(view.Position) || double.IsNaN(view.Size))
                {
                    return;   // 视图未初始化，跳过
                }
                double right = view.Position + view.Size;   // 保持窗口右端不动，仅改宽度
                view.Size = PageSizeDays;
                view.Position = right - PageSizeDays;
                _followTail = false;
                SyncRangeRect();
                UpdatePageLabel();
            };

            RebuildPoints();
            FitWindow();

            _timer = new Timer { Interval = 16 };   // 默认 60fps（可经刷新率下拉切换）
            _timer.Tick += (s, e) =>
            {
                RefreshChart();
                // 游标信息已含区域信息；区域仅在未开游标时单独显示（防覆盖）
                if (chkCursor.Checked) UpdateCursorInfo();
                else if (chkRange.Checked) UpdateRangeInfo();
            };
            // 刷新率切换：60fps/30fps/10fps → 16/33/100ms
            cmbRefresh.SelectedIndexChanged += (s, e) =>
            {
                int[] intervals = { 16, 33, 100 };
                int idx = Math.Max(0, cmbRefresh.SelectedIndex);
                _timer.Interval = intervals[Math.Min(idx, intervals.Length - 1)];
            };
            _timer.Start();
            FormClosing += (s, e) => _timer.Stop();
        }

        /// <summary>初始化游标注释</summary>
        private void InitAnnotations()
        {
            var area = chart1.ChartAreas[0];
            _cursor = new VerticalLineAnnotation
            {
                AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name,
                AllowMoving = true, AllowSelecting = false, LineColor = Color.Orange,
                LineWidth = 2, Visible = false
            };
            _curStart = new VerticalLineAnnotation
            {
                AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name,
                AllowMoving = true, AllowSelecting = false, LineColor = Color.DeepSkyBlue,
                LineWidth = 2, Visible = false
            };
            _curEnd = new VerticalLineAnnotation
            {
                AxisX = area.AxisX, IsInfinitive = true, ClipToChartArea = area.Name,
                AllowMoving = true, AllowSelecting = false, LineColor = Color.DeepSkyBlue,
                LineWidth = 2, Visible = false
            };
            _rangeRect = new RectangleAnnotation
            {
                AxisX = area.AxisX, AxisY = area.AxisY,
                AllowMoving = false, AllowResizing = false, LineWidth = 0,
                BackColor = Color.FromArgb(70, 25, 25, 112),   // 透明深蓝标注
                Visible = false
            };

            chart1.Annotations.Add(_cursor);
            chart1.Annotations.Add(_curStart);
            chart1.Annotations.Add(_curEnd);
            chart1.Annotations.Add(_rangeRect);
        }

        /// <summary>重建曲线点（try-finally 防布局挂起）</summary>
        private void RebuildPoints()
        {
            chart1.SuspendLayout();
            try
            {
                _series.Points.Clear();
                foreach (var e in _events) _series.Points.AddXY(e.Time, e.Step);
            }
            finally
            {
                chart1.ResumeLayout();
            }
        }

        /// <summary>默认视图：最近 1 页，游标置于视野内（无数据时初始化合理轴范围）</summary>
        private void FitWindow()
        {
            var area = chart1.ChartAreas[0];
            if (_loadingFromFile)
            {
                // 历史模式：双击退出，恢复实时内存数据
                _loadingFromFile = false;
                _events.Clear();
                _events.AddRange(EventStore.GetAll(_station));
                if (_events.Count > 0)
                {
                    _lastPointTime = _events[_events.Count - 1].Time;
                }
                RebuildPoints();
                Text = "工位 " + _station + " 实时趋势";
            }
            DateTime end = _events.Count > 0 ? _events[_events.Count - 1].Time : DateTime.Now;
            DateTime start = end.AddMilliseconds(-PageSizeMs);
            var view = area.AxisX.ScaleView;
            view.Position = start.ToOADate();
            view.Size = PageSizeDays;
            if (_events.Count == 0)
            {
                // 无数据：显式给 Y 轴合理范围，避免轴 NaN 导致图表白屏
                area.AxisY.ScaleView.Position = 0;
                area.AxisY.ScaleView.Size = 10;
            }
            PlaceCursorInView();
            PlaceRangeInView();
            UpdatePageLabel();
            _followTail = true;
        }

        /// <summary>单游标置于视野中间（不碰区域线）</summary>
        private void PlaceCursorInView()
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            _cursor.X = view.Position + view.Size / 2;
            UpdateCursorInfo();
            chart1.Invalidate();
        }

        /// <summary>区域线置于视野中间两侧（不碰单游标）</summary>
        private void PlaceRangeInView()
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            double center = view.Position + view.Size / 2;
            _curStart.X = center - view.Size * 0.15;
            _curEnd.X = center + view.Size * 0.15;
            _lastRangeStart = _curStart.X;
            _lastRangeEnd = _curEnd.X;
            SyncRangeRect();
            UpdateRangeInfo();
            chart1.Invalidate();
        }

        /// <summary>适应当前页：X/Y 轴四周预留空间（上下 10% 步数留白，左右数据范围留白）</summary>
        private void FitToData()
        {
            var area = chart1.ChartAreas[0];
            var view = area.AxisX.ScaleView;
            if (double.IsNaN(view.Position) || double.IsNaN(view.Size))
            {
                return;
            }
            // 当前窗口内数据时间范围与步号范围
            DateTime t0 = SafeFromOADate(view.Position);
            DateTime t1 = SafeFromOADate(view.Position + view.Size);
            short minY = short.MaxValue, maxY = short.MinValue;
            DateTime d0 = DateTime.MaxValue, d1 = DateTime.MinValue;
            bool found = false;
            foreach (var ev in _events)
            {
                if (ev.Time < t0) continue;
                if (ev.Time >= t1) break;
                found = true;
                if (ev.Step < minY) minY = ev.Step;
                if (ev.Step > maxY) maxY = ev.Step;
                if (ev.Time < d0) d0 = ev.Time;
                if (ev.Time > d1) d1 = ev.Time;
            }

            // X 轴：窗口 = 数据范围 + 左右 10% 留白（上限每页时长），数据居中
            if (found)
            {
                double dataSpan = (d1 - d0).TotalDays;
                double padX = dataSpan * 0.1;
                double ideal = dataSpan + padX * 2;
                if (ideal <= PageSizeDays)
                {
                    view.Size = Math.Max(ideal, 0.5 / 86400000.0);
                    view.Position = d0.ToOADate() - padX;
                }
                else
                {
                    view.Size = PageSizeDays;
                    view.Position = d1.ToOADate() - PageSizeDays;
                }
            }

            // Y 轴：min~max 上下各 10% 留白（1000 步时顶部显示 ~1100）
            if (!found)
            {
                minY = 0; maxY = 1;
            }
            double ySpan = Math.Max(maxY - minY, 1);
            double padY = ySpan * 0.1;
            area.AxisY.ScaleView.Position = minY - padY;
            area.AxisY.ScaleView.Size = ySpan + padY * 2 + 1;
            _followTail = true;   // 适应后进入跟随模式
            SyncRangeRect();
            UpdatePageLabel();
        }

        /// <summary>加载历史记录文件（records/events_日期.csv），进入历史查看模式</summary>
        private void LoadHistory()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "加载历史记录";
                dlg.Filter = "CSV 记录 (*.csv)|*.csv";
                dlg.InitialDirectory = RecordStore.RecordsDir;
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                var fileEvents = RecordStore.Load(dlg.FileName).FindAll(ev => ev.Station == _station);
                if (fileEvents.Count == 0)
                {
                    MessageBox.Show("文件中无该工位的有效数据");
                    return;
                }
                fileEvents.Sort((a, b) => a.Time.CompareTo(b.Time));
                _loadingFromFile = true;
                _events.Clear();
                _events.AddRange(fileEvents);
                _lastPointTime = _events[_events.Count - 1].Time;

                RebuildPoints();
                // 适应显示文件全部数据
                var area = chart1.ChartAreas[0];
                double start = _events[0].Time.ToOADate();
                double end = _events[_events.Count - 1].Time.ToOADate();
                area.AxisX.ScaleView.Position = start;
                area.AxisX.ScaleView.Size = Math.Max(end - start, 0.5 / 86400000.0);
                short minY = short.MaxValue, maxY = short.MinValue;
                foreach (var ev in _events)
                {
                    if (ev.Step < minY) minY = ev.Step;
                    if (ev.Step > maxY) maxY = ev.Step;
                }
                double ySpan = Math.Max(maxY - minY, 1);
                area.AxisY.ScaleView.Position = minY - ySpan * 0.1;
                area.AxisY.ScaleView.Size = ySpan * 1.2 + 1;
                _followTail = false;
                UpdatePageLabel();
                SyncRangeRect();
                chart1.Invalidate();
                Text = "工位 " + _station + " 历史记录（双击图表回实时）";
            }
        }

        /// <summary>增量拉取 + 跟随模式下窗口贴最新（历史加载模式暂停增量；异常防护防 UI 卡死）</summary>
        private void RefreshChart()
        {
            if (_loadingFromFile)
            {
                return;   // 历史模式：不追加实时数据
            }
            try
            {
                var fresh = EventStore.GetSince(_station, _lastPointTime);
                bool dataActive = PlcData.IsConnected || Simulator.IsRunning;   // 有数据源才滚动
                if (fresh.Count == 0)
                {
                    // 无新事件：数据源活跃时窗口随时间平滑滚动（防"卡死"感）；断开/无源时冻结
                    if (_followTail && dataActive)
                    {
                        var view = chart1.ChartAreas[0].AxisX.ScaleView;
                        DateTime tail = DateTime.Now;
                        view.Position = tail.AddMilliseconds(-PageSizeMs).ToOADate();
                        ExtendCurrentStep();   // 曲线右端延伸当前步
                    }
                    return;
                }
                _lastPointTime = fresh[fresh.Count - 1].Time;
                _events.AddRange(fresh);
                chart1.SuspendLayout();
                try
                {
                    foreach (var e in fresh) _series.Points.AddXY(e.Time, e.Step);
                    // 内存上限保护：超出删除最老（分页/历史查看止于数据起点，更早靠记录加载）
                    if (_events.Count > MaxEvents)
                    {
                        int remove = _events.Count - MaxEvents;
                        _events.RemoveRange(0, remove);
                        // Points 无批量删除 API，重建（低频触发，可接受）
                        _series.Points.Clear();
                        foreach (var ev in _events) _series.Points.AddXY(ev.Time, ev.Step);
                    }
                    if (_followTail && dataActive)
                    {
                        var view = chart1.ChartAreas[0].AxisX.ScaleView;
                        DateTime tail = _lastPointTime > DateTime.Now ? _lastPointTime : DateTime.Now;
                        view.Position = tail.AddMilliseconds(-PageSizeMs).ToOADate();
                        ExtendCurrentStep();   // 曲线右端延伸当前步
                    }
                }
                finally
                {
                    chart1.ResumeLayout();
                }
                UpdatePageLabel();
            }
            catch
            {
                // 任何异常不中断 Timer 消息循环（锁屏唤醒/数据异常时防止界面无响应）
            }
        }

        /// <summary>跟随模式：曲线右端延伸一个"当前步"虚拟点（时间=现在，步号=最后事件步号）</summary>
        private void ExtendCurrentStep()
        {
            if (_events.Count == 0)
            {
                return;
            }
            // 移除旧的虚拟点（Points 最后一点时间 > 最后事件时间 即虚拟点）
            if (_series.Points.Count > 0)
            {
                var last = _series.Points[_series.Points.Count - 1];
                if (last.XValue > _lastPointTime.ToOADate())
                {
                    _series.Points.RemoveAt(_series.Points.Count - 1);
                }
            }
            _series.Points.AddXY(DateTime.Now, _events[_events.Count - 1].Step);
        }

        /// <summary>窗口右端不超当前时间（防拖拽/缩放拖出空白）</summary>
        private void ClampToNow()
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            if (double.IsNaN(view.Position) || double.IsNaN(view.Size))
            {
                return;
            }
            double now = DateTime.Now.ToOADate();
            if (view.Position + view.Size > now + 0.0001)
            {
                view.Position = now - view.Size;
            }
        }

        /// <summary>分页移动（◀ 止于数据起点；▶ 到最新回跟随；无数据直接返回）</summary>
        private void PageMove(int dir)
        {
            if (_events.Count == 0 || _lastPointTime == DateTime.MinValue)
            {
                return;
            }
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            double start = _events[0].Time.ToOADate();           // 数据起点
            double tail = Math.Max(_lastPointTime.ToOADate(), DateTime.Now.ToOADate());
            if (dir < 0)   // 上一页（更早）
            {
                double newPos = view.Position - PageSizeDays;
                if (newPos < start)
                {
                    newPos = start;   // 不能翻过数据起点
                }
                view.Position = newPos;
                _followTail = false;
            }
            else           // 下一页（更晚）
            {
                double newPos = view.Position + PageSizeDays;
                if (newPos + view.Size >= tail)
                {
                    _followTail = true;   // 到最新 → 回跟随
                    view.Position = tail - PageSizeDays;
                }
                else
                {
                    view.Position = newPos;
                    _followTail = false;
                }
            }
            SyncRangeRect();
            UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            var view = chart1.ChartAreas[0].AxisX.ScaleView;
            if (double.IsNaN(view.Position) || double.IsNaN(view.Size))
            {
                lblPage.Text = "窗口: --:--:-- ~ --:--:--";
                return;
            }
            try
            {
                DateTime s = DateTime.FromOADate(view.Position);
                DateTime e = DateTime.FromOADate(view.Position + view.Size);
                lblPage.Text = "窗口: " + s.ToString("HH:mm:ss") + " ~ " + e.ToString("HH:mm:ss");
            }
            catch
            {
                lblPage.Text = "窗口: --:--:-- ~ --:--:--";
            }
        }

        /// <summary>滚轮缩放：X/Y 双轴锚点（布局未就绪或鼠标在绘图区外时跳过）</summary>
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
                return;   // 图表布局未就绪（刚打开/布局挂起期间），跳过本次缩放
            }
            if (double.IsNaN(mouseX) || double.IsNaN(mouseY))
            {
                return;   // 鼠标不在绘图区，跳过本次缩放
            }
            double factor = e.Delta > 0 ? 0.8 : 1.25;
            var viewX = area.AxisX.ScaleView;
            double xSize = viewX.Size;
            // 先算新宽度（含 clamp：下限 0.5ms / 上限每页时长），再按实际比例定锚点
            double newXSize = Math.Min(Math.Max(xSize * factor, 0.5 / 86400000.0), PageSizeDays);
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
            _followTail = false;
            ClampToNow();   // 右端不超当前时间
        }

        /// <summary>鼠标按下：仅绘图区内启动拖拽平移；游标在视野外时滚动到游标</summary>
        private void OnChartMouseDown(object sender, MouseEventArgs e)
        {
            var hit = chart1.HitTest(e.X, e.Y);
            if (hit.ChartElementType == ChartElementType.Annotation && hit.Object is VerticalLineAnnotation va)
            {
                // 点击游标，若在视野外则滚动到视野内
                double x = va.X;
                var view = chart1.ChartAreas[0].AxisX.ScaleView;
                if (x < view.Position || x > view.Position + view.Size)
                {
                    view.Position = x - PageSizeDays / 2;
                    _followTail = false;
                    SyncRangeRect();
                    UpdatePageLabel();
                }
                _draggingWhich = hit.Object == _cursor ? 0 : (hit.Object == _curStart ? 1 : 2);   // 进入拖动状态（对应线标签 Y 跟随鼠标）
                return;   // 注释拖动由 Chart 默认处理
            }
            // 只在绘图区内启动拖拽平移（轴区/图例区不干扰）
            // Position/InnerPlotPosition 均为百分比（0~100），换算像素需 /100
            var ca = chart1.ChartAreas[0];
            var outer = ca.Position;
            var inner = ca.InnerPlotPosition;
            var plotRect = new Rectangle(
                (int)(chart1.Width * (outer.X + outer.Width * inner.X / 100) / 100),
                (int)(chart1.Height * (outer.Y + outer.Height * inner.Y / 100) / 100),
                (int)(chart1.Width * outer.Width * inner.Width / 10000),
                (int)(chart1.Height * outer.Height * inner.Height / 10000));
            if (!plotRect.Contains(e.Location))
            {
                return;
            }
            // 开始拖拽平移（X/Y 双轴）
            var area = chart1.ChartAreas[0];
            _isPanning = true;
            _panStart = e.Location;
            _panViewStart = area.AxisX.ScaleView.Position;
            // Y 轴缩放状态可能未激活（NaN），取当前可视范围作为平移基准
            _panYSizeStart = double.IsNaN(area.AxisY.ScaleView.Size)
                ? (area.AxisY.Maximum - area.AxisY.Minimum)
                : area.AxisY.ScaleView.Size;
            _panYViewStart = double.IsNaN(area.AxisY.ScaleView.Position)
                ? area.AxisY.Minimum
                : area.AxisY.ScaleView.Position;
        }

        /// <summary>鼠标移动：记录位置（步号标签跟随）；拖拽平移；锁定区域联动</summary>
        private void OnChartMouseMove(object sender, MouseEventArgs e)
        {
            _mousePos = e.Location;
            // 拖动中：只有被拖动的线标签跟随鼠标（上方 68px，靠近顶部翻到下方）
            if (_draggingWhich >= 0)
            {
                float y = _mousePos.Y > 70 ? _mousePos.Y - 68 : _mousePos.Y + 12;
                if (_draggingWhich == 0) _cursorLabelY = y;
                else if (_draggingWhich == 1) _startLabelY = y;
                else if (_draggingWhich == 2) _endLabelY = y;
            }
            // 游标/区域开启时实时刷新标签（跟随鼠标，拖动游标时不被遮挡）
            if ((chkCursor.Checked || chkRange.Checked) && _events.Count > 0)
            {
                chart1.Invalidate();
            }
            if (_isPanning)
            {
                var area = chart1.ChartAreas[0];
                double dx = -(e.X - _panStart.X) / (double)chart1.Width * area.AxisX.ScaleView.Size;
                area.AxisX.ScaleView.Position = _panViewStart + dx;
                double dy = (e.Y - _panStart.Y) / (double)chart1.Height * _panYSizeStart;
                area.AxisY.ScaleView.Position = _panYViewStart + dy;
                area.AxisY.ScaleView.Size = _panYSizeStart;
                _followTail = false;
                ClampToNow();   // 右端不超当前时间（防拖出空白）
                SyncRangeRect();
                UpdatePageLabel();
                return;
            }
            // 锁定区域：拖动一根线时另一根同步（保持区域长度，整体平移）
            if (chkLockRange.Checked && chkRange.Checked)
            {
                double dStart = _curStart.X - _lastRangeStart;
                double dEnd = _curEnd.X - _lastRangeEnd;
                if (Math.Abs(dStart) > 1e-12 && Math.Abs(dEnd) < Math.Abs(dStart) * 0.1)
                {
                    _curEnd.X += dStart;            // start 移动 → end 跟随
                }
                else if (Math.Abs(dEnd) > 1e-12 && Math.Abs(dStart) < Math.Abs(dEnd) * 0.1)
                {
                    _curStart.X += dEnd;            // end 移动 → start 跟随
                }
                _lastRangeStart = _curStart.X;
                _lastRangeEnd = _curEnd.X;
                SyncRangeRect();
                UpdateRangeInfo();
                chart1.Invalidate();
            }
        }

        /// <summary>安全 OADate 转换（NaN/越界返回 MinValue，防 FromOADate 抛异常）</summary>
        private static DateTime SafeFromOADate(double v)
        {
            if (double.IsNaN(v) || v < 0 || v > 2958465.99)
            {
                return DateTime.MinValue;
            }
            return DateTime.FromOADate(v);
        }

        /// <summary>游标信息更新：步号标签跟随线 + 底部信息栏</summary>
        private void UpdateCursorInfo()
        {
            if (!chkCursor.Checked) return;
            var area = chart1.ChartAreas[0];
            DateTime t = SafeFromOADate(_cursor.X);
            short step = FindStepAt(t);
            string stepText = step < 0 ? "--" : step.ToString();
            string info = "游标 " + t.ToString("HH:mm:ss.fff") + "  步号 " + stepText;
            if (chkRange.Checked)
            {
                DateTime t1 = SafeFromOADate(_curStart.X);
                DateTime t2 = SafeFromOADate(_curEnd.X);
                info += "   |   " + RangeText(t1, t2);
            }
            lblCursorInfo.Text = info;
        }

        /// <summary>自绘游标/区域步号标签（多标签重叠自动分层错开，Y 跟随鼠标）</summary>
        private void OnChartPaint(object sender, PaintEventArgs e)
        {
            // 整体防护：锁屏/睡眠唤醒后布局可能未就绪，ValueToPixelPosition 会抛异常，
            // 未捕获会导致 UI 消息循环中断（界面无响应）。异常时跳过自绘标签即可。
            try
            {
                var area = chart1.ChartAreas[0];
                if (_events.Count == 0)
                {
                    // 无数据提示
                    using (var font = new Font("微软雅黑", 12f))
                    using (var brush = new SolidBrush(Color.Gray))
                    {
                        string msg = "暂无数据";
                        SizeF sz = e.Graphics.MeasureString(msg, font);
                        e.Graphics.DrawString(msg, font, brush,
                            (chart1.Width - sz.Width) / 2, (chart1.Height - sz.Height) / 2);
                    }
                    return;
                }
                var tags = new List<TagItem>();
                if (chkCursor.Checked && !double.IsNaN(_cursor.X))
                {
                    tags.Add(new TagItem(_cursor.X, StepText(_cursor.X), Color.Orange, _cursorLabelY));
                }
                if (chkRange.Checked)
                {
                    if (!double.IsNaN(_curStart.X))
                    {
                        tags.Add(new TagItem(_curStart.X, StepText(_curStart.X), Color.DeepSkyBlue, _startLabelY));
                    }
                    if (!double.IsNaN(_curEnd.X))
                    {
                        tags.Add(new TagItem(_curEnd.X, StepText(_curEnd.X), Color.DeepSkyBlue, _endLabelY));
                    }
                    DrawRangeDuration(e.Graphics, area);   // 区域时长画在图表内
                }
                if (tags.Count > 0)
                {
                    DrawStepTags(e.Graphics, area, tags);
                }
            }
            catch (InvalidOperationException)
            {
                // 图表布局未就绪（唤醒/缩放中），跳过自绘，Chart 自身绘制不受影响
            }
            catch
            {
                // 其他绘制异常同样跳过，绝不中断 UI 消息循环
            }
        }

        private struct TagItem
        {
            public double X;
            public string Text;
            public Color Color;
            public float BaseY;   // 该线标签的基准 Y（-1 = 顶部）
            public TagItem(double x, string text, Color color, float baseY)
            { X = x; Text = text; Color = color; BaseY = baseY; }
        }

        private string StepText(double xOADate)
        {
            short step = FindStepAt(SafeFromOADate(xOADate));
            return step < 0 ? "步号 --" : "步号 " + step;
        }

        /// <summary>分层绘制步号标签：每根线独立基准 Y（拖动时跟随鼠标），重叠自动向下错开</summary>
        private void DrawStepTags(Graphics g, ChartArea area, List<TagItem> tags)
        {
            float topBase = (float)(area.Position.Y / 100 * chart1.Height + 2);   // 顶部基准
            double plotLeft = area.Position.X / 100 * chart1.Width;

            // 计算每个标签像素范围（BaseY = -1 用顶部）
            var items = new List<TagLayout>();
            foreach (var t in tags)
            {
                float xPix = (float)area.AxisX.ValueToPixelPosition(t.X);
                SizeF sz = g.MeasureString(t.Text, _labelFont);
                float l = xPix - sz.Width / 2 - 4;
                if (l < plotLeft) l = (float)plotLeft;
                float baseY = t.BaseY >= 0 ? t.BaseY : topBase;
                items.Add(new TagLayout(l, l + sz.Width + 8, t.Text, t.Color, baseY));
            }
            items.Sort((a, b) => a.Left.CompareTo(b.Left));

            // 分层：按 x 重叠判断，从各自基准 Y 向下错开（每层 24px）
            var placed = new List<TagLayout>();
            foreach (var it in items)
            {
                int layer = 0;
                foreach (var p in placed)
                {
                    if (it.Left <= p.Right + 4 && it.Right >= p.Left - 4)
                    {
                        layer = Math.Max(layer, p.Layer + 1);
                    }
                }
                float y = it.BaseY + layer * 24;
                using (var bg = new SolidBrush(it.Color))
                {
                    var rect = new RectangleF(it.Left, y, it.Right - it.Left, 20);
                    g.FillRectangle(bg, rect);
                    g.DrawString(it.Text, _labelFont, Brushes.White, it.Left + 4, y + 2);
                }
                placed.Add(new TagLayout(it.Left, it.Right, it.Text, it.Color, it.BaseY, layer));
            }
        }

        private struct TagLayout
        {
            public float Left;
            public float Right;
            public string Text;
            public Color Color;
            public float BaseY;
            public int Layer;
            public TagLayout(float left, float right, string text, Color color, float baseY, int layer = 0)
            { Left = left; Right = right; Text = text; Color = color; BaseY = baseY; Layer = layer; }
        }

        /// <summary>区域时长文本画在区域中间（顶部标签行下方），单位 ms</summary>
        private void DrawRangeDuration(Graphics g, ChartArea area)
        {
            double x1 = Math.Min(_curStart.X, _curEnd.X);
            double x2 = Math.Max(_curStart.X, _curEnd.X);
            double midX = (x1 + x2) / 2;
            double durMs = (x2 - x1) * 86400000.0;
            string text = durMs.ToString("F0") + " ms";
            double xPix = area.AxisX.ValueToPixelPosition(midX);
            double topPix = area.Position.Y / 100 * chart1.Height + 24;   // 标签行下方

            using (var bg = new SolidBrush(Color.FromArgb(180, 25, 25, 112)))
            {
                SizeF sz = g.MeasureString(text, _labelFont);
                float left = (float)xPix - sz.Width / 2 - 4;
                double plotLeft = area.Position.X / 100 * chart1.Width;
                if (left < plotLeft) left = (float)plotLeft;
                var rect = new RectangleF(left, (float)topPix, sz.Width + 8, sz.Height + 4);
                g.FillRectangle(bg, rect);
                g.DrawString(text, _labelFont, Brushes.White, rect.X + 4, rect.Y + 2);
            }
        }

        /// <summary>区域游标：更新高亮矩形与底部信息</summary>
        private void UpdateRangeInfo()
        {
            SyncRangeRect();
            if (!chkRange.Checked) return;
            DateTime t1 = SafeFromOADate(_curStart.X);
            DateTime t2 = SafeFromOADate(_curEnd.X);
            lblCursorInfo.Text = "区域 " + RangeText(t1, t2);
        }

        private void SyncRangeRect()
        {
            var area = chart1.ChartAreas[0];
            // 用 ScaleView 可视范围（含 NaN/Infinity 防护），保证矩形覆盖整个绘图区
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

        private string RangeText(DateTime t1, DateTime t2)
        {
            TimeSpan d = (t2 - t1).Duration();
            return t1.ToString("HH:mm:ss.fff") + " ~ " + t2.ToString("HH:mm:ss.fff")
                   + "  时长 " + d.TotalSeconds.ToString("F3") + " s";
        }

        /// <summary>二分查找：t 时刻所处的步号</summary>
        private short FindStepAt(DateTime t)
        {
            int lo = 0, hi = _events.Count - 1, ans = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_events[mid].Time <= t) { ans = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            return ans >= 0 ? _events[ans].Step : (short)-1;
        }
    }
}