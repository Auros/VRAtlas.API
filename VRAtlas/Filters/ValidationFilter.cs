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
        // Find the validation argument and throw bad request if it does not exist (nothing to validate).
        if (context.Arguments.FirstOrDefault(arg => arg is T) is not T argument)
            return Results.BadRequest();

        var result = await _validator.ValidateAsync(argument);
        if (!result.IsValid)
            return Results.BadRequest(new FilterValidationResponse(result.Errors.Select(e => e.ErrorMessage)));

        return await next(context);
    }
}