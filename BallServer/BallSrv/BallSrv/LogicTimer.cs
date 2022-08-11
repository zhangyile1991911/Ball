using System;
using System.Diagnostics;

namespace BallSrv
{
    public  class LogicTimer
    {
        public const float FramesPerSecond = 30.0f;
        public const float FixedDelta = 1.0f / FramesPerSecond;

        private double m_accumulator;
        private long m_lastTime;

        private readonly Stopwatch m_stopwatch;
        private readonly Action m_action;

        public float LerpAlpha => (float)m_accumulator / FixedDelta;

        public LogicTimer(Action action)
        {
            m_stopwatch = new Stopwatch();
            m_action = action;
        }

        public void Start()
        {
            m_lastTime = 0;
            m_accumulator = 0.0;
            m_stopwatch.Restart();
        }

        public void Stop()
        {
            m_stopwatch.Stop();
        }

        public void Update()
        {
            //从程序开始到现在过了多少tick
            long elapsedTicks = m_stopwatch.ElapsedTicks;
            //当前tick-上一次tick 除以 频率 =时间
            m_accumulator += (double)(elapsedTicks - m_lastTime) / Stopwatch.Frequency;
            m_lastTime = elapsedTicks;

            while(m_accumulator >= FixedDelta)
            {
                m_action();
                m_accumulator -= FixedDelta;
            }
        }

    }
}
