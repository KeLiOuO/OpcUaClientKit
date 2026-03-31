# OpcUaClientKit

[English](./README.md)

`OpcUaClientKit` 是一个面向现代 .NET 应用的轻量 OPC UA 客户端类库。

这个仓库包含核心类库、规范化的控制台 demo，以及面向真实 OPC UA 服务器的回归测试。

## 仓库内容

- `OpcUaClientKit`
  - 核心可复用类库，目标框架为 `netstandard2.1`
- `OpcUaClientKit.Demo`
  - 规范化的控制台示例项目，演示推荐用法
- `OpcUaClientKit.RegressionTests`
  - 面向真实 OPC UA 服务端的回归测试项目，覆盖读写、方法调用、数据订阅和报警事件订阅
- `OpcUaClientKit.UnitTests`
  - 面向并发、诊断能力和批量错误语义的单元测试项目
- `OpcUaClientKit.slnx`
  - 解决方案入口

## 核心能力

- 基于工厂的简单客户端创建方式
- 基于 Builder 的高级配置方式
- 证书初始化和 Endpoint 选择
- 单节点与批量读写
- 方法调用
- 数据订阅
- 报警事件订阅

## 项目结构

```text
.
|-- OpcUaClientKit
|   |-- Abstractions
|   |-- Client
|   |-- Configuration
|   |-- DependencyInjection
|   |-- Events
|   |-- Extensions
|   |-- Models
|   |-- Samples
|   `-- Subscriptions
|-- OpcUaClientKit.Demo
|-- OpcUaClientKit.RegressionTests
|-- OpcUaClientKit.UnitTests
`-- OpcUaClientKit.slnx
```

## 快速开始

构建整个解决方案：

```bash
dotnet build C:\Code\ConsoleApp\OpcUaClientKit.slnx
```

运行控制台 demo：

```bash
dotnet run --project C:\Code\ConsoleApp\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- list
```

运行回归测试：

```bash
dotnet test C:\Code\ConsoleApp\OpcUaClientKit.RegressionTests\OpcUaClientKit.RegressionTests.csproj
```

运行单元测试：

```bash
dotnet test C:\Code\ConsoleApp\OpcUaClientKit.UnitTests\OpcUaClientKit.UnitTests.csproj
```

## 文档入口

- 类库使用说明：[OpcUaClientKit/README.md](./OpcUaClientKit/README.md)
- Demo 说明：[OpcUaClientKit.Demo/README.md](./OpcUaClientKit.Demo/README.md)
- 回归测试说明：[OpcUaClientKit.RegressionTests/README.md](./OpcUaClientKit.RegressionTests/README.md)

## 推荐本地测试环境

当前 demo 和回归测试默认按下面的环境准备：

- Prosys OPC UA Simulation Server
- 用户名/密码认证
- 为了方便本地联调，默认接受不信任的服务端证书

如果你的服务端节点与默认值不同，请修改：

- [OpcUaClientKit.Demo/DemoSettings.json](./OpcUaClientKit.Demo/DemoSettings.json)
- [OpcUaClientKit.RegressionTests/RegressionTestSettings.json](./OpcUaClientKit.RegressionTests/RegressionTestSettings.json)
