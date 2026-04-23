# Siemens SINUMERIK OPC UA 常用方法与报警订阅整理

本文根据 `840Dsl_828D_OPCUA_config_man_0522_zh-CHS.pdf` 整理，面向 `OpcUaClientKit` 的实际集成使用。重点覆盖 SINUMERIK 840D sl / 828D OPC UA 服务器中常用方法、访问权限、文件/程序/刀具操作，以及报警事件节点的订阅与字段解析。

参考章节：

- 第 6 章：用户管理
- 第 7.4 节：报警
- 第 7.5 节：文件系统
- 第 7.6 / 7.7 节：选择 / 取消选择
- 第 7.8 节：刀具管理
- 第 10.1 节：技术数据

## 1. 基本约定

### 1.1 命名空间与 NodeId

Siemens 文档和示例通常把 SINUMERIK 节点放在命名空间 `ns=2`，但实际设备、CSOM 模型、网关或不同版本可能导致命名空间索引变化。生产代码建议优先通过浏览地址空间定位节点，不要永久硬编码 `ns=2`。

常见浏览路径：

| 功能 | 浏览路径 / 对象 |
| --- | --- |
| SINUMERIK 根对象 | `Objects > Sinumerik` |
| 变量访问 | `Sinumerik > Channel / Plc / GUD / TEA / SEA / Tool ...` |
| 文件系统 | `Sinumerik > FileSystem` |
| 常用方法 | `Sinumerik > Methods` |
| 文件处理方法 | `Sinumerik > Methods > Filehandling` |
| 刀具管理方法 | `Sinumerik > Methods > ToolManagement` |
| 报警事件源 | `Sinumerik` 对象本身 |

常见字符串 NodeId 形式可能类似：

```text
ns=2;s=Sinumerik
ns=2;s=/Methods/GetUserList
ns=2;s=/Methods/Filehandling/Select
ns=2;s=/Methods/ToolManagement/CreateTool
ns=2;s=Sinumerik/FileSystem/Part Program/partprg.mpf
```

这些路径用于帮助定位，最终以目标设备实际 Browse 出来的 `NodeId` 为准。

### 1.2 权限是方法能否成功的核心

SINUMERIK OPC UA 的很多操作即使连接成功，也会因为权限不足返回 `BadUserAccessDenied`。常用权限如下：

| 权限 | 用途 |
| --- | --- |
| `SinuReadAll` | 所有标准 SINUMERIK 读权限，也可用于报警订阅 |
| `SinuWriteAll` | 所有标准 SINUMERIK 写权限 |
| `AlarmRead` | 允许订阅报警 |
| `FsRead` | 文件系统读、`CopyFileFromServer` |
| `FsWrite` | 文件系统写、创建/删除/移动/写入文件、`CopyFileToServer` |
| `ApWrite` | 调用程序选择 `Select` |
| `ToolRead` | 读取刀具和刀库数据 |
| `ToolWrite` | 调用刀具管理方法 |
| `PlcRead` / `PlcWrite` | PLC 全局读写 |
| `PlcReadDBx` / `PlcWriteDBx` | 指定 PLC DB 的读写，`x` 为 DB 号 |
| `CsomReadx` / `CsomWritex` | 指定 CSOM 命名空间读写，`x` 通常为 3-9 |

多个权限用分号拼接，例如：

```text
SinuReadAll;AlarmRead;FsRead;FsWrite;ApWrite;ToolWrite
```

### 1.3 方法调用模板

`OpcUaClientKit` 的方法调用入口是 `CallMethodAsync(objectNodeId, methodNodeId, inputArguments)`。OPC UA 方法调用需要同时指定：

- `objectNodeId`：方法所属对象节点。
- `methodNodeId`：方法节点。
- `inputArguments`：输入参数，按方法签名顺序传入。

示例：

```csharp
var outputs = await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods/ToolManagement",
    methodNodeId: "ns=2;s=/Methods/ToolManagement/CreateTool",
    inputArguments: new object?[] { "1", "00042" });

var statusCode = Convert.ToUInt32(outputs[0], CultureInfo.InvariantCulture);
```

