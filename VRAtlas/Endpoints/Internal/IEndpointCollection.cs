namespace VRAtlas.Endpoints.Internal;

public interface IEndpointCollection
{
    public static virtual void AddServices(IServiceCollection services) { }
    public static abstract void BuildEndpoints(IEndpointRouteBuilder app);
}