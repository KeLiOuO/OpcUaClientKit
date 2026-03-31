# OpcUaClientKit

`OpcUaClientKit` 是一个轻量、可复用的 OPC UA 客户端类库，基于 `OPCFoundation.NetStandard.Opc.Ua.Client`，目标是把日常项目里最常用的 OPC UA 客户端能力收敛成一套简单、稳定、可扩展的 API。

它适合这类场景：

- Console 工具
- WinForms / WPF 桌面程序
- ASP.NET Core / Worker Service
- 需要快速集成 OPC UA 连接、读写、方法调用和订阅能力的业务项目

## 功能概览

- 工厂模式创建客户端
- 简单参数重载和 Builder 两种配置方式
- 自动完成配置、证书检查/生成、Endpoint 选择、用户身份和 Session 创建
- 单节点读取、泛型读取、批量读取
- 单节点写入、批量写入
- OPC UA 方法调用
- 数据订阅
- 报警事件订阅
- 同时支持原始 `nodeId` 和封装后的 `OpcUaNode`

## 目标框架

当前类库使用 `netstandard2.1` 构建。

这意味着它可以直接用于支持 `netstandard2.1` 的现代 .NET 项目，例如：

- `.NET Core 3.1`
- `.NET 5`
- `.NET 6`
- `.NET 7`
- `.NET 8+`

## 安装

本地项目引用：

```xml
<ProjectReference Include="..\OpcUaClientKit\OpcUaClientKit.csproj" />
```

NuGet 安装：

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

最简单的连接方式：

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

var value = await client.ReadNodeAsync("ns=6;s=MyLevel");
Console.WriteLine(value);
```

## 简单配置与复杂配置

### 简单配置

简单配置适合大多数业务代码：

```csharp
var client = factory.Create(
    serverUrl: "opc.tcp://127.0.0.1:4840",
    applicationName: "MyOpcUaApp",
    deviceId: "device-a",
    userName: "OpcUaClient",
    password: "123456",
    autoAcceptUntrustedServerCertificate: true);
```

当提供 `deviceId` 时，默认 PKI 目录会按下面的结构组织：

```text
%LocalApplicationData%\OPC Foundation\<applicationName>\<deviceId>\pki
```

### Builder 配置

当你需要更细粒度控制时，可以使用 Builder：

```csharp
var client = await factory
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
    .BuildConnectedAsync();
```

也可以直接传完整配置对象：

```csharp
var options = new OpcUaClientOptions
{
    ServerUrl = "opc.tcp://127.0.0.1:4840",
    ApplicationName = "MyOpcUaApp",
    DeviceId = "device-c",
    UserName = "OpcUaClient",
    Password = "123456",
    UseSecurity = true,
    SessionTimeout = 60000,
    Certificate =
    {
        AutoAcceptUntrustedServerCertificate = true
    }
};

