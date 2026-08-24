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
        private readonly Timer _timer;

        public Frm_MultiTrend()
        {
            InitializeComponent();
            _data = EventStore.LoadedHistory ?? new List<StepEvent>();
            FillStationList();
            InitChart();
            RebuildSeries();
            Text = "多工位趋势图（双击图例改色）";
            _timer = new Timer { Interval = 100 };
            _timer.Tick += (s, e) => RefreshChart();
            _timer.Start();
            FormClosing += (s, e) => _timer.Stop();
        }

        private void FillStationList()
        {
            int maxStation = _data.Count > 0 ? _data.Max(ev => ev.Station) : PlcData.StepCount - 1;
            for (int i = 0; i <= maxStation; i++)
            {
                int cnt = _data.Count(ev => ev.Station == i);
                chkStations.Items.Add("工位 " + i + " (" + cnt + ")", cnt > 0);
            }
            // 默认选前 10 个
            for (int i = 0; i < chkStations.Items.Count && i < 10; i++)
            {
                chkStations.SetItemChecked(i, true);
            }
            chkStations.ItemCheck += (s, e) =>
            {
                // 最多选 10 个
                if (e.NewValue == CheckState.Checked && chkStations.CheckedItems.Count >= 10)
                {
                    e.NewValue = CheckState.Unchecked;
                }
            };
            chkStations.SelectedIndexChanged += (s, e) => RebuildSeries();
        }

        private void InitChart()
        {
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
                        }
                    }
                }
            };
        }

        private void RebuildSeries()
        {
            chart1.Series.Clear();
            int n = chkStations.CheckedItems.Count;
            if (n == 0) return;
            int idx = 0;
            short gMinY = short.MaxValue, gMaxY = short.MinValue;
            double gStart = double.MaxValue, gEnd = double.MinValue;
            foreach (int i in chkStations.CheckedIndices)
            {
                var events = _data.FindAll(ev => ev.Station == i);
                if (events.Count == 0) continue;
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
            // 适应 Y 轴
            if (gMinY != short.MaxValue)
            {
                double span = Math.Max(gMaxY - gMinY, 1);
                chart1.ChartAreas[0].AxisY.ScaleView.Position = gMinY - span * 0.1;
                chart1.ChartAreas[0].AxisY.ScaleView.Size = span * 1.2 + 1;
            }
            // 适应 X 轴
            if (gStart != double.MaxValue)
            {
                chart1.ChartAreas[0].AxisX.ScaleView.Position = gStart;
                chart1.ChartAreas[0].AxisX.ScaleView.Size = Math.Max(gEnd - gStart, 0.5 / 86400000.0);
            }
        }

        private void RefreshChart()
        {
            // 多工位实时刷新（当前为静态数据源，暂不实现增量）
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = ((int)(hue / 60)) % 6;
            double f = hue / 60 - hi;
            double p = value * (1 - saturation);
            double q = value * (1 - f * saturation);
            double t = value * (1 - (1 - f) * saturation);
            int r = (int)(value * 255), g = (int)(value * 255), b = (int)(value * 255);
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