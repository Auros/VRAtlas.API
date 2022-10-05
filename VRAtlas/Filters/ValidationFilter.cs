using FluentValidation;

namespace VRAtlas.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.GetArgument<T>(0); // Arguments.SingleOrDefault(a => a is T);

        // If there's nothing to validate, then the request is invalid.
        if (argument is null)
            return Results.BadRequest();

        var result = await _validator.ValidateAsync(argument);
        if (!result.IsValid)
            return Results.BadRequest(new FilterValidationResponse(result.Errors.Select(e => e.ErrorMessage)));

        return await next(context);
    }
}