如果不确定 `objectNodeId`，建议通过浏览树拿到方法的父对象。不同 SINUMERIK 版本中方法 NodeId 的字符串形式可能不完全相同。

## 2. 用户管理方法

用户管理方法需要管理员身份连接。匿名连接主要用于调试，手册说明匿名连接下方法不可用，可能返回 `BadRequestNotAllowed`。

### 2.1 用户方法清单

| 方法 | 作用 | 输入 | 输出 / 说明 |
| --- | --- | --- | --- |
| `AddUser` | 创建用户名密码用户 | `UserName` | 新用户初始密码通常为用户名，随后应调用 `ChangeMyPassword` 修改 |
| `AddCertificateUser` | 创建证书认证用户 | `UserName`, `CertificateData` | `CertificateData` 为 `.der` 证书字节 |
| `DeleteUser` | 删除用户 | `UserName` | 不能删除 OPC UA 设置时创建的管理员用户 |
| `GetUserList` | 获取用户列表 | 无 | 返回用户列表 |
| `ChangeMyPassword` | 修改当前登录用户密码 | `OldPwd`, `NewPwd1`, `NewPwd2` | 必须以对应用户连接后调用 |
| `GetMyAccessRights` | 获取当前用户权限 | 无 | 返回当前用户权限 |
| `GetUserAccessRights` | 获取指定用户权限 | `UserName` | 管理员使用 |
| `GiveUserAccess` | 给用户授予权限 | `UserName`, `AccessRights` | 权限字符串可用分号拼接 |
| `DeleteUserAccess` | 删除用户权限 | `UserName`, `AccessRights` | 删除主权限时，相关子权限也可能被重置 |

### 2.2 常用调用示例

创建用户并授权：

```csharp
await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods",
    methodNodeId: "ns=2;s=/Methods/AddUser",
    inputArguments: new object?[] { "opc_user" });

await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods",
    methodNodeId: "ns=2;s=/Methods/GiveUserAccess",
    inputArguments: new object?[]
    {
        "opc_user",
        "SinuReadAll;AlarmRead;FsRead;FsWrite;ApWrite"
    });
```

查询当前用户权限：

```csharp
var outputs = await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods",
    methodNodeId: "ns=2;s=/Methods/GetMyAccessRights");

Console.WriteLine(outputs.Count > 0 ? outputs[0] : "(no output)");
```

## 3. 文件系统与 NC 程序文件

SINUMERIK OPC UA 支持标准 OPC UA `FileType` / `FolderType` 文件对象，也提供两个小文件快捷传输方法。快捷方法默认适合小于 16 MB 的文件；超过该大小建议使用标准 `Open / Read / Write / Close` 文件方法分块传输。

### 3.1 文件系统常见根路径

| 文件区域 | 示例路径 |
| --- | --- |
| 零件程序 | `Sinumerik/FileSystem/Part Program/partprg.mpf` |
| 子程序 | `Sinumerik/FileSystem/Sub Program/subprg.spf` |
| 工件 | `Sinumerik/FileSystem/Work Pieces/wrkprg.wpf` |
| NCExtend | `Sinumerik/FileSystem/NCExtend/Program.mpf` |
| USB / 网络共享 | `Sinumerik/FileSystem/ExtendedDrives/USBdrive/Q3.mpf` |

注意：`NCExtend`、`ExtendedDrives` 是否出现取决于系统版本、硬件和许可证。

### 3.2 标准文件夹方法

这些方法一般在文件或目录的直接父目录对象上调用。

| 方法 | 输入 | 输出 | 用途 |
| --- | --- | --- | --- |
| `CreateDirectory` | `directoryName: string` | `directoryNodeId: NodeId` | 创建目录 |
| `CreateFile` | `fileName: string`, `requestFileOpen: bool` | `fileNodeId: NodeId`, `fileHandle: uint` | 创建文件 |
| `Delete` | `objectToDelete: NodeId` | 无 | 删除文件或目录 |
| `MoveOrCopy` | `objectToMoveOrCopy: NodeId`, `targetDirectory: NodeId`, `createCopy: bool`, `newName: string` | `newNodeId: NodeId` | 移动、复制、重命名 |

