using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;

namespace Robalo.Controller.Api.Validation;

public class ResultFactory(ProblemDetailsFactory problemDetailsFactory) : IFluentValidationAutoValidationResultFactory
{
    public async Task<IActionResult?> CreateActionResult(
        ActionExecutingContext context,
        ValidationProblemDetails validationProblemDetails,
        IDictionary<IValidationContext, ValidationResult> validationResults)
    {
        var problem = problemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Validation failed",
            detail: validationProblemDetails.Errors.First().Value.First());

        return new UnprocessableEntityObjectResult(problem);
    }
}