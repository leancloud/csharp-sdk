using System;
namespace LeanCloud.Realtime.Internal
{
    public interface IAVTimer
    {
        /// <summary>
        /// Start this timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop this timer.
        /// </summary>
        void Stop();

        bool Enabled { get; set; }

        /// <summary>
        /// The number of milliseconds between timer events.
        /// </summary>
        /// <value>The interval.</value>
        double Interval { get; set; }

        /// <summary>
        /// 已经执行了多少次
        /// </summary>
        long Executed { get; }

        /// <summary>
        /// Occurs when elapsed.
        /// </summary>
        event EventHandler<TimerEventArgs> Elapsed;
    }
    /// <summary>
    /// Timer event arguments.
    /// </summary>
    public class TimerEventArgs : EventArgs
    {
        public TimerEventArgs(DateTime signalTime)
        {
            SignalTime = signalTime;
        }
        public DateTime SignalTime
        {
            get;
            private set;
        }
    }
}