### 3.3 标准文件方法

| 方法 / 属性 | 用途 |
| --- | --- |
| `Open` | 打开文件，返回文件句柄 |
| `Read` | 通过文件句柄读取字节 |
| `Write` | 通过文件句柄写入字节 |
| `Close` | 关闭文件句柄 |
| `GetPosition` | 获取文件指针位置 |
| `SetPosition` | 设置文件指针位置 |
| `Size` | 文件大小 |
| `Writable` / `UserWritable` | 是否可写 |

### 3.4 快捷传输方法

#### `CopyFileFromServer`

用途：从 SINUMERIK 服务器读取文件到客户端。

| 参数 | 类型 | 方向 | 说明 |
| --- | --- | --- | --- |
| `SourceFile` | `string` | 输入 | 服务器端完整路径 |
| `Data` | `ByteString` | 输出 | 文件原始字节 |

示例：

```csharp
var outputs = await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods/Filehandling",
    methodNodeId: "ns=2;s=/Methods/Filehandling/CopyFileFromServer",
    inputArguments: new object?[]
    {
        "Sinumerik/FileSystem/Part Program/partprg.mpf"
    });

var fileBytes = (byte[])outputs[0]!;
```

#### `CopyFileToServer`

用途：把客户端文件写入 SINUMERIK 服务器。

| 参数 | 类型 | 方向 | 说明 |
| --- | --- | --- | --- |
| `TargetFilename` | `string` | 输入 | 服务器端目标完整路径 |
| `Data` | `ByteString` | 输入 | 文件原始字节 |
| `Overwrite` | `bool` | 输入 | 是否覆盖 |

示例：

```csharp
var data = File.ReadAllBytes(@"D:\NcPrograms\partprg.mpf");

await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods/Filehandling",
    methodNodeId: "ns=2;s=/Methods/Filehandling/CopyFileToServer",
    inputArguments: new object?[]
    {
        "Sinumerik/FileSystem/Part Program/partprg.mpf",
        data,
        true
    });
```

注意：手册说明 `CreateFile`、`CopyFileToServer`、`CopyFileFromServer`、`MoveOrCopy` 不支持多个扩展名，例如 `test.mpf.mpf`。

## 4. 程序选择与取消选择

### 4.1 `Select`

用途：选择一个 NC 程序到指定通道。它只负责“选择程序”，不会启动程序执行。

位置：`Sinumerik > Methods > Filehandling > Select`

权限：`ApWrite`

签名：

```text
Select(
  [in]  string SourceFileNodeId,
  [in]  int32  ChannelNumber,
  [out] int32  StatusCode)
```

状态码：

| 状态码 | 说明 |
| --- | --- |
| `0` | 成功 |
| `1` | 通道不存在 |
| `2` | 零件程序无法找到 |
| `3` | 通道不在复位状态 |
| `4` | 目标拒绝操作 |

示例：

```csharp
var outputs = await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods/Filehandling",
    methodNodeId: "ns=2;s=/Methods/Filehandling/Select",
    inputArguments: new object?[]
    {
        "Sinumerik/FileSystem/Part Program/partprg.mpf",
        1
    });

var status = Convert.ToInt32(outputs[0], CultureInfo.InvariantCulture);
```

调用前请确认目标通道处于复位状态，否则常见返回 `3`。

### 4.2 `Unselect`

用途：按通道取消已选择程序。

位置：`Sinumerik > Methods > Filehandling > Unselect`

签名：

```text
Unselect(
  [in]  uint16 channelNo,
  [out] uint16 statusCode)
```

状态码：

