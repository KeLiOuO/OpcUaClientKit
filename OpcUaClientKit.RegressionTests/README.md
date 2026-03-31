# OpcUaClientKit Regression Tests

这个项目是 `OpcUaClientKit` 的本地回归测试工程，默认按当前 demo 的思路连接本机 `Prosys OPC UA Simulation Server`。

## 默认测试内容

- 用户名密码连接
- 单点读取
- 单点写入和批量写入
- 方法调用
- 数据订阅
- 报警事件订阅

## 默认连接参数

- `ServerUrl`: `opc.tcp://127.0.0.1:4840`
- `UserName`: `OpcUaClient`
- `Password`: `123456`
- `AutoAcceptUntrustedServerCertificate`: `true`

## 节点配置

测试使用的节点定义在 [RegressionTestSettings.json](C:/Code/ConsoleApp/OpcUaClientKit.RegressionTests/RegressionTestSettings.json)。

如果你的 Prosys 模拟器节点名和 demo 不完全一致，直接改这个文件即可。常见需要对齐的节点包括：

- `levelNode`
- `writableNode1`
- `writableNode2`
- `methodObjectNode`
- `methodNode`
- `alarmSourceNode`

## 运行方式

```powershell
dotnet test C:\Code\ConsoleApp\OpcUaClientKit.RegressionTests\OpcUaClientKit.RegressionTests.csproj
```

建议在运行前先确认：

- Prosys 模拟器已经启动
- 对应用户名密码可登录
- 服务端证书允许客户端自动接受
- 测试涉及的节点和方法已经存在
