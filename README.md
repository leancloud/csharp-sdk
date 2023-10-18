# csharp-sdk

![build](https://img.shields.io/github/workflow/status/leancloud/csharp-sdk/.NET)
![version](https://img.shields.io/github/v/release/leancloud/csharp-sdk?include_prereleases)

LeanCloud C# SDK，基于 .Net Standard 2.0 标准开发，包括服务如下：

- 存储
- 排行榜
- 即时通讯
- Live Query
- 云引擎
- 多人对战

参考：[Unity Demo](https://github.com/leancloud/CSharp-SDK-Unity-Demo)

## 安装

### 直接导入

从 [Release](https://github.com/leancloud/csharp-sdk/releases) 下载指定版本 SDK，暂不支持 Nuget 方式。

### UPM

在 Unity 项目的 Packages/manifest.json 中添加依赖项

```json
"dependencies": {
  "com.leancloud.storage": "https://github.com/leancloud/csharp-sdk-upm.git#storage-2.2.2",
  "com.leancloud.realtime": "https://github.com/leancloud/csharp-sdk-upm.git#realtime-2.2.2",
  "com.leancloud.play": "https://github.com/leancloud/csharp-sdk-upm.git#play-2.2.2"
}
```

## 编译

从 [Repo](https://github.com/leancloud/csharp-sdk) clone 仓库，使用 Visual Studio 打开 csharp-sdk.sln 编译。
Unity 用户在编译完成后，请将 XX-Unity 工程中 Debug/Release 的 dlls 拷贝至 Unity 工程下的 Plugins 目录中即可使用。
其他 .Net 平台用户使用 XX 工程即可。
（XX 指 Storage，Realtime，LiveQuery 等）

## 项目结构

由于 Unity 平台并不是标准的 .Net Standard 2.0，所以在每个服务下单独拆分出了 XX-Unity 工程，源码和主工程是一致的，只是在依赖库方面有些区别。后面也可能针对 Unity 平台做些相关支持。

```
├── csharp-sdk.sln              // 项目配置
├── Common                      // 公共库，包含基础功能
├── Storage                     // 存储服务
│   ├── Storage                 // .Net Standard 2.0 工程
│   ├── Storage-Unity           // Unity 工程
│   └── Storage.Test            // 单元测试
├── Realtime                    // 即时通讯服务
│   ├── Realtime                // .Net Standard 2.0 工程
│   ├── Realtime-Unity          // Unity 工程
│   └── Realtime.Test           // 单元测试
├── LiveQuery                   // LiveQuery 服务
│   ├── LiveQuery               // .Net Standard 2.0 工程
│   ├── LiveQuery-Unity         // Unity 工程
│   └── LiveQuery.Test          // 单元测试
├── Sample                      // 示例
│   ├── RealtimeApp             // 即时通讯应用，主要测试断线重连
│   └── LiveQueryApp            // LiveQuery 应用，主要测试断线重连
└── UnityLibs                   // Unity 依赖
    └── Newtonsoft.Json.dll     // Json 库，由于 Unity iOS AOT 的原因，不能使用 .Net Standard 2.0 版本
```

## 导入

```csharp
using LeanCloud;
// 数据存储
using LeanCloud.Storage;
// 即时通讯
using LeanCloud.Realtime;
// 多人对战
using Leancloud.Play;
```

## 初始化

```csharp
LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
```

## 调试

开启调试日志

```csharp
LCLogger.LogDelegate += (level, info) => {
    switch (level) {
        case LCLogLevel.Debug:
            WriteLine($"[DEBUG] {DateTime.Now} {info}\n");
            break;
        case LCLogLevel.Warn:
            WriteLine($"[WARNING] {DateTime.Now} {info}\n");
            break;
        case LCLogLevel.Error:
            WriteLine($"[ERROR] {DateTime.Now} {info}\n");
            break;
        default:
            WriteLine(info);
            break;
    }
}
```

## 数据存储

### 对象

```csharp
LCObject obj = new LCObject("Hello");
obj["intValue"] = 123;
await obj.Save();
```

更多关于**对象**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Storage/Storage.Test/ObjectTest.cs)

### 查询

```csharp
LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
query.Limit(2);
List<LCObject> list = await query.Find();
```

更多关于**查询**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Storage/Storage.Test/QueryTest.cs)

### 文件

```csharp
LCFile file = new LCFile("avatar", AvatarFilePath);
await file.Save((count, total) => {
    TestContext.WriteLine($"progress: {count}/{total}");
});
```

更多关于**文件**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Storage/Storage.Test/FileTest.cs)

### 用户

```csharp
await LCUser.Login("hello", "world");
```

更多关于**用户**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Storage/Storage.Test/UserTest.cs)

### GeoPoint

```csharp
LCGeoPoint p1 = new LCGeoPoint(20.0059, 110.3665);
```

更多关于 **GeoPoint** 用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Storage/Storage.Test/GeoTest.cs)

