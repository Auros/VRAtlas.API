namespace VRAtlas.Endpoints.Internal;

public interface IEndpointCollection
{
    public static abstract void AddServices(IServiceCollection services);
    public static abstract void BuildEndpoints(IEndpointRouteBuilder app);
}