using Xunit;

namespace VRAtlas.Tests.Integration.Status;

[CollectionDefinition(Definition)]
public class StatusCollection : ICollectionFixture<VRAtlasFactory>
{
    public const string Definition = "Status Collection";
}