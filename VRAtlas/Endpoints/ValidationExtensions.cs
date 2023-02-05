using VRAtlas.Filters;

namespace VRAtlas.Endpoints;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder AddValidationFilter<TFilter>(this RouteHandlerBuilder builder) where TFilter : class
    {
        return builder.AddEndpointFilter<ValidationFilter<TFilter>>();
    }
}