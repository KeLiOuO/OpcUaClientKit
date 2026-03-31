using Xunit;

namespace OpcUaClientKit.RegressionTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class OpcUaIntegrationCollection : ICollectionFixture<OpcUaIntegrationFixture>
{
    public const string Name = "OpcUa integration";
}