| 状态码 | 说明 |
| --- | --- |
| `0` | 良好 |
| `1` | 通道未找到或不可用 |
| `2` | 未选择程序 |
| `3` | 程序运行中或通道未复位 |
| `4` | 请求被拒绝 |
| `5` | 未知错误 |

## 5. 刀具管理方法

刀具管理方法位于 `Sinumerik > Methods > ToolManagement`，需要 `ToolWrite` 权限。方法只负责创建/删除刀具或刀沿；刀具类型、刀沿参数等具体数据通常通过数据访问读写相关变量完成。

### 5.1 `CreateTool`

签名：

```text
CreateTool(
  [in]  string ToolArea,
  [in]  string ToolNumber,
  [out] uint32 StatusCode)
```

状态码：

| 状态码 | 说明 |
| --- | --- |
| `0` | 正常 |
| `1` | 刀具区域不存在 |
| `2` | 刀具号超范围 |
| `3` | 刀具号已存在 |
| `4` | 达到最大刀具数量 |

示例：

```csharp
var outputs = await client.CallMethodAsync(
    objectNodeId: "ns=2;s=/Methods/ToolManagement",
    methodNodeId: "ns=2;s=/Methods/ToolManagement/CreateTool",
    inputArguments: new object?[] { "1", "00042" });
```

### 5.2 `DeleteTool`

签名：

```text
DeleteTool(
  [in]  string ToolArea,
  [in]  string ToolNumber,
  [out] uint32 StatusCode)
```

状态码：

| 状态码 | 说明 |
| --- | --- |
| `0` | 正常 |
| `1` | 刀具区域不存在 |
| `2` | 刀具号超范围 |
| `3` | 刀具不存在 |
| `6` | 刀具生效或正在使用 |

### 5.3 `CreateCuttingEdge`

签名：

```text
CreateCuttingEdge(
  [in]  string ToolArea,
  [in]  string ToolNumber,
  [out] uint32 DNumber,
  [out] uint32 StatusCode)
```

状态码：

| 状态码 | 说明 |
| --- | --- |
| `0` | 正常 |
| `2` | 刀具号超范围 |
| `4` | 达到最大刀沿数 |
| `5` | 没有可创建刀沿的刀具 |

### 5.4 `DeleteCuttingEdge`

签名：

```text
DeleteCuttingEdge(
  [in]  string ToolArea,
  [in]  string ToolNumber,
  [in]  string CuttingEdgeNumber,
  [out] uint32 StatusCode)
```

状态码：

| 状态码 | 说明 |
| --- | --- |
| `0` | 正常 |
| `2` | 刀具号超范围 |
| `4` | 刀沿不存在 |
| `5` | 没有可删除刀沿的刀具 |
| `6` | 刀具生效或正在使用 |
| `7` | 首个刀沿无法删除 |

## 6. 报警事件订阅

### 6.1 订阅哪个节点

SINUMERIK 报警事件对象连接在 `Sinumerik` 对象上。要接收报警，事件订阅应添加在 `Sinumerik` 节点，而不是普通变量节点。

常见事件源：

```text
ns=2;s=Sinumerik
```

仍然建议实际部署时 Browse `Objects > Sinumerik` 获取准确 NodeId。

### 6.2 所需权限

订阅报警需要：

- `AlarmRead`，或
- `SinuReadAll`

没有权限时，订阅事件可能返回 `BadUserAccessDenied`。

### 6.3 报警来源与限制

SINUMERIK OPC UA 报警包含：

- HMI 报警
- NCK 报警，包括驱动报警
- 诊断缓冲报警
- PLC 报警，例如 FC10
- Alarm_S / Alarm_SQ 类报警

零件程序消息不是 OPC UA 报警事件，不会在报警订阅里出现；应通过变量路径读取，例如：

```text
/Channel/ProgramInfo/msg
```

### 6.4 推荐订阅方式

推荐使用动态字段模式，因为 Siemens 的 `CncAlarmType` 继承链较长，字段包含 BaseEvent、Condition、AlarmCondition、AcknowledgeableCondition 以及 CNC 扩展字段。

