using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Roomly.Api.Common.Errors;

/// <summary>400 <c>ValidationProblemDetails</c> + <c>code: VALIDATION_FAILED</c> üretir.</summary>
public static class ValidationProblems
{
    public static IActionResult FromModelState(ActionContext context) =>
        Create(new ValidationProblemDetails(context.ModelState));

    /// <summary>FluentValidation sonuçları gibi alan hatalarından (alan → mesajlar).</summary>
    public static IActionResult FromErrors(IDictionary<string, string[]> errors) =>
        Create(new ValidationProblemDetails(errors));

    /// <summary>FluentValidation sonucundan; alan adları camelCase (istek gövdesiyle aynı).</summary>
    public static IActionResult FromValidation(ValidationResult result) =>
        FromErrors(result.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()));

    private static ObjectResult Create(ValidationProblemDetails problem)
    {
        problem.Status = StatusCodes.Status400BadRequest;
        problem.Title = "Gönderilen bilgiler geçersiz.";
        problem.Extensions["code"] = ErrorCodes.ValidationFailed;
        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }
}
