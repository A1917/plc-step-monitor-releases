using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using HslCommunication;
using HslCommunication.Profinet.Omron;

namespace Test
{
    /// <summary>
    /// 主窗体：PLC IP/工位数配置、连接/断开控制、心跳监控、设备状态显示，MDI 容器承载工位监控子窗体。
    /// </summary>
    public partial class Frm_Main : Form
    {
        private const int PlcPort = 9600;                 // FINS-TCP 固定端口

        private bool isConnected;
        private Thread _stateThd;                          // 设备状态轮询线程（1s 一次）
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Frm_StepInfo _stepInfo;                    // 工位监控子窗体

        public Frm_Main()
        {
            InitializeComponent();
            InitializeChildForm();
        }

        /// <summary>创建并显示工位监控子窗体（MDI）</summary>
        private void InitializeChildForm()
        {
            _stepInfo = new Frm_StepInfo
            {
                MdiParent = this,
                Dock = DockStyle.Fill,
                TopMost = false
            };
            _stepInfo.Show();
        }

        /// <summary>连接 / 断开切换</summary>
        private void btnConnect_Click(object sender, System.EventArgs e)
        {
            try
            {
                if (!isConnected)
                {
                    ConnectPlc();
                }
                else
                {
                    DisconnectPlc();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("操作异常：" + ex.Message);
            }
        }

        private void ConnectPlc()
        {
            // 读取主界面配置：PLC IP + 工位数
            string ip = txtPlcIp.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                ip = "192.168.1.50";
                txtPlcIp.Text = ip;
            }
            if (!int.TryParse(textBox1.Text.Trim(), out int stepCount) || stepCount <= 0)
            {
                stepCount = 50;
                textBox1.Text = "50";
            }
            if (stepCount > 200)
            {
                MessageBox.Show("工位数过大（上限 200）");
                return;
            }

            // 应用工位数（重建子窗体网格 + 数据数组）
            _stepInfo.ApplyConfig(stepCount);

            // 按 IP 创建客户端并连接
            PlcData.omronFinsNet = new OmronFinsNet(ip, PlcPort);
            OperateResult result = PlcData.omronFinsNet.ConnectServer();
            if (result.IsSuccess)
            {
                isConnected = true;
                PlcData.IsConnected = true;
                timer1.Enabled = true;
                btnConnect.Text = "断开连接";
                lbConnectState.BackColor = Color.Green;
                lbConnectState.Text = "连接成功";
                MessageBox.Show("连接成功");
            }
            else
            {
                PlcData.omronFinsNet = null;
                MessageBox.Show("连接失败：" + result.Message);
            }
        }

        private void DisconnectPlc()
        {
            timer1.Enabled = false;
            if (PlcData.omronFinsNet != null)
            {
                PlcData.omronFinsNet.ConnectClose();
            }
            PlcData.omronFinsNet = null;      // 置空，重连时按新 IP 重建
            isConnected = false;
            PlcData.IsConnected = false;
            btnConnect.Text = "连接";
            lbConnectState.BackColor = Color.Red;
            lbConnectState.Text = "未连接";
            lbl_MachineState.Text = "未连接";
            lbl_MachineState.BackColor = Color.Yellow;
        }

        /// <summary>心跳监控（timer1 周期触发），PLC 掉线/异常时红灯</summary>
        private void CheckHeartBeat(object sender, System.EventArgs e)
        {
            try
            {
                if (!PlcData.IsConnected || PlcData.omronFinsNet == null)
                {
                    return;
                }
                bool alive = PlcData.omronFinsNet.ReadBool("D10000.1").Content;
                label1.BackColor = alive ? Color.Green : Color.Red;
            }
            catch
            {
                label1.BackColor = Color.Red;
            }
        }

        /// <summary>读取设备状态字 D10：1=正常运行 2=停止运行 3=故障调试 4=闲置待机</summary>
        private void ShowStateEven_Run()
        {
            short status = 0;
            try
            {
                if (isConnected && PlcData.omronFinsNet != null)
                {
                    status = PlcData.omronFinsNet.ReadInt16("D10").Content;
                }
            }
            catch
            {
                return;
            }

            string desc = "未知状态";
            Color stateColor = Color.White;
            switch (status)
            {
                case 1: desc = "正常运行"; stateColor = Color.Green; break;
                case 2: desc = "停止运行"; stateColor = Color.OrangeRed; break;
                case 3: desc = "故障调试"; stateColor = Color.Yellow; break;
                case 4: desc = "闲置待机"; stateColor = Color.LightGreen; break;
            }
            SafeSetStatus(desc, stateColor);
        }

