using System.Reflection;

namespace VRAtlas.Endpoints.Internal;

public static class EndpointExtensions
{
    public static void AddVRAtlasEndpoints(this IServiceCollection services)
    {
        foreach (var type in GetEndpointCollectionTypes())
            type.GetMethod(nameof(IEndpointCollection.AddServices))?.Invoke(null, new[] { services });
    }

    public static void UseVRAtlasEndpoints(this IEndpointRouteBuilder builder)
    {
        foreach (var type in GetEndpointCollectionTypes())
            type.GetMethod(nameof(IEndpointCollection.BuildEndpoints))!.Invoke(null, new[] { builder });
    }

    private static IEnumerable<TypeInfo> GetEndpointCollectionTypes()
    {
        return Assembly.GetExecutingAssembly().DefinedTypes.Where(x => !x.IsAbstract && !x.IsInterface && typeof(IEndpointCollection).IsAssignableFrom(x));
    }
}