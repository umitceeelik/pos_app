using FluentValidation;

namespace Server.Common;

/// <summary>
/// Minimal API endpoint filter that automatically runs FluentValidation
/// for a given request body type <typeparamref name="T"/>.
/// If validation fails, it returns HTTP 400 with ProblemDetails-like payload.
/// </summary>
public class ValidateRequestFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        // Resolve validator; if none, skip validation.
        var validator = ctx.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null) return await next(ctx);

        // Find the first argument matching T (the bound body)
        var model = ctx.Arguments.FirstOrDefault(a => a is T) as T;
        if (model is null) return await next(ctx);

        // Run validation
        var result = await validator.ValidateAsync(model);
        if (!result.IsValid)
        {
            // Convert to a dictionary that Minimal APIs understand for 400 response
            return Results.ValidationProblem(result.ToDictionary());
        }

        // Proceed to actual endpoint
        return await next(ctx);
    }
}
