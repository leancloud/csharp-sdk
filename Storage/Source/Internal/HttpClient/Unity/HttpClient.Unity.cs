using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LeanCloud.Storage.Internal
{
    public class HttpClient : IHttpClient
    {
        private static bool isCompiledByIL2CPP = System.AppDomain.CurrentDomain.FriendlyName.Equals("IL2CPP Root Domain");

        public Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
            IProgress<AVUploadProgressEventArgs> uploadProgress,
            IProgress<AVDownloadProgressEventArgs> downloadProgress,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Tuple<HttpStatusCode, string>>();
            cancellationToken.Register(() => tcs.TrySetCanceled());
            uploadProgress = uploadProgress ?? new Progress<AVUploadProgressEventArgs>();
            downloadProgress = downloadProgress ?? new Progress<AVDownloadProgressEventArgs>();

            Task readBytesTask = null;
            IDisposable toDisposeAfterReading = null;
            byte[] bytes = null;
            if (httpRequest.Data != null)
            {
                var ms = new MemoryStream();
                toDisposeAfterReading = ms;
                readBytesTask = httpRequest.Data.CopyToAsync(ms).OnSuccess(_ =>
                {
                    bytes = ms.ToArray();
                });
            }

            readBytesTask.Safe().ContinueWith(t =>
            {
                if (toDisposeAfterReading != null)
                {
                    toDisposeAfterReading.Dispose();
                }
                return t;
            }).Unwrap().OnSuccess(_ =>
            {
                float oldDownloadProgress = 0;
                float oldUploadProgress = 0;

                Dispatcher.Instance.Post(() =>
                {
                    WaitForWebRequest(GenerateRequest(httpRequest, bytes), request =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled();
                            return;
                        }
                        if (request.isDone)
                        {
                            uploadProgress.Report(new AVUploadProgressEventArgs { Progress = 1 });
                            downloadProgress.Report(new AVDownloadProgressEventArgs { Progress = 1 });

                            var statusCode = GetResponseStatusCode(request);
                            // Returns HTTP error if that's the only info we have.
                            // if (!String.IsNullOrEmpty(www.error) && String.IsNullOrEmpty(www.text))
                            if (!String.IsNullOrEmpty(request.error) && String.IsNullOrEmpty(request.downloadHandler.text))
                            {
                                var errorString = string.Format("{{\"error\":\"{0}\"}}", request.error);
                                tcs.TrySetResult(new Tuple<HttpStatusCode, string>(statusCode, errorString));
                            }
                            else
                            {
                                tcs.TrySetResult(new Tuple<HttpStatusCode, string>(statusCode, request.downloadHandler.text));
                            }
                        }
                        else
                        {
                            // Update upload progress
                            var newUploadProgress = request.uploadProgress;
                            if (oldUploadProgress < newUploadProgress)
                            {
                                uploadProgress.Report(new AVUploadProgressEventArgs { Progress = newUploadProgress });
                            }
                            oldUploadProgress = newUploadProgress;

                            // Update download progress
                            var newDownloadProgress = request.downloadProgress;
                            if (oldDownloadProgress < newDownloadProgress)
                            {
                                downloadProgress.Report(new AVDownloadProgressEventArgs { Progress = newDownloadProgress });
                            }
                            oldDownloadProgress = newDownloadProgress;
                        }
                    });
                });
            });

            // Get off of the main thread for further processing.
            return tcs.Task.ContinueWith(t =>
            {
                var dispatchTcs = new TaskCompletionSource<object>();
                // ThreadPool doesn't work well in IL2CPP environment, but Thread does!
                if (isCompiledByIL2CPP)
                {
                    var thread = new Thread(_ =>
                    {
                        dispatchTcs.TrySetResult(null);
                    });
                    thread.Start();
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(_ => dispatchTcs.TrySetResult(null));
                }
                return dispatchTcs.Task;
            }).Unwrap()
            .ContinueWith(_ => tcs.Task).Unwrap();
        }

        private static HttpStatusCode GetResponseStatusCode(UnityWebRequest request)
        {
            if (Enum.IsDefined(typeof(HttpStatusCode), (int)request.responseCode))
            {
                return (HttpStatusCode)request.responseCode;
            }
            return (HttpStatusCode)400;
        }

        private static UnityWebRequest GenerateRequest(HttpRequest request, byte[] bytes)
        {
            var webRequest = new UnityWebRequest();
            webRequest.method = request.Method;
            webRequest.url = request.Uri.AbsoluteUri;
            // Explicitly assume a JSON content.
            webRequest.SetRequestHeader("Content-Type", "application/json");
            //webRequest.SetRequestHeader("User-Agent", "net-unity-" + AVVersionInfo.Version);
            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    webRequest.SetRequestHeader(header.Key as string, header.Value as string);
                }
            }

            if (bytes != null)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bytes);
            }
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.Send();
            return webRequest;
        }

        private static void WaitForWebRequest(UnityWebRequest request, Action<UnityWebRequest> action)
        {
            Dispatcher.Instance.Post(() =>
            {
                var isDone = request.isDone;
                action(request);
                if (!isDone)
                {
                    WaitForWebRequest(request, action);
                }
            });
        }
    }
}