await using var client = await factory.CreateConnectedAsync(options);
```

## 节点封装

如果你希望在读、写、订阅时保留业务显示名，可以使用 `OpcUaNode`：

```csharp
var levelNode = new OpcUaNode("ns=6;s=MyLevel", "Level");
var value = await client.ReadNodeAsync(levelNode);
```

原始 `string nodeId` 调用方式仍然保留，`OpcUaNode` 只是更易用的轻量封装。

## 读写

单节点读取：

```csharp
var rawValue = await client.ReadNodeAsync("ns=6;s=MyLevel");
var typedValue = await client.ReadNodeAsync<double>("ns=6;s=MyLevel");
```

批量读取：

```csharp
var values = await client.ReadNodesAsync(new[]
{
    "ns=3;s=/Plc/DB66.DBW0",
    "ns=3;s=/Plc/DB66.DBW2"
});
```

单节点写入：

```csharp
await client.WriteNodeAsync("ns=3;s=/Plc/DB66.DBW0", (short)1);
```

批量写入：

```csharp
await client.WriteNodesAsync(new Dictionary<string, object?>
{
    ["ns=3;s=/Plc/DB66.DBW0"] = (short)1,
    ["ns=3;s=/Plc/DB66.DBW2"] = (short)3
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

## 方法调用

```csharp
var objectNode = new OpcUaNode("ns=6;s=MyDevice", "MyDevice");
var methodNode = new OpcUaNode("ns=6;s=MyMethod", "MyMethod");

var outputs = await client.CallMethodAsync(
    objectNode,
    methodNode,
    new object?[] { "sin", 90d });
```

## 数据订阅

快速订阅单节点：

```csharp
var subscribable = client.AsSubscribable();

await using var subscription = await subscribable.SubscribeNodeAsync(
    "ns=6;s=MyLevel",
    notification =>
    {
        Console.WriteLine(
            $"{notification.NodeId} = {notification.Value}, Good={notification.IsGood}");
    });
```

创建空订阅组，再动态添加节点：

```csharp
var subscribable = client.AsSubscribable();

await using var subscription = await subscribable
    .CreateSubscriptionBuilder()
    .WithName("runtime-data")
    .WithPublishingInterval(250)
    .BuildAsync();

await subscription.AddNodesAsync(new[]
{
    new OpcUaSubscriptionNodeDefinition(
        new OpcUaNode("ns=3;s=/Plc/DB66.DBW0", "WritableNode1"),
        notification => Console.WriteLine($"{notification.DisplayName} = {notification.Value}")),
    new OpcUaSubscriptionNodeDefinition(
        new OpcUaNode("ns=6;s=MyLevel", "Level"),
        notification => Console.WriteLine($"{notification.DisplayName} = {notification.Value}"))
});
```

## 报警事件订阅

创建空事件订阅组，再动态添加事件源：

```csharp
var eventClient = client.AsEventSubscribable();

await using var subscription = await eventClient
    .CreateEventSubscriptionBuilder()
    .WithName("alarm-events")
    .WithPublishingInterval(500)
    .WithConditionRefreshOnStart(true)
    .WithSelectClauseMode(OpcUaEventSelectClauseMode.Dynamic)
    .BuildAsync(notification =>
    {
        Console.WriteLine(
            $"[{notification.Time:HH:mm:ss}] {notification.SourceName} | " +
            $"Message={notification.Message} | Severity={notification.Severity}");
    });

await subscription.AddSourceAsync(new OpcUaNode("ns=6;s=MyObjectsFolder", "MyObjects"));
```

说明：

- 默认事件类型是 `AlarmConditionType`
- 默认 `SelectClauses` 模式是 `Dynamic`
- `Fixed` 模式仍然保留
- 默认会在添加事件源后执行 `ConditionRefresh`
- `Refresh Start` / `Refresh End` 已在库内过滤
- `SelectedFields` 会按实际 `SelectClauses` 顺序返回，适合调试和通用表格展示

## 项目结构

- `Abstractions`
  - 公共接口定义
- `Client`
  - 客户端核心、工厂、Builder 和连接流程
- `Configuration`
  - 客户端配置模型
- `DependencyInjection`
  - DI 扩展
- `Events`
  - 报警事件订阅相关类型
- `Extensions`
  - `AsSubscribable()`、`AsEventSubscribable()` 扩展入口
- `Models`
  - 通用节点模型
- `Subscriptions`
  - 数据订阅相关类型
- `Samples`
  - 参考配置样例

## Demo 项目

当前解决方案里只保留一个新的规范化 console demo：

- `OpcUaClientKit.Demo`

这个 demo 按场景拆分：

- `quickstart`
- `readwrite`
- `method`
- `data-sub`
- `event-sub`

运行示例：

```bash
dotnet run --project C:\Code\ConsoleApp\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- quickstart
```

Demo 配置文件：

- `C:\Code\ConsoleApp\OpcUaClientKit.Demo\DemoSettings.json`

## 回归测试

当前解决方案还包含一个基于真实服务器联调的回归测试项目：

- `OpcUaClientKit.RegressionTests`

默认测试场景基于本地 `Prosys OPC UA Simulation Server`，覆盖：

- 连接 / 断开
- 单点与批量读写
- 方法调用
- 数据订阅
- 事件订阅

运行方式：

```bash
dotnet test C:\Code\ConsoleApp\OpcUaClientKit.RegressionTests\OpcUaClientKit.RegressionTests.csproj
```

## 当前边界

当前版本已经覆盖常见的连接、读写、方法、数据订阅和报警事件订阅，但仍未包含这些增强项：

- 自动重连和订阅恢复
- 报警 Ack / Confirm / Shelve / Unshelve
- Browse / 节点元数据浏览
- 历史数据 / 历史事件读取
- 更高级的自定义事件过滤器
- 更完整的复杂结构体 / UDT 高层封装

如果你的主要场景是：

- 读变量
- 写变量
- 调方法
- 收数据变化
- 收报警事件

那么当前版本已经可以直接投入使用。
