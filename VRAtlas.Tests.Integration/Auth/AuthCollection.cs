using Xunit;

namespace VRAtlas.Tests.Integration.Auth;

[CollectionDefinition(Definition)]
public class AuthCollection : ICollectionFixture<VRAtlasFactory>
{
    public const string Definition = "Auth Collection";
}