```csharp
await using var eventSubscription = await client
    .AsEventSubscribable()
    .CreateEventSubscriptionBuilder()
    .WithName("sinumerik-alarms")
    .WithPublishingInterval(500)
    .WithSelectClauseMode(OpcUaEventSelectClauseMode.Dynamic)
    .WithConditionRefreshOnStart(true)
    .BuildAsync(notification =>
    {
        Console.WriteLine(
            $"{notification.Time:yyyy-MM-dd HH:mm:ss} " +
            $"{notification.SourceName} " +
            $"Severity={notification.Severity} " +
            $"Active={notification.Active} " +
            $"Acked={notification.Acked} " +
            $"Retain={notification.Retain} " +
            $"Message={notification.Message}");

        foreach (var field in notification.SelectedFields)
        {
            Console.WriteLine($"  {field.Key}: {field.Value}");
        }
    });

await eventSubscription.AddSourceAsync(
    new OpcUaNode("ns=2;s=Sinumerik", "Sinumerik"));
```

说明：

- 默认 `AlarmConditionType` 可覆盖其派生报警类型，通常不需要显式指定 `CncAlarmType`。
- `ConditionRefreshOnStart` 用于拿到当前仍然保留的活动报警。
- `Refresh Start` / `Refresh End` 是 OPC UA 刷新边界事件，不是业务报警，显示层通常应过滤。

### 6.5 关键报警字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `EventId` | ByteString / String | 事件唯一标识 |
| `EventType` | NodeId | 对 SINUMERIK 报警通常是 `CncAlarmType` 或其相关类型 |
| `SourceNode` | NodeId | 报警源节点 |
| `SourceName` | String | 报警源名称，例如 HMI、NCK、PLC、通道等 |
| `Time` | DateTime | 事件发生时间 |
| `ReceiveTime` | DateTime | OPC UA 服务器接收时间 |
| `Message` | LocalizedText | 本地化报警文本 |
| `Severity` | UInt16 | 严重度，范围 1-1000 |
| `Retain` | Boolean | 报警是否仍应显示；活动报警通常为 `true` |
| `Quality` | Status / String | 报警质量 |
| `LastSeverity` | UInt16 | 上一次严重度 |
| `ConditionName` | String | 条件实例名称 |
| `EnabledState` | LocalizedText / TwoStateVariable | 通常启用 |
| `AckedState` | LocalizedText / TwoStateVariable | 是否已应答 |
| `ActiveState` | LocalizedText / TwoStateVariable | 是否激活 |
| `AlarmIdentifier` | String | CNC 报警编号 |
| `AuxParameters` | String | SINUMERIK 辅助参数 |
| `HelpSource` | String | 帮助信息来源 |

### 6.6 严重度建议映射

手册给出的 SINUMERIK 到 OPC UA 严重度映射可以按三档理解：

| SINUMERIK 报警等级 | OPC UA Severity | 建议 UI |
| --- | --- | --- |
| 通知 | `1` 左右 | 灰 / 蓝 |
| 警告 | `500` 左右 | 黄 / 橙 |
| 故障 | `1000` 左右 | 红 |

实际项目中也可以按 OPC UA 通用范围分色：

```text
0-199     信息
200-399   轻微
400-599   警告
600-799   严重
800-1000  故障 / 停机风险
```

### 6.7 报警顺序与刷新

连接后，服务器只会推送连接建立以后状态变化的报警。若要获取当前仍然有效的报警，应调用 `ConditionRefresh`。刷新期间可能收到边界事件：

- `Refresh Start`
- `Refresh End`

这些事件用于标记刷新范围，不应当当作机床报警展示。

### 6.8 报警应答

手册中给出了基于 OPC Foundation SDK 调用 `Acknowledge` 方法的示例，但同一版本限制章节又说明报警的应答和确认不受支持。因此在工程上应按目标设备版本实测：

- 如果目标设备支持 Ack，可调用标准 `Acknowledge` 方法。
- 如果目标设备不支持，应只做报警显示、状态跟踪和日志记录。
- 不要在未确认目标设备支持的情况下，把 Ack / Confirm 作为必须功能。