        /// <summary>跨线程安全更新状态标签</summary>
        private void SafeSetStatus(string desc, Color color)
        {
            if (IsDisposed)
            {
                return;
            }
            try
            {
                BeginInvoke((Action)delegate
                {
                    if (IsDisposed || lbl_MachineState == null)
                    {
                        return;
                    }
                    lbl_MachineState.Text = isConnected ? desc : "未连接";
                    lbl_MachineState.BackColor = isConnected ? color : Color.Yellow;
                });
            }
            catch (System.ObjectDisposedException) { }
            catch (System.InvalidOperationException) { }
        }

        /// <summary>模拟数据开关：驱动最后一个工位按模拟周期序列步进（验证趋势图/网格用）</summary>
        private void chkSimulate_CheckedChanged(object sender, System.EventArgs e)
        {
            if (chkSimulate.Checked)
            {
                Simulator.Start(PlcData.StepCount - 1);
            }
            else
            {
                Simulator.Stop();
            }
        }

        /// <summary>记录开关：每次开启新建独立记录文件（前缀=记录名输入框，默认 PLCStep）</summary>
        private void chkRecord_CheckedChanged(object sender, System.EventArgs e)
        {
            if (chkRecord.Checked)
            {
                RecordStore.Start(txtRecordPrefix.Text);
            }
            else
            {
                RecordStore.Stop();
            }
        }

        /// <summary>加载历史记录到全局缓存（多选 CSV 文件，按时间合并）</summary>
        private void btnLoadHistory_Click(object sender, System.EventArgs e)
        {
            using (var dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Title = "选择历史记录文件（可多选）";
                dlg.Filter = "CSV 记录 (*.csv)|*.csv";
                dlg.InitialDirectory = RecordStore.RecordsDir;
                dlg.Multiselect = true;
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                var data = RecordStore.LoadMultiple(dlg.FileNames);
                if (data.Count == 0)
                {
                    System.Windows.Forms.MessageBox.Show("文件无有效数据");
                    return;
                }
                EventStore.LoadedHistory = data;
                EventStore.HistoryMode = true;   // 加载后自动切到历史模式
                btnToggleMode.Text = "显示实时";
                System.Windows.Forms.MessageBox.Show("已加载 " + data.Count + " 条事件记录\n双击工位格子查看历史趋势图");
            }
        }

        /// <summary>打开多工位总览趋势图</summary>
        private void btnMultiTrend_Click(object sender, System.EventArgs e)
        {
            using (var frm = new Frm_MultiTrend())
            {
                frm.ShowDialog(this);
            }
        }

        /// <summary>检查更新：从 GitHub Release 获取最新版本</summary>
        private void btnCheckUpdate_Click(object sender, System.EventArgs e)
        {
            btnCheckUpdate.Enabled = false;
            btnCheckUpdate.Text = "检查中...";
            UpdateChecker.CheckAsync((hasUpdate, tag, url) =>
            {
                BeginInvoke((Action)delegate
                {
                    btnCheckUpdate.Enabled = true;
                    btnCheckUpdate.Text = "检查更新";
                    if (hasUpdate)
                    {
                        var result = MessageBox.Show("发现新版本 " + tag + "\n\n是否立即下载更新？",
                            "更新可用", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            UpdateChecker.DownloadAndApply(url, tag);
                        }
                    }
                    else if (tag == null)
                    {
                        MessageBox.Show("检查更新失败：网络异常或 GitHub 不可达", "更新错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("当前已是最新版本 " + tag, "无需更新",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                });
            });
        }

        /// <summary>切换显示模式：实时 / 历史文件</summary>
        private void btnToggleMode_Click(object sender, System.EventArgs e)
        {
            if (EventStore.LoadedHistory == null)
            {
                System.Windows.Forms.MessageBox.Show("请先加载历史文件");
                return;
            }
            EventStore.HistoryMode = !EventStore.HistoryMode;
            btnToggleMode.Text = EventStore.HistoryMode ? "显示实时" : "显示历史";
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            FormClosing += Form1_FormClosing;
            _stateThd = new Thread(StateLoop) { IsBackground = true };
            _stateThd.Start();
        }

        /// <summary>设备状态轮询循环（1s 一次），支持协作式取消</summary>
        private void StateLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                ShowStateEven_Run();
                _cts.Token.WaitHandle.WaitOne(1000);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Enabled = false;
            _cts.Cancel();
            if (_stateThd != null && _stateThd.IsAlive)
            {
                _stateThd.Join(2000);
            }
            _cts.Dispose();   // 释放内核句柄（WaitHandle）
            try
            {
                if (PlcData.omronFinsNet != null)
                {
                    PlcData.omronFinsNet.ConnectClose();
                }
            }
            catch { }
        }
    }
}
