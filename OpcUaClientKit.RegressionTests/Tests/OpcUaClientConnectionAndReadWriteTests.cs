using OpcUaClientKit;
using OpcUaClientKit.RegressionTests.Infrastructure;
using Xunit;

namespace OpcUaClientKit.RegressionTests.Tests;

[Collection(OpcUaIntegrationCollection.Name)]
public sealed class OpcUaClientConnectionAndReadWriteTests
{
    private readonly OpcUaIntegrationFixture _fixture;

    public OpcUaClientConnectionAndReadWriteTests(OpcUaIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_connect_and_disconnect_with_user_authentication()
    {
        await using var client = _fixture.CreateClient();

        Assert.False(client.IsConnected);

        await client.ConnectAsync();

        Assert.True(client.IsConnected);

        await client.DisconnectAsync();

        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task Can_read_single_nodes_and_write_single_and_batch_values()
    {
        var settings = _fixture.Settings;
        var levelNode = settings.LevelNode.ToOpcUaNode();
        var writableNode1 = settings.WritableNode1.ToOpcUaNode();
        var writableNode2 = settings.WritableNode2.ToOpcUaNode();

        await using var client = await _fixture.CreateConnectedClientAsync();

        var level = await client.ReadNodeAsync<double>(levelNode);
        Assert.False(double.IsNaN(level));

        var originalValue1 = await client.ReadNodeAsync<short>(writableNode1);
        var originalValue2 = await client.ReadNodeAsync<short>(writableNode2);
        var singleWriteValue = originalValue1 == settings.SingleWriteValue
            ? checked((short)(settings.SingleWriteValue + 1))
            : settings.SingleWriteValue;
        var batchWriteValue1 = originalValue1 == settings.BatchWriteValue1
            ? checked((short)(settings.BatchWriteValue1 + 1))
            : settings.BatchWriteValue1;
        var batchWriteValue2 = originalValue2 == settings.BatchWriteValue2
            ? checked((short)(settings.BatchWriteValue2 + 1))
            : settings.BatchWriteValue2;

        try
        {
            await client.WriteNodeAsync(writableNode1, singleWriteValue);
            var singleReadBack = await client.ReadNodeAsync<short>(writableNode1);
            Assert.Equal(singleWriteValue, singleReadBack);

            await client.WriteNodesAsync(new Dictionary<OpcUaNode, object?>
            {
                [writableNode1] = batchWriteValue1,
                [writableNode2] = batchWriteValue2
            });

            var batchValues = await client.ReadNodesAsync(new[] { writableNode1, writableNode2 });
            Assert.Equal(batchWriteValue1, RegressionTestHelpers.ToInt16(batchValues[writableNode1.NodeId], writableNode1.NodeId));
            Assert.Equal(batchWriteValue2, RegressionTestHelpers.ToInt16(batchValues[writableNode2.NodeId], writableNode2.NodeId));
        }
        finally
        {
            await client.WriteNodesAsync(new Dictionary<OpcUaNode, object?>
            {
                [writableNode1] = originalValue1,
                [writableNode2] = originalValue2
            });
        }
    }
}
