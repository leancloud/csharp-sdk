using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RealtimeApp {
    /// <summary>
    /// Mimics UI thread under .Net console.
    /// </summary>
    public class SingleThreadSynchronizationContext : SynchronizationContext {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> queue = new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

        public override void Post(SendOrPostCallback d, object state) {
            queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void RunOnCurrentThread() {
            while (queue.TryTake(out KeyValuePair<SendOrPostCallback, object> workItem, Timeout.Infinite)) {
                workItem.Key(workItem.Value);
            }
        }

        public void Complete() {
            queue.CompleteAdding();
        }

        public static void Run(Func<Task> func) {
            SynchronizationContext prevContext = Current;
            try {
                SingleThreadSynchronizationContext syncContext = new SingleThreadSynchronizationContext();
                SetSynchronizationContext(syncContext);

                Task t = func();
                syncContext.RunOnCurrentThread();

                t.GetAwaiter().GetResult();
            } finally {
                SetSynchronizationContext(prevContext);
            }
        }
    }
}
