using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// Command Runner.
    /// </summary>
    public class AVCommandRunner : IAVCommandRunner
    {
        private readonly IHttpClient httpClient;
        private readonly IInstallationIdController installationIdController;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="installationIdController"></param>
        public AVCommandRunner(IHttpClient httpClient, IInstallationIdController installationIdController)
        {
            this.httpClient = httpClient;
            this.installationIdController = installationIdController;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="uploadProgress"></param>
        /// <param name="downloadProgress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(AVCommand command,
            IProgress<AVUploadProgressEventArgs> uploadProgress = null,
            IProgress<AVDownloadProgressEventArgs> downloadProgress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return PrepareCommand(command).ContinueWith(commandTask =>
            {
                var requestLog = commandTask.Result.ToLog();
                AVClient.PrintLog("http=>" + requestLog);

                return httpClient.ExecuteAsync(commandTask.Result, uploadProgress, downloadProgress, cancellationToken).OnSuccess(t =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var response = t.Result;
                    var contentString = response.Item2;
                    int responseCode = (int)response.Item1;

                    var responseLog = responseCode + ";" + contentString;
                    AVClient.PrintLog("http<=" + responseLog);

                    if (responseCode >= 500)
                    {
                        // Server error, return InternalServerError.
                        throw new AVException(AVException.ErrorCode.InternalServerError, response.Item2);
                    }
                    else if (contentString != null)
                    {
                        IDictionary<string, object> contentJson = null;
                        try
                        {
                            if (contentString.StartsWith("["))
                            {
                                var arrayJson = Json.Parse(contentString);
                                contentJson = new Dictionary<string, object> { { "results", arrayJson } };
                            }
                            else
                            {
                                contentJson = Json.Parse(contentString) as IDictionary<string, object>;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new AVException(AVException.ErrorCode.OtherCause,
                                "Invalid response from server", e);
                        }
                        if (responseCode < 200 || responseCode > 299)
                        {
                            AVClient.PrintLog("error response code:" + responseCode);
                            int code = (int)(contentJson.ContainsKey("code") ? (int)contentJson["code"] : (int)AVException.ErrorCode.OtherCause);
                            string error = contentJson.ContainsKey("error") ?
                                contentJson["error"] as string : contentString;
                            AVException.ErrorCode ec = (AVException.ErrorCode)code;
                            throw new AVException(ec, error);
                        }
                        return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1,
                            contentJson);
                    }
                    return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, null);
                });
            }).Unwrap();
        }

        private const string revocableSessionTokenTrueValue = "1";
        private Task<AVCommand> PrepareCommand(AVCommand command)
        {
            AVCommand newCommand = new AVCommand(command);

            Task<AVCommand> installationIdTask = installationIdController.GetAsync().ContinueWith(t =>
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Installation-Id", t.Result.ToString()));
                return newCommand;
            });

            AVClient.Configuration configuration = AVClient.CurrentConfiguration;
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Id", configuration.ApplicationId));

            long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            if (!string.IsNullOrEmpty(configuration.MasterKey) && AVClient.UseMasterKey)
            {
                string sign = MD5.GetMd5String(timestamp + configuration.MasterKey);
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Sign", string.Format("{0},{1},master", sign, timestamp)));
            }
            else
            {
                string sign = MD5.GetMd5String(timestamp + configuration.ApplicationKey);
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Sign", string.Format("{0},{1}", sign, timestamp)));
            }

            newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Client-Version", AVClient.VersionString));

            if (!string.IsNullOrEmpty(configuration.VersionInfo.BuildVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-App-Build-Version", configuration.VersionInfo.BuildVersion));
            }
            if (!string.IsNullOrEmpty(configuration.VersionInfo.DisplayVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-App-Display-Version", configuration.VersionInfo.DisplayVersion));
            }
            if (!string.IsNullOrEmpty(configuration.VersionInfo.OSVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-OS-Version", configuration.VersionInfo.OSVersion));
            }

            if (AVUser.IsRevocableSessionEnabled)
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LeanCloud-Revocable-Session", revocableSessionTokenTrueValue));
            }

            if (configuration.AdditionalHTTPHeaders != null)
            {
                var headersDictionary = newCommand.Headers.ToDictionary(kv => kv.Key, kv => kv.Value);
                foreach (var header in configuration.AdditionalHTTPHeaders)
                {
                    if (headersDictionary.ContainsKey(header.Key))
                    {
                        headersDictionary[header.Key] = header.Value;
                    }
                    else
                    {
                        newCommand.Headers.Add(header);
                    }
                }
                newCommand.Headers = headersDictionary.ToList();
            }

            return installationIdTask;
        }
    }
}
