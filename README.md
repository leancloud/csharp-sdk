# csharp-sdk

LeanCloud 数据存储，即时通讯 C# SDK，基于 .Net Standard 2.0 标准开发。

## 安装

从 [Release](https://github.com/leancloud/csharp-sdk/releases) 下载指定版本 SDK，暂不支持 Nuget 方式。

## 导入

```csharp
using LeanCloud;
// 数据存储
using LeanCloud.Storage;
// 即时通讯
using LeanCloud.Realtime;
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

## 文档

[API 文档](https://leancloud.github.io/csharp-sdk/html/index.html)
