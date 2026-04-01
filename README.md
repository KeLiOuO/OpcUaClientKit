# OpcUaClientKit

[中文](./README.zh-CN.md)

`OpcUaClientKit` is a lightweight OPC UA client library for modern .NET applications.

This repository includes the core library, a structured console demo, and regression tests against a real OPC UA server.

Note: the code in this repository was generated and iteratively refined with GPT-5.4.

## What is included

- `OpcUaClientKit`
  - The main reusable library targeting `netstandard2.1`
- `OpcUaClientKit.Demo`
  - A console demo that shows the recommended usage patterns
- `OpcUaClientKit.RegressionTests`
  - Integration-style regression tests for read/write, method calls, data subscriptions, and alarm event subscriptions
- `OpcUaClientKit.UnitTests`
  - Focused unit tests for concurrency, diagnostics, and batch error semantics
- `OpcUaClientKit.slnx`
  - The solution entry point

## Core capabilities

- Simple factory-based client creation
- Advanced builder-based configuration
- Certificate bootstrap and endpoint selection
- Single-node and batch read/write
- Method invocation
- Data subscriptions
- Alarm event subscriptions
- Optional automatic reconnect with subscription restoration

## Project structure

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

## Quick start

Build the full solution:

```bash
dotnet build .\OpcUaClientKit.slnx
```

Run the console demo:

```bash
dotnet run --project .\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- list
```

Run regression tests:

```bash
dotnet test .\OpcUaClientKit.RegressionTests\OpcUaClientKit.RegressionTests.csproj
```

Run focused unit tests:

```bash
dotnet test .\OpcUaClientKit.UnitTests\OpcUaClientKit.UnitTests.csproj
```

## Documentation

- Library guide: [OpcUaClientKit/README.md](./OpcUaClientKit/README.md)
- Demo guide: [OpcUaClientKit.Demo/README.md](./OpcUaClientKit.Demo/README.md)
- Regression test notes: [OpcUaClientKit.RegressionTests/README.md](./OpcUaClientKit.RegressionTests/README.md)

## Reconnect behavior

`OpcUaClientKit` now supports optional automatic reconnect. It is disabled by default and only starts
when explicitly configured through `OpcUaClientOptions.Reconnect` or `WithReconnect(...)`.

- Transparent recovery is limited to data subscriptions and alarm event subscriptions
- By default the first reconnect attempt starts immediately after an unexpected disconnect is detected
- Normal read/write/method calls are not retried automatically during the reconnect window
- `Disconnected`, `Reconnecting`, `AttemptFailed`, `Reconnected`, and `GaveUp` lifecycle notifications
  are exposed through `ReconnectHandler`
- A reconnect attempt is considered successful only when the new session is created and all tracked
  subscriptions are fully restored

## Recommended local test environment

The current demo and regression tests are aligned with:

- Prosys OPC UA Simulation Server
- Username/password authentication
- Auto-accepting untrusted server certificates for local testing convenience

If your server nodes differ from the defaults, update:

- [OpcUaClientKit.Demo/DemoSettings.json](./OpcUaClientKit.Demo/DemoSettings.json)
- [OpcUaClientKit.RegressionTests/RegressionTestSettings.json](./OpcUaClientKit.RegressionTests/RegressionTestSettings.json)
