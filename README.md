# OpcUaClientKit

[中文](./README.zh-CN.md)

`OpcUaClientKit` is a lightweight OPC UA client library for modern .NET applications.

This repository includes the core library, a structured console demo, and regression tests against a real OPC UA server.

## What is included

- `OpcUaClientKit`
  - The main reusable library targeting `netstandard2.1`
- `OpcUaClientKit.Demo`
  - A console demo that shows the recommended usage patterns
- `OpcUaClientKit.RegressionTests`
  - Integration-style regression tests for read/write, method calls, data subscriptions, and alarm event subscriptions
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
`-- OpcUaClientKit.slnx
```

## Quick start

Build the full solution:

```bash
dotnet build C:\Code\ConsoleApp\OpcUaClientKit.slnx
```

Run the console demo:

```bash
dotnet run --project C:\Code\ConsoleApp\OpcUaClientKit.Demo\OpcUaClientKit.Demo.csproj -- list
```

Run regression tests:

```bash
dotnet test C:\Code\ConsoleApp\OpcUaClientKit.RegressionTests\OpcUaClientKit.RegressionTests.csproj
```

## Documentation

- Library guide: [OpcUaClientKit/README.md](./OpcUaClientKit/README.md)
- Demo guide: [OpcUaClientKit.Demo/README.md](./OpcUaClientKit.Demo/README.md)
- Regression test notes: [OpcUaClientKit.RegressionTests/README.md](./OpcUaClientKit.RegressionTests/README.md)

## Recommended local test environment

The current demo and regression tests are aligned with:

- Prosys OPC UA Simulation Server
- Username/password authentication
- Auto-accepting untrusted server certificates for local testing convenience

If your server nodes differ from the defaults, update:

- [OpcUaClientKit.Demo/DemoSettings.json](./OpcUaClientKit.Demo/DemoSettings.json)
- [OpcUaClientKit.RegressionTests/RegressionTestSettings.json](./OpcUaClientKit.RegressionTests/RegressionTestSettings.json)
