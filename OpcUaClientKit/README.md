# OpcUaClientKit

`OpcUaClientKit` 是一个轻量、通用、可复用的 OPC UA 客户端封装，基于 `OPCFoundation.NetStandard.Opc.Ua.Client`，适合在 Console、WinForms、WPF、ASP.NET Core、Worker Service 等项目中直接使用。

它的目标不是做一个很重的企业级框架，而是把 OPC UA 客户端最常用的能力整理成一套简单、清晰、可扩展的 API。

## 功能概览

- 工厂模式创建客户端
- 简单配置和复杂配置两种使用方式
- 自动完成客户端配置、证书检查/生成、Endpoint 选择、用户身份、Session 创建
- 单节点和批量读写
- 方法调用
- 数据订阅
- 报警事件订阅
- 支持原始 `nodeId` 和封装后的 `OpcUaNode`

## 目标框架

当前类库使用 `netstandard2.1` 构建。

这意味着它适合用于支持 `netstandard2.1` 的现代 .NET 项目，例如：

- `.NET Core 3.1`
- `.NET 5/6/7/8/...`

如果后续需要兼容较老的 `.NET Framework` 项目，需要再评估是否下探到 `netstandard2.0`。

## 安装

如果你是本地项目引用：

```xml
<ProjectReference Include="..\OpcUaClientKit\OpcUaClientKit.csproj" />
```

如果你已经打成 NuGet 包：

```bash
dotnet add package OpcUaClientKit
```

## 核心类型

- `OpcUaClientFactory`
- `IOpcUaClient`
- `OpcUaClientOptions`
- `OpcUaClientBuilder`
- `OpcUaNode`
- `ISubscribableOpcUaClient`
- `IEventSubscribableOpcUaClient`

## 快速开始

### 1. 最简单的连接方式

```csharp
using OpcUaClientKit;

IOpcUaClientFactory factory = new OpcUaClientFactory();

await using var client = factory.Create(
    serverUrl: "opc.tcp://127.0.0.1:4840",
    applicationName: "MyOpcUaApp",
    autoAcceptUntrustedServerCertificate: true,
    useSecurity: true,
    sessionTimeout: 60000);

await client.ConnectAsync();

var value = await client.ReadNodeAsync("ns=3;s=/Plc/DB66.DBW0");
Console.WriteLine(value);

await client.DisconnectAsync();
```

### 2. 用户名密码连接

```csharp
using OpcUaClientKit;

IOpcUaClientFactory factory = new OpcUaClientFactory();

await using var client = factory.Create(
    serverUrl: "opc.tcp://127.0.0.1:4840",
    applicationName: "MyOpcUaApp",
    deviceId: "device-a",
    userName: "OpcUaClient",
    password: "123456",
    autoAcceptUntrustedServerCertificate: true,
    useSecurity: true,
    sessionTimeout: 60000);

await client.ConnectAsync();
```

## 简单配置与复杂配置

### 简单配置

简单配置适合大多数项目，工厂直接暴露最常用参数：

- `serverUrl`
- `applicationName`
- `deviceId`
- `userName/password`
- `autoAcceptUntrustedServerCertificate`
- `useSecurity`
- `sessionTimeout`

当提供 `deviceId` 时，默认会按下面的目录组织证书：

```text
%LocalApplicationData%\OPC Foundation\<applicationName>\<deviceId>\pki
```

### 复杂配置

复杂配置适合你需要更细粒度控制时使用：

```csharp
using OpcUaClientKit;

IOpcUaClientFactory factory = new OpcUaClientFactory();

var client = factory
    .CreateBuilder()
    .WithServerUrl("opc.tcp://127.0.0.1:4840")
    .WithApplicationName("MyOpcUaApp")
    .WithDeviceId("device-b")
    .WithUserNamePassword("OpcUaClient", "123456")
    .WithSecurity(true)
    .WithSessionTimeout(60000)
    .WithOperationTimeout(30000)
    .WithCheckDomain(false)
    .WithAutoAcceptUntrustedServerCertificate(true)
    .Build();

await using var _ = client;
await client.ConnectAsync();
```

如果你喜欢直接传完整配置对象，也可以使用 `OpcUaClientOptions`：

