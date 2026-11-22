using Microsoft.AspNetCore.Mvc;
using Roomly.Api.Common.Results;

namespace Roomly.Api.Common.Http;

/// <summary>
/// Controller'ların <see cref="Result{T}"/> değerini HTTP yanıtına çevirdiği tek yer.
/// Örnek: <c>return result.ToActionResult(Ok);</c>
/// Hata → <c>ProblemDetails</c> (<c>status</c> + <c>extensions.code</c>).
/// </summary>
public static class ResultHttpExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error!.ToProblem();

    public static IActionResult ToProblem(this Error error)
    {
        var problem = new ProblemDetails { Status = error.Status, Title = error.Message };
        problem.Extensions["code"] = error.Code;
        return new ObjectResult(problem)
        {
            StatusCode = error.Status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