## 即时通讯

### 用户

```csharp
LCIMClient client = new LCIMClient("c1");
// 登录
await client.Open();
// 注销
await client.Close();
```

更多关于**用户**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Realtime/Realtime.Test/Client.cs)

### 对话

```csharp
// 创建普通对话
LCIMConversation conversation = await client.CreateConversation(new string[] { "world" }, name: name, unique: false);
// 创建聊天室
LCIMConversation chatroom = await client.CreateChatRoom(name);
// 创建临时对话
LCIMConversation tempConversation = await client.CreateTemporaryConversation(new string[] { "world" });
```

更多关于**对话**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Realtime/Realtime.Test/Conversation.cs)

### 消息

```csharp
// 发送消息
LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
await conversation.Send(textMessage);
// 接收消息
m2.OnMessage = (conv, msg) => {
    if (msg is LCIMTextMessage textMsg) {
        WriteLine($"text: {textMsg.Text}");
    }
};
```

更多关于**对话**用法：[参考](https://github.com/leancloud/csharp-sdk/blob/master/Realtime/Realtime.Test/Message.cs)

## 排行榜

### 创建排行榜

```csharp
LCLeaderboard leaderboard = await LCLeaderboard.CreateLeaderboard(leaderboardName);
```

### 更新成绩

```csharp
await LCLeaderboard.UpdateStatistics(user, new Dictionary<string, double> {
    { leaderboardName, 100 }
});
```

### 获取成绩

```csharp
LCUser user = await LCUser.Login(username, password);
ReadOnlyCollection<LCStatistic> statistics = await LCLeaderboard.GetStatistics(user);
foreach (LCStatistic statistic in statistics) {
    WriteLine($"{statistic.Name} : {statistic.Value}");
}
```

### 获取我附近的成绩

```csharp
await LCUser.Login(username, password);
LCLeaderboard leaderboard = LCLeaderboard.CreateWithoutData(leaderboardName);
ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResultsAroundUser(limit: 5);
foreach (LCRanking ranking in rankings) {
    WriteLine($"{ranking.Rank} : {ranking.User.ObjectId}, {ranking.Value}");
}
```

### 获取榜单

```csharp
LCLeaderboard leaderboard = LCLeaderboard.CreateWithoutData(leaderboardName);
ReadOnlyCollection<LCRanking> rankings = await leaderboard.GetResults();
foreach (LCRanking ranking in rankings) {
    WriteLine($"{ranking.Rank} : {ranking.User.ObjectId}, {ranking.Value}");
}
```

## LiveQuery

### 订阅

```csharp
LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
query.WhereGreaterThan("balance", 100);
liveQuery = await query.Subscribe();
```

### 事件

```csharp
// 新建符合条件对象的事件
liveQuery.OnCreate = (obj) => {
    WriteLine($"create: {obj}");
};

// 符合条件对象更新事件
liveQuery.OnUpdate = (obj, updatedKeys) => {
    WriteLine($"update: {obj}");
};

// 符合条件对象被删除事件
liveQuery.OnDelete = (objId) => {
    WriteLine($"delete: {objId}");
};

// 有新的符合条件的对象事件
liveQuery.OnEnter = (obj, updatedKeys) => {
    WriteLine($"enter: {obj}");
};

// 有符合条件的对象不再满足条件事件
liveQuery.OnLeave = (obj, updatedKeys) => {
    WriteLine($"level: {obj}");
};
```

### 特殊事件

当一个用户成功登录应用，OnLogin 事件会被触发。下面的 user 就是登录的 LCUser:

```csharp
await LCUser.Login("hello", "world");
LCQuery<LCUser> userQuery = LCUser.GetQuery();
userQuery.WhereEqualTo("username", "hello");
LCLiveQuery userLiveQuery = await userQuery.Subscribe();
userLiveQuery.OnLogin = (user) => {
    WriteLine($"login: {user}");
};
```

## 云引擎

[脚手架工程](https://github.com/leancloud/dotnet-core-getting-started)

## 文档

[API 文档](https://leancloud.github.io/csharp-sdk/html/index.html)
