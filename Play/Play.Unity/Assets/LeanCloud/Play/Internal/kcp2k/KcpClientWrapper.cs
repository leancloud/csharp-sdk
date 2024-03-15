using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LC.Newtonsoft.Json;

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
            LCLogger.Debug($"{Id} Kcp client init with config: {JsonConvert.SerializeObject(kcpConfig)}");
        }

        public Task Connect(string address, ushort port)
        {
            if (!shouldRunning)
            {
                throw new InvalidOperationException("Kcp client has closed, can't reconnect");
            }
            LCLogger.Debug($"{Id} Kcp client connecting with {address}:{port}");
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
            // byteBuffer = byteBuffer.Concat(bytes.ToArray()).ToArray();
            // while (byteBuffer.Length > 3)
            // {
            //     byte[] lengthBytes = new ArraySegment<byte>(byteBuffer, 0, 4).ToArray();
            //     if (BitConverter.IsLittleEndian) {
            //         Array.Reverse(lengthBytes);
            //     }
            //     int length = BitConverter.ToInt32(lengthBytes, 0);
            //     LCLogger.Debug($"{Id} Kcp client received data, data length: {length}, channel: {channel}");
            //     if (4 + length > byteBuffer.Length)
            //     {
            //         return;
            //     }
            //     byte[] message = new ArraySegment<byte>(byteBuffer, 4, length).ToArray();
            //     OnMessage?.Invoke(message, length);
            //     if (4 + length == byteBuffer.Length)
            //     {
            //         byteBuffer = Array.Empty<byte>();
            //     }
            //     else
            //     {
            //         byteBuffer = new ArraySegment<byte>(byteBuffer, 4 + length, byteBuffer.Length - (4 + length)).ToArray();
            //     }
            // }
            byte[] message = new ArraySegment<byte>(bytes.ToArray(), 4, bytes.Count - 4).ToArray();
            LCLogger.Debug($"{Id} Kcp client received data, data length: {message.Length}, channel: {channel}");
            OnMessage?.Invoke(message, message.Length);
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
            LCLogger.Debug($"{Id} Kcp client start run loop");
            while (shouldRunning)
            {
                bool isSendQueueEmpty = sendQueue.IsEmpty;
                while (sendQueue.Count > 0)
                {
                    if (sendQueue.TryDequeue(out SendTask task))
                    {
                        byte[] lengthBytes = BitConverter.GetBytes(task.Bytes.Count);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(lengthBytes);
                        }
                        byte[] bytes = lengthBytes.Concat(task.Bytes.ToArray()).ToArray();
                        kcpClient.Send(new ArraySegment<byte>(bytes), KcpChannel.Reliable);
                        LCLogger.Debug($"{Id} Kcp client send bytes {bytes.Length}");
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