using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using HslCommunication;
using HslCommunication.Profinet.Omron;

namespace Test
{
    /// <summary>
    /// 主窗体：连接/断开控制、心跳监控、设备状态显示，MDI 容器承载工位监控子窗体。
    /// </summary>
    public partial class Frm_Main : Form
    {
        private bool isConnected;
        private Thread _stateThd;                          // 设备状态轮询线程（1s 一次）
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public Frm_Main()
        {
            InitializeComponent();
            InitializeChildForm();
        }

        /// <summary>创建并显示工位监控子窗体（MDI）</summary>
        private void InitializeChildForm()
        {
            var stepInfo = new Frm_StepInfo
            {
                MdiParent = this,
                Dock = DockStyle.Fill,
                TopMost = false
            };
            stepInfo.Show();
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
            if (PlcData.omronFinsNet == null)
            {
                PlcData.omronFinsNet = new OmronFinsNet("192.168.1.50", 9600);
            }
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
