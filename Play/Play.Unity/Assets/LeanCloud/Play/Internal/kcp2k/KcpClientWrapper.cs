using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace LeanCloud.Play.kcp2k
{
    public class KcpClientWrapper
    {
        private class SendTask
        {
            internal TaskCompletionSource<object> Tcs { get; set; }
            internal ArraySegment<byte> Bytes { get; set; }
        }

        private readonly string Id;
        private readonly KcpClient kcpClient;
        private readonly KcpConfig kcpConfig;
        private readonly ConcurrentQueue<SendTask> sendQueue = new ConcurrentQueue<SendTask>();

        private TaskCompletionSource<object> connectTcs;
        private volatile bool shouldRunning = true;
        private byte[] byteBuffer = { };

        public Action<byte[], int> OnMessage;
        public Action OnClose;

        public KcpClientWrapper()
        {
            Id = Guid.NewGuid().ToString();
            kcpConfig = new KcpConfig();
            kcpClient = new KcpClient(
                () => { OnConnected(); },
                (message, channel) => { OnData(message, channel); },
                () => { OnDisconnected(); },
                (error, reason) => { OnError(error, reason); },
                kcpConfig
            );
            LCLogger.Debug($"{Id} Kcp client init with config: {kcpConfig}");
        }

        public Task Connect(string address, ushort port)
        {
            if (!shouldRunning)
            {
                throw new InvalidOperationException("Kcp client has closed, can't reconnect");
            }
            LCLogger.Debug($"{Id} Kcp client connecting ...");
            connectTcs = new TaskCompletionSource<object>();
            kcpClient.Connect(address, port);
            _ = StartRunLoop();
            return connectTcs.Task;
        }

        private void OnConnected()
        {
            LCLogger.Debug($"{Id} Kcp client connected");
            connectTcs.TrySetResult(null);
        }

        private void OnData(ArraySegment<byte> bytes, KcpChannel channel)
        {
            LCLogger.Debug($"{Id} Kcp client received data, bytes count: {bytes.Count}, channel: {channel}");
            byteBuffer = byteBuffer.Concat(bytes.ToArray()).ToArray();
            while (byteBuffer.Length > 3)
            {
                int length = BitConverter.ToInt32(byteBuffer, 0);
                if (4 + length > byteBuffer.Length)
                {
                    return;
                }
                byte[] message = new ArraySegment<byte>(byteBuffer, 4, length).ToArray();
                OnMessage?.Invoke(message, length);
                if (4 + length == byteBuffer.Length)
                {
                    byteBuffer = Array.Empty<byte>();
                }
                else
                {
                    byteBuffer = new ArraySegment<byte>(byteBuffer, 4 + length, byteBuffer.Length - (4 + length)).ToArray();
                }
            }
        }

        private void OnDisconnected()
        {
            LCLogger.Debug($"{Id} Kcp client disconnected");
            shouldRunning = false;
            OnClose?.Invoke();
        }

        private void OnError(ErrorCode code, string reason)
        {
            LCLogger.Error($"{Id} Kcp client encountered error: {code}, reason: {reason}");
            shouldRunning = false;
            OnClose?.Invoke();
        }

        public Task Send(byte[] data)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            sendQueue.Enqueue(new SendTask
            {
                Tcs = tcs,
                Bytes = new ArraySegment<byte>(data)
            });
            return tcs.Task;
        }

        private async Task StartRunLoop()
        {
            while (shouldRunning)
            {
                bool isSendQueueEmpty = sendQueue.IsEmpty;
                while (sendQueue.Count > 0)
                {
                    if (sendQueue.TryDequeue(out SendTask task))
                    {
                        kcpClient.Send(task.Bytes, KcpChannel.Reliable);
                        task.Tcs.TrySetResult(null);
                    }
                }
                kcpClient.Tick();
                if (isSendQueueEmpty)
                {
                    await Task.Delay((int)kcpConfig.Interval);
                }
            }
        }

        public void Stop()
        {
            shouldRunning = false;
        }
    }
}