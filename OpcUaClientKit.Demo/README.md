# OpcUaClientKit Console Demo

这个 demo 项目演示 `OpcUaClientKit` 在真实 OPC UA 服务器上的常见使用方式，默认按本地 `Prosys OPC UA Simulation Server` 的测试配置准备。

## 默认连接配置

- 服务器地址：`opc.tcp://127.0.0.1:4840`
- 用户名：`OpcUaClient`
- 密码：`123456`
- 默认启用安全连接
- 默认接受不信任的服务端证书，方便本地联调

配置文件位于 [DemoSettings.json](C:\Code\ConsoleApp\OpcUaClientKit.Demo\DemoSettings.json)。

## 场景列表

- `quickstart`
  - 用简单工厂重载连接服务器并读取一个节点
- `readwrite`
  - 演示单点读取、单点写入、批量写入和批量读取
- `method`
  - 演示使用 Builder 连接并执行方法调用
- `data-sub`
  - 演示创建空数据订阅组、动态添加节点和接收数据变化
- `event-sub`
  - 演示创建空事件订阅组、添加报警源、刷新 retained alarm 和打印事件字段

如果你想在 demo 里体验自动重连，可以参考类库 README 中的 `WithReconnect(...)`
示例，把同样的配置加到 demo 创建客户端的代码里。当前 demo 不会默认开启自动重连，
这样更方便区分“基础连接行为”和“重连行为”。

## 运行方式

列出可用场景：

```bash
dotnet run --project C:\Code\ConsoleApp\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- list
```

运行快速开始：

```bash
dotnet run --project C:\Code\ConsoleApp\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- quickstart
```

运行事件订阅：

```bash
dotnet run --project C:\Code\ConsoleApp\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- event-sub
```

## Prosys 测试建议

如果你使用的是 `Prosys OPC UA Simulation Server`，建议至少准备这些节点：

- `ns=6;s=MyLevel`
- `ns=3;s=/Plc/DB66.DBW0`
- `ns=3;s=/Plc/DB66.DBW2`
- `ns=6;s=MyDevice`
- `ns=6;s=MyMethod`
- `ns=6;s=MyObjectsFolder`

事件订阅场景默认假设：

- `MyLevel` 的值变化可以触发报警
- `MyObjectsFolder` 可以作为事件源

如果你的服务器节点命名不同，只需要修改 `DemoSettings.json`。
