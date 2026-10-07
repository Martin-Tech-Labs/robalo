using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Robalo.Controller.Api.Filters;

public sealed class InvalidBindingFilter(
    ProblemDetailsFactory problemDetailsFactory) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var problem = problemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Malformed request body.",
            detail: "invalid_json");

        context.Result = new BadRequestObjectResult(problem);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}