```csharp
using OpcUaClientKit;

var options = new OpcUaClientOptions
{
    ServerUrl = "opc.tcp://127.0.0.1:4840",
    ApplicationName = "MyOpcUaApp",
    DeviceId = "device-c",
    UseSecurity = true,
    SessionTimeout = 60000,
    UserName = "OpcUaClient",
    Password = "123456",
    Certificate =
    {
        AutoAcceptUntrustedServerCertificate = true
    }
};

IOpcUaClientFactory factory = new OpcUaClientFactory();
await using var client = factory.Create(options);
await client.ConnectAsync();
```

## 节点封装

如果你希望在读、写、订阅时带上业务显示名，可以使用 `OpcUaNode`：

```csharp
using OpcUaClientKit;

var levelNode = new OpcUaNode("ns=6;s=MyLevel", "液位");
var value = await client.ReadNodeAsync(levelNode);
```

`OpcUaNode` 不会替代原始 `nodeId`，只是一个更易用的轻量封装。库里大部分 API 同时支持这两种写法。

## 读写教程

### 单节点读取

```csharp
var value = await client.ReadNodeAsync("ns=6;s=MyLevel");
var typedValue = await client.ReadNodeAsync<double>("ns=6;s=MyLevel");
```

### 使用 `OpcUaNode` 读取

```csharp
var levelNode = new OpcUaNode("ns=6;s=MyLevel", "液位");
var value = await client.ReadNodeAsync(levelNode);
```

### 批量读取

```csharp
var values = await client.ReadNodesAsync(new[]
{
    "ns=3;s=/Plc/DB66.DBW0",
    "ns=3;s=/Plc/DB66.DBW2"
});
```

### 单节点写入

```csharp
await client.WriteNodeAsync("ns=3;s=/Plc/DB66.DBW0", (short)1);
```

### 批量写入

```csharp
await client.WriteNodesAsync(new Dictionary<string, object?>
{
    ["ns=3;s=/Plc/DB66.DBW2"] = (short)3,
    ["ns=3;s=/Plc/DB66.DBW0"] = (short)1
});
```

也可以直接使用 `OpcUaNode`：

```csharp
var openNode = new OpcUaNode("ns=3;s=/Plc/DB66.DBW0", "Open");
var speedNode = new OpcUaNode("ns=3;s=/Plc/DB66.DBW2", "Speed");

await client.WriteNodesAsync(new Dictionary<OpcUaNode, object?>
{
    [openNode] = (short)1,
    [speedNode] = (short)3
});
```

## 方法调用教程

### 使用原始 `nodeId`

```csharp
var outputs = await client.CallMethodAsync(
    objectNodeId: "ns=3;s=/Objects/MyDevice",
    methodNodeId: "ns=3;s=/Objects/MyDevice/Start",
    inputArguments: new object?[] { (short)1, true });
```

### 使用 `OpcUaNode`

```csharp
var objectNode = new OpcUaNode("ns=3;s=/Objects/MyDevice", "MyDevice");
var methodNode = new OpcUaNode("ns=3;s=/Objects/MyDevice/Start", "Start");

var outputs = await client.CallMethodAsync(
    objectNode,
    methodNode,
    new object?[] { (short)1, true });
```

## 数据订阅教程

### 快速订阅单节点

```csharp
using OpcUaClientKit;

var subscribable = client.AsSubscribable();

await using var subscription = await subscribable.SubscribeNodeAsync(
    "ns=6;s=MyLevel",
    notification =>
    {
        Console.WriteLine(
            $"{notification.NodeId} = {notification.Value}, Good={notification.IsGood}");
    });
```

### 创建订阅组，再动态添加节点

```csharp
using OpcUaClientKit;

var subscribable = client.AsSubscribable();

await using var subscription = await subscribable
    .CreateSubscriptionBuilder()
    .WithName("plc-values")
    .WithPublishingInterval(250)
    .BuildAsync();

await subscription.AddNodeAsync(
    new OpcUaNode("ns=3;s=/Plc/DB66.DBW2", "速度"),
    notification =>
    {
        Console.WriteLine($"{notification.DisplayName} = {notification.Value}");
    });

await subscription.AddNodesAsync(new[]
{
    new OpcUaSubscriptionNodeDefinition(
        new OpcUaNode("ns=3;s=/Plc/DB66.DBW0", "启停"),
        notification => Console.WriteLine($"{notification.DisplayName} = {notification.Value}")),
    new OpcUaSubscriptionNodeDefinition(
        new OpcUaNode("ns=6;s=MyLevel", "液位"),
        notification => Console.WriteLine($"{notification.DisplayName} = {notification.Value}"))
});
```

