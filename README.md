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

## 文档

[API 文档](https://leancloud.github.io/csharp-sdk/html/index.html)
