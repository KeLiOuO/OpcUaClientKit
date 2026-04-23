# OpcUaClientKit WinForms Demo

这是一个面向 `.NET Framework 4.6.2` 的 WinForms 示例，用来演示 `OpcUaClientKit`
的完整常用功能。

## 功能

- 连接 / 断开
- 自动重连通知
- 单点读 / 单点写
- 批量读 / 批量写
- 方法调用
- 数据订阅
- 报警事件订阅
- 诊断日志
- 显式安全策略和安全模式选择，例如 `Basic128Rsa15 + SignAndEncrypt`

报警事件页显示的是“当前报警列表”：收到 `Active=false` 或 `Retain=false`
的恢复/清除事件后，会从报警表格中移除对应行，并在 Log 页记录清除信息。

## 环境要求

- Visual Studio 2019/2022 或 MSBuild/.NET SDK
- .NET Framework 4.6.2 Developer Pack
- 可访问的 OPC UA 服务端

如果构建时报缺少 `.NETFramework,Version=v4.6.2` reference assemblies，请先安装
`.NET Framework 4.6.2 Developer Pack`。

## 运行

```bash
dotnet build .\OpcUaClientKit.slnx
```

然后运行：

```bash
.\OpcUaClientKit.WinFormsDemo\bin\Debug\net462\OpcUaClientKit.WinFormsDemo.exe
```

## SinuTrain / Siemens 旧服务端建议

如果 UaExpert 中验证通过的是：

- SecurityPolicy：`Basic128Rsa15`
- SecurityMode：`SignAndEncrypt`
- Identity：`UserName`

可以在 Connection 页里选择：

- Security Policy：`http://opcfoundation.org/UA/SecurityPolicy#Basic128Rsa15`
- Security Mode：`SignAndEncrypt`
- 取消 Anonymous，填写用户名密码

如果服务端证书较旧，可以把 `Minimum Key Size` 降到 `1024`，并勾选自动接受不信任证书。
