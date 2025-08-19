using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Common.Behaviours;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count == 0)
            return await next();

        // Determine if the response type is IResult or Result<T>
        if (typeof(IResult).IsAssignableFrom(typeof(TResponse)))
        {
            // For IResult responses (ASP.NET Core Minimal API)
            return (TResponse)(object)CreateHttpValidationErrorResponse(failures);
        }
        else if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            // For our custom Result<T> responses
            return CreateResultValidationErrorResponse(failures);
        }
        else
        {
            // For other response types - create a generic error response
            throw new ValidationException(failures);
        }
    }

    private IResult CreateHttpValidationErrorResponse(List<ValidationFailure> failures)
    {
        var errorsByProperty = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return Results.ValidationProblem(
            errors: errorsByProperty,
            title: "Validation error",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private TResponse CreateResultValidationErrorResponse(List<ValidationFailure> failures)
    {
        // Create a Result<T> instance with validation errors
        var resultType = typeof(TResponse);
        var resultInstance = Activator.CreateInstance(resultType);

        // Set the basic properties
        resultType.GetProperty("Succeeded")?.SetValue(resultInstance, false);
        resultType.GetProperty("Message")?.SetValue(resultInstance, "Validation failed");

        // Group errors by property name (same format as IResult)
        var errorsByProperty = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());

        // Set the errors property - now using a dictionary instead of a flat list
        var errorsProperty = resultType.GetProperty("Errors");
        if (errorsProperty != null)
        {
            // Check if Errors property is IDictionary or IList
            if (errorsProperty.PropertyType.IsGenericType &&
                errorsProperty.PropertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                // If it's a dictionary, set the grouped errors
                errorsProperty.SetValue(resultInstance, errorsByProperty);
            }
            else
            {
                // If it's a list or another type, use the flat list (backward compatibility)
                var errorMessages = failures.Select(f => f.ErrorMessage).ToList();
                errorsProperty.SetValue(resultInstance, errorMessages);
            }
        }

        return (TResponse)resultInstance!;
    }
}

public class ValidationErrorResponse
{
    public bool Succeeded { get; set; }
    public string Message { get; set; }
    public IDictionary<string, string[]> Errors { get; set; }

    public ValidationErrorResponse()
    {
        Succeeded = false;
        Message = "Validation error";
        Errors = new Dictionary<string, string[]>();
    }
}