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

## 用法

### 对象

```csharp
LCObject obj = new LCObject("Hello");
obj["intValue"] = 123;
await obj.Save();
```

### 查询

```csharp
LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
query.Limit(2);
List<LCObject> list = await query.Find();
```

### 文件

```csharp
LCFile file = new LCFile("avatar", AvatarFilePath);
await file.Save((count, total) => {
    TestContext.WriteLine($"progress: {count}/{total}");
});
```

### 用户

```csharp
await LCUser.Login("hello", "world");
```

### GeoPoint

```csharp
LCGeoPoint p1 = new LCGeoPoint(20.0059, 110.3665);
```

[API 文档](https://leancloud.github.io/csharp-sdk/html/index.html)
