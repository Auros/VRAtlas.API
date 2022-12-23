using Xunit;

namespace VRAtlas.Tests.Integration.User;

[CollectionDefinition(Definition)]
public class UserCollection : ICollectionFixture<VRAtlasFactory>
{
    public const string Definition = "Auth Collection";
}