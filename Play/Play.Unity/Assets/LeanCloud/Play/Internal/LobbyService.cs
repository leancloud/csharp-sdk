using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using LC.Newtonsoft.Json;
using LeanCloud.Common;

namespace LeanCloud.Play {
    /// <summary>
    /// 创建 / 加入房间结果
    /// </summary>
    internal class LobbyRoomResult {
        [JsonProperty("cid")]
        internal string RoomId {
            get; set;
        }

        [JsonProperty("addr")]
        internal string Url {
            get { return "106.75.21.173:8007"; } 
            set {}
        }

        [JsonProperty("roomCreated")]
        internal bool Create {
            get; set;
        }
    }

    internal class LobbyService {
        readonly Client client;

        GameRouter gameRouter;

        internal LobbyService(Client client) {
            this.client = client;
            gameRouter = new GameRouter(client);
        }

        internal async Task<LobbyInfo> Authorize() {
            return await gameRouter.Authorize();
        }

        internal async Task<LobbyRoomResult> CreateRoom(string roomName) {
            string path = "1/multiplayer/lobby/room";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "useInsecureAddr", !client.Ssl }
            };
            if (!string.IsNullOrEmpty(roomName)) {
                body.Add("cid", roomName);
            }
            return await Request<LobbyRoomResult>(path, HttpMethod.Post, data: body);
        }

        internal async Task<LobbyRoomResult> JoinRoom(string roomName, List<string> expectedUserIds, bool rejoin, bool createOnNotFound) {
            string path = $"1/multiplayer/lobby/room/{roomName}";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "cid", roomName },
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "useInsecureAddr", !client.Ssl }
            };
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            if (rejoin) {
                body.Add("rejoin", rejoin);
            }
            if (createOnNotFound) {
                body.Add("createOnNotFound", createOnNotFound);
            }
            return await Request<LobbyRoomResult>(path, HttpMethod.Post, data: body);
        }

        internal async Task<LobbyRoomResult> JoinRandomRoom(PlayObject matchProperties, List<string> expectedUserIds) {
            string path = $"1/multiplayer/lobby/match/room";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "useInsecureAddr", !client.Ssl }
            };
            if (matchProperties != null) {
                body.Add("expectAttr", matchProperties.Data);
            }
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            return await Request<LobbyRoomResult>(path, HttpMethod.Post, data: body);
        }

        internal async Task<LobbyRoomResult> MatchRandom(string piggybackUserId, PlayObject matchProperties, List<string> expectedUserIds) {
            string path = "1/multiplayer/lobby/match/room";
            Dictionary<string, object> body = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion },
                { "piggybackPeerId", piggybackUserId }
            };
            if (matchProperties != null) {
                body.Add("expectAttr", matchProperties.Data);
            }
            if (expectedUserIds != null) {
                body.Add("expectMembers", expectedUserIds);
            }
            return await Request<LobbyRoomResult>(path, HttpMethod.Post, data: body);
        }

        internal async Task<LobbyRoomResult> FetchMyRoom() {
            string path = "1/multiplayer/lobby/peer/self/room";
            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { "gameVersion", client.GameVersion },
                { "sdkVersion", Config.SDKVersion },
                { "protocolVersion", Config.ProtocolVersion }
            };
            return await Request<LobbyRoomResult>(path, HttpMethod.Get, queryParams: queryParams);
        }

        async Task<T> Request<T>(string path,
            HttpMethod method,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> queryParams = null,
            object data = null) {
            HttpClient httpClient = await NewHttpClient();

            string url = await BuildUrl(path, queryParams);
            HttpRequestMessage request = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = method,
            };
            if (headers != null) {
                foreach (KeyValuePair<string, object> kv in headers) {
                    request.Headers.Add(kv.Key, kv.Value.ToString());
                }
            }

            string content = null;
            if (data != null) {
                content = JsonConvert.SerializeObject(data);
                StringContent requestContent = new StringContent(content);
                requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = requestContent;
            }
            LCHttpUtils.PrintRequest(httpClient, request, content);
            HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();

            string resultString = await response.Content.ReadAsStringAsync();
            response.Dispose();
            LCHttpUtils.PrintResponse(response, resultString);

            if (response.IsSuccessStatusCode) {
                T ret = JsonConvert.DeserializeObject<T>(resultString,
                    LCJsonConverter.Default);
                return ret;
            }
            throw HandleErrorResponse(response.StatusCode, resultString);
        }

        PlayException HandleErrorResponse(HttpStatusCode statusCode, string responseContent) {
            int code = (int)statusCode;
            string message = responseContent;
            try {
                // 尝试获取 LeanCloud 返回错误信息
                PlayException error = JsonConvert.DeserializeObject<PlayException>(responseContent,
                    LCJsonConverter.Default);
                return error;
            } catch (Exception e) {
                LCLogger.Error(e);
            }
            return new PlayException(code, message);
        }

        async Task<HttpClient> NewHttpClient() {
            HttpClient httpClient = new HttpClient();

            LobbyInfo lobbyInfo = await gameRouter.Authorize();

            httpClient.DefaultRequestHeaders.Add("X-LC-ID", client.AppId);
            httpClient.DefaultRequestHeaders.Add("X-LC-KEY", client.AppKey);
            httpClient.DefaultRequestHeaders.Add("X-LC-PLAY-USER-ID", client.UserId);
            httpClient.DefaultRequestHeaders.Add("X-LC-PLAY-MULTIPLAYER-SESSION-TOKEN", lobbyInfo.SessionToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        async Task<string> BuildUrl(string path, Dictionary<string, object> queryParams) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            StringBuilder urlSB = new StringBuilder(lobbyInfo.Url.TrimEnd('/'));
            urlSB.Append($"/{path}");
            if (queryParams != null) {
                IEnumerable<string> queryPairs = queryParams.Select(kv => $"{kv.Key}={kv.Value}");
                string queries = string.Join("&", queryPairs);
                urlSB.Append("?");
                urlSB.Append(queries);
            }
            return urlSB.ToString();
        }

        async Task AddHeaders(HttpContentHeaders headers) {
            LobbyInfo lobbyInfo = await gameRouter.Authorize();
            headers.Add("X-LC-ID", client.AppId);
            headers.Add("X-LC-KEY", client.AppKey);
            headers.Add("X-LC-PLAY-USER-ID", client.UserId);
            headers.Add("X-LC-PLAY-MULTIPLAYER-SESSION-TOKEN", lobbyInfo.SessionToken);
            headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
    }
}
