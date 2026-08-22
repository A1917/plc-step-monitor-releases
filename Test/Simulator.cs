using System;
using System.Threading;

namespace Test
{
    /// <summary>
    /// 模拟数据源：驱动一个模拟工位按周期序列步进，喂入 EventStore，
    /// 用于无 PLC 时验证趋势图/网格/周期判定等显示与记录功能。
    /// 序列：0→10→100→500→0→1000→0 循环（模拟真实流程的周期重复），
    /// 每步停留随机 300~1500ms。
    /// </summary>
    public static class Simulator
    {
        private static readonly short[] Sequence = { 0, 10, 100, 500, 0, 1000, 0 };
        private static readonly Random _rnd = new Random();
        private static readonly Timer _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);

        private static int _station = -1;      // 被模拟的工位号
        private static int _seqIdx;
        private static short _current;

        /// <summary>模拟开关是否开启</summary>
        public static bool IsRunning { get; private set; }

        /// <summary>被模拟的工位号（-1 = 未启动）</summary>
        public static int Station
        {
            get { lock (_lockObj) { return _station; } }
        }

        /// <summary>模拟工位当前步号（轮询线程用于保持 ThdStep 显示一致性）</summary>
        public static short CurrentValue
        {
            get { lock (_lockObj) { return _current; } }
        }

        private static readonly object _lockObj = new object();

        /// <summary>启动模拟（指定工位号）</summary>
        public static void Start(int station)
        {
            lock (_lockObj)
            {
                if (IsRunning)
                {
                    return;
                }
                _station = station;
                _seqIdx = 0;
                _current = Sequence[0];
                IsRunning = true;
            }
            EventStore.FeedSingle(station, _current);   // 建立基线
            _timer.Change(300, Timeout.Infinite);
        }

        /// <summary>停止模拟</summary>
        public static void Stop()
        {
            lock (_lockObj)
            {
                IsRunning = false;
                _station = -1;
            }
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private static void OnTick(object state)
        {
            int station;
            short next;
            int nextDelay;
            lock (_lockObj)
            {
                if (!IsRunning)
                {
                    return;
                }
                _seqIdx = (_seqIdx + 1) % Sequence.Length;
                _current = Sequence[_seqIdx];
                next = _current;
                station = _station;
                nextDelay = _rnd.Next(300, 1501);   // 步停留 300~1500ms
            }
            EventStore.FeedSingle(station, next);
            _timer.Change(nextDelay, Timeout.Infinite);
        }
    }
}
