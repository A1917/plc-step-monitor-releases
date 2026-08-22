using HslCommunication.Profinet.Omron;

namespace Test
{
    /// <summary>全局 PLC 数据共享：连接客户端、工位步值、连接状态。</summary>
    public static class PlcData
    {
        /// <summary>工位数（= 读取的寄存器个数，D10002 起连续），主界面可配置，默认 50</summary>
        public static int StepCount = 50;

        /// <summary>欧姆龙 FINS-TCP 客户端（点击连接时按主界面 IP 创建，断开后置空以便重建）</summary>
        public static OmronFinsNet omronFinsNet;

        /// <summary>各工位当前流程步号（长度 = StepCount，随配置重建）</summary>
        public static short[] ThdStep = new short[50];

        /// <summary>连接状态</summary>
        public static bool IsConnected { get; set; }
    }
}
