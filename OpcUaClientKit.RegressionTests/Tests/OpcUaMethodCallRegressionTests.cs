using OpcUaClientKit;
using OpcUaClientKit.RegressionTests.Infrastructure;
using Xunit;

namespace OpcUaClientKit.RegressionTests.Tests;

[Collection(OpcUaIntegrationCollection.Name)]
public sealed class OpcUaMethodCallRegressionTests
{
    private readonly OpcUaIntegrationFixture _fixture;

    public OpcUaMethodCallRegressionTests(OpcUaIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_call_method_with_arguments_and_receive_expected_output()
    {
        var settings = _fixture.Settings;
        var objectNode = settings.MethodObjectNode.ToOpcUaNode();
        var methodNode = settings.MethodNode.ToOpcUaNode();

        await using var client = await _fixture.CreateConnectedClientAsync();

        var outputs = await client.CallMethodAsync(
            objectNode,
            methodNode,
            settings.CreateMethodArguments());

        Assert.NotEmpty(outputs);

        var firstOutput = RegressionTestHelpers.ToDouble(outputs[0], "Method output");
        var delta = Math.Abs(firstOutput - settings.MethodExpectedFirstOutput);
        Assert.True(
            delta <= settings.MethodExpectedTolerance,
            $"Expected first method output to be {settings.MethodExpectedFirstOutput}, actual {firstOutput}.");
    }
}
