using System;
using System.Threading.Tasks;
using System.Threading;

namespace LeanCloud.Realtime.Internal
{
    internal delegate void TimerCallback();

    internal sealed class Timer : CancellationTokenSource, IDisposable
    {
        TimerCallback exe;
        int Interval { get; set; }
        
        internal Timer(TimerCallback callback, int interval, bool enable)
        {
            exe = callback;
            Interval = interval;

            Enabled = enable;
            Execute();
        }

        Task Execute()
        {
            if (Enabled)
                return Task.Delay(Interval).ContinueWith(t =>
                {
                    if (!Enabled)
                        return null;
                    exe();
                    return this.Execute();
                });
            else
                return Task.FromResult(0);
        }

        volatile bool enabled;
        public bool Enabled
        {
            get {
                return enabled;
            } set {
                enabled = value;
            }
        }
    }

    public class AVTimer : IAVTimer
    {
        public AVTimer()
        {

        }

        Timer timer;

        public bool Enabled
        {
            get
            {
                return timer.Enabled;
            }
            set
            {
                timer.Enabled = value;
            }
        }

        public double Interval
        {
            get; set;
        }

        long executed;

        public long Executed
        {
            get
            {
                return executed;
            }

            internal set
            {
                executed = value;
            }
        }

        public void Start()
        {
            if (timer == null)
            {
                timer = new Timer(() =>
                {
                    Elapsed(this, new TimerEventArgs(DateTime.Now));
                }, (int)Interval, true);
            }
        }

        public void Stop()
        {
            if (timer != null) timer.Enabled = false;
        }

        public event EventHandler<TimerEventArgs> Elapsed;
    }
}