标准 Ack 调用概念：

```text
ObjectId: 具体 ConditionId
MethodId: Acknowledge 方法
Input: EventId, Comment
```

## 7. CSOM 中的报警节点

如果通过 SiOME / CSOM 建模自定义对象，也可以添加一个用于报警订阅的对象：

1. 在自定义对象下添加 `Object`。
2. 在 `Additional OPC UA Attributes` 中启用 `EventNotifier`。
3. 勾选 `SubscribeToEvents`。

手册说明这种 CSOM 报警订阅对象无需从 SINUMERIK 节点映射变量。它本质上是一个允许事件订阅的对象节点。

## 8. 技术限制与工程建议

### 8.1 数量与周期

手册技术数据中给出：

- 828D 会话数量约 5。
- 840D sl 会话数量约 10。
- 订阅数量约 5 / 10，取决于系统。
- 最小采样间隔约 100 ms。
- 发布间隔建议使用 `{100, 250, 500, 1000, 2500, 5000}` ms 这类离散值。
- 最大监控项数量与控制器负载、采样率相关。

工程建议：

- 变量变化监控优先用数据订阅，不要高频轮询。
- 报警订阅建议单独建一个事件订阅组。
- 不要为每个变量创建一个订阅；应把相关变量放在同一个订阅内。
- 机床运行负载高时，降低采样率和发布频率。

### 8.2 安全建议

- 常规运行不要依赖匿名访问。
- Siemens 手册建议使用加密通讯，并优先使用较高安全策略。
- 老版本 SinuTrain / 设备可能只支持 `Basic128Rsa15 + SignAndEncrypt`，此时可显式选择该安全组合。
- 客户端证书需要被服务器信任；调试时可启用自动接受证书，生产环境建议手动信任。

`OpcUaClientKit` 示例：

```csharp
var client = new OpcUaClientFactory()
    .CreateBuilder()
    .WithServerUrl("opc.tcp://192.168.0.10:4840")
    .WithApplicationName("OpcUaClientKit-Sinumerik")
    .WithDeviceId("sinumerik-client-01")
    .WithUserNamePassword("opc_user", "password")
    .WithSecurity(true)
    .WithSecurityProfile(
        SecurityPolicies.Basic128Rsa15,
        MessageSecurityMode.SignAndEncrypt)
    .WithAutoAcceptUntrustedServerCertificate()
    .Build();

await client.ConnectAsync();
```

## 9. 推荐集成清单

最小可用能力：

- 连接使用用户名密码或证书认证。
- 通过 `GetMyAccessRights` 在启动时显示当前权限。
- 数据订阅用于关键变量变化。
- 报警订阅添加 `Sinumerik` 事件源，并启用 `ConditionRefreshOnStart`。
- 报警 UI 过滤 `Refresh Start / Refresh End`。
- NC 文件传输小于 16 MB 可用快捷方法，大文件用标准文件方法。
- 程序选择前检查通道复位状态。
- 刀具管理方法调用后同时解析 OPC UA 调用结果和业务 `StatusCode` 输出参数。

常见故障排查：

| 现象 | 优先检查 |
| --- | --- |
| 用户名密码连接失败 | HMI 时间、证书有效期、服务器是否信任客户端证书、安全策略是否匹配 |
| 方法返回 `BadUserAccessDenied` | 当前用户是否有对应权限 |
| 报警订阅失败 | 是否订阅 `Sinumerik` 对象、是否有 `AlarmRead` 或 `SinuReadAll` |
| 看不到当前已有报警 | 是否调用 `ConditionRefresh` |
| 程序选择失败 | 通道是否复位、文件 NodeId 是否正确、是否有 `ApWrite` |
| 文件写入失败 | 是否有 `FsWrite`、文件扩展名是否符合限制、文件是否被占用 |
| 刀具删除失败 | 刀具是否正在使用、是否有 `ToolWrite` |
