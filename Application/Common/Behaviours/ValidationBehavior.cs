using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        var context = new ValidationContext<TRequest>(request);

        var validationTasks = _validators
       .Select(v => v.ValidateAsync(context, cancellationToken)) // Call ValidateAsync
       .ToList();

        var validationResults = await Task.WhenAll(validationTasks);

        var failures = validationResults
       .SelectMany(result => result.Errors)
       .Where(f => f != null)
       .ToList();

        if (failures.Count != 0)
        {
            return (TResponse)(object)CreateValidationErrorResponse(failures);
        }

        return await next();
    }
    private static Task<TResponse> ThrowErrors(IEnumerable<ValidationFailure> failures)
    {
        IDictionary<string, string[]> Errors = new Dictionary<string, string[]>();
        //var response = new Response<string>();
        var failureGroups = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage);

        foreach (var failureGroup in failureGroups)
        {
            var propertyName = failureGroup.Key;
            var propertyFailures = failureGroup.ToArray();

            Errors.Add(propertyName, propertyFailures);
        }
        var response = CreateErrorResponse<TResponse>(Errors);

        return Task.FromResult(response);
    }
    private static TResponse CreateErrorResponse<TResponse>(IDictionary<string, string[]> errors)
    {
        // Use reflection or factory methods to dynamically create the response type and set errors
        var response = Activator.CreateInstance<TResponse>();
        var errorsProperty = typeof(TResponse).GetProperty("Errors");
        var succeededProperty = typeof(TResponse).GetProperty("Succeeded");
        var messageProperty = typeof(TResponse).GetProperty("Message");

        if (errorsProperty != null && succeededProperty != null && messageProperty != null)
        {
            errorsProperty.SetValue(response, errors);
            succeededProperty.SetValue(response, false);
            messageProperty.SetValue(response, "Validation-error");
        }

        return response;
    }

    private IResult CreateValidationErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        var errorResponse = new
        {
            Errors = failures
                .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray()),
            Message = "Validation error"
        };

        return Results.ValidationProblem(errorResponse.Errors, errorResponse.Message);
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

