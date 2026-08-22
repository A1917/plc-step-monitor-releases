using System;
using System.Collections.Generic;

namespace Test
{
    /// <summary>一次步号变化事件（趋势图/记录的最小数据单元）</summary>
    public class StepEvent
    {
        public DateTime Time;
        public int Station;
        public short Step;

        public StepEvent(DateTime time, int station, short step)
        {
            Time = time;
            Station = station;
            Step = step;
        }
    }

    /// <summary>
    /// 步号变化事件库：轮询线程喂入步值快照，内部检测变化生成事件，
    /// 每工位一个环形缓冲（内存），趋势图窗体实时读取。
    /// 设计：只存变化事件（非全量采样），数据量小且天然是台阶曲线数据。
    /// </summary>
    public static class EventStore
    {
        /// <summary>每工位环形缓冲容量（约 14 小时 @ 1 事件/秒）</summary>
        public const int BufferCapacity = 50000;

        private static readonly Dictionary<int, Queue<StepEvent>> _buffers = new Dictionary<int, Queue<StepEvent>>();
        private static readonly short[] _lastStep = new short[PlcData.StepCount];
        private static readonly bool[] _initialized = new bool[PlcData.StepCount];
        private static readonly object _lock = new object();

        /// <summary>
        /// 轮询线程调用：喂入步值快照，与上次对比，变化则生成事件（时间戳=采样时刻）。
        /// 首次出现的工位只建立基线，不生成事件（避免启动瞬间的假变化）。
        /// </summary>
        public static void Feed(short[] steps)
        {
            lock (_lock)
            {
                int count = Math.Min(steps.Length, _lastStep.Length);
                for (int i = 0; i < count; i++)
                {
                    short v = steps[i];
                    if (!_initialized[i])
                    {
                        _lastStep[i] = v;
                        _initialized[i] = true;
                        continue;   // 首次：只建基线
                    }
                    if (v != _lastStep[i])
                    {
                        _lastStep[i] = v;
                        AddEvent(i, v);
                    }
                }
            }
        }

        private static void AddEvent(int station, short step)
        {
            Queue<StepEvent> q;
            if (!_buffers.TryGetValue(station, out q))
            {
                q = new Queue<StepEvent>();
                _buffers[station] = q;
            }
            q.Enqueue(new StepEvent(DateTime.Now, station, step));
            while (q.Count > BufferCapacity)
            {
                q.Dequeue();   // 环形：超出容量丢弃最老
            }
        }

        /// <summary>
        /// 取某工位自 since 时刻之后的事件（时间升序，含 since 之后全部）。
        /// 趋势图定时器调用，增量拉取。
        /// </summary>
        public static List<StepEvent> GetSince(int station, DateTime since)
        {
            lock (_lock)
            {
                var result = new List<StepEvent>();
                Queue<StepEvent> q;
                if (!_buffers.TryGetValue(station, out q))
                {
                    return result;
                }
                foreach (var e in q)
                {
                    if (e.Time > since)
                    {
                        result.Add(e);
                    }
                }
                return result;
            }
        }

        /// <summary>取某工位全部缓冲事件（加载历史/导出用）</summary>
        public static List<StepEvent> GetAll(int station)
        {
            lock (_lock)
            {
                var result = new List<StepEvent>();
                Queue<StepEvent> q;
                if (_buffers.TryGetValue(station, out q))
                {
                    result.AddRange(q);
                }
                return result;
            }
        }

        /// <summary>清空全部缓冲（重建网格/断开重连时调用）</summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _buffers.Clear();
                for (int i = 0; i < _lastStep.Length; i++)
                {
                    _lastStep[i] = 0;
                    _initialized[i] = false;
                }
            }
        }
    }
}