### 删除订阅节点

```csharp
await subscription.RemoveNodeAsync("ns=3;s=/Plc/DB66.DBW0");
```

## 报警事件订阅教程

### 快速订阅报警事件

```csharp
using OpcUaClientKit;

var eventClient = client.AsEventSubscribable();

await using var subscription = await eventClient
    .CreateEventSubscriptionBuilder()
    .WithName("alarm-events")
    .WithPublishingInterval(500)
    .BuildAsync(notification =>
    {
        Console.WriteLine(
            $"[{notification.Time:HH:mm:ss}] {notification.SourceName} | " +
            $"Message={notification.Message} | Severity={notification.Severity}");
    });

await subscription.AddSourceAsync(new OpcUaNode("ns=6;s=MyObjectsFolder", "AlarmRoot"));
```

### 常用事件订阅选项

```csharp
using OpcUaClientKit;

var eventClient = client.AsEventSubscribable();

await using var subscription = await eventClient
    .CreateEventSubscriptionBuilder()
    .WithName("alarm-events")
    .WithPublishingInterval(500)
    .WithConditionRefreshOnStart(true)
    .WithIgnoreSuppressedOrShelved(true)
    .WithSelectClauseMode(OpcUaEventSelectClauseMode.Dynamic)
    .BuildAsync(notification =>
    {
        Console.WriteLine(notification.Message);

        foreach (var field in notification.SelectedFields)
        {
            Console.WriteLine($"{field.DisplayName}: {field.Value}");
        }
    });
```

### 事件订阅说明

- 默认事件类型是 `AlarmConditionType`
- 默认 `SelectClauses` 模式是 `Dynamic`
- 默认会在添加事件源后执行 `ConditionRefresh`
- `Refresh Start` / `Refresh End` 边界事件已经在库内过滤，不会再回调到业务层
- `SelectedFields` 按实际 `SelectClauses` 顺序返回，适合做调试和通用事件表展示
- 强类型字段适合业务层直接消费

## 典型使用流程

一个比较完整的使用过程通常是：

1. 创建 `IOpcUaClient`
2. `ConnectAsync()`
3. 执行读、写、方法调用
4. 建立数据订阅或事件订阅
5. 退出前 `DisconnectAsync()` 或直接 `DisposeAsync()`

例如：

```csharp
using OpcUaClientKit;

IOpcUaClientFactory factory = new OpcUaClientFactory();

await using var client = factory.Create(
    "opc.tcp://127.0.0.1:4840",
    "MyOpcUaApp",
    "device-a",
    "OpcUaClient",
    "123456",
    autoAcceptUntrustedServerCertificate: true);

await client.ConnectAsync();

var currentLevel = await client.ReadNodeAsync<double>("ns=6;s=MyLevel");
await client.WriteNodeAsync("ns=3;s=/Plc/DB66.DBW0", (short)1);

var outputs = await client.CallMethodAsync(
    "ns=3;s=/Objects/MyDevice",
    "ns=3;s=/Objects/MyDevice/Reset");

var subscribable = client.AsSubscribable();
await using var dataSubscription = await subscribable
    .CreateSubscriptionBuilder()
    .WithName("runtime-data")
    .WithPublishingInterval(500)
    .BuildAsync();

await dataSubscription.AddNodeAsync(
    new OpcUaNode("ns=6;s=MyLevel", "液位"),
    notification => Console.WriteLine($"{notification.DisplayName} = {notification.Value}"));

Console.ReadLine();
```

## Demo 项目

当前解决方案里提供了几个示例项目：

- `OpcUaClientKit.Demo`
- `OpcUaClientKit.EventDemo`
- `MyDemo`

如果你想快速看代码用法，建议优先从这几个 demo 开始。

## 当前暂未覆盖的能力

目前这个库已经覆盖了常用的连接、读写、方法、数据订阅、报警事件订阅，但还没有做这些增强项：

- 自动重连和订阅恢复
- 报警 Ack / Confirm / Shelve / Unshelve
- Browse / 元数据浏览能力
- 历史数据 / 历史事件读取
- 更高级的自定义事件过滤器
- 更完整的复杂结构体/UDT 高层封装

如果你的主要使用场景就是：

- 读变量
- 写变量
- 调方法
- 收数据变化
- 收报警事件

那么当前版本已经可以直接投入使用。
