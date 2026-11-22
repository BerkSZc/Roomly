using Roomly.Api.Common.Errors;

namespace Roomly.Api.Common.Results;

/// <summary>
/// Beklenen bir iş hatası. <see cref="Code"/> istemciye ve testlere giden sabit koddur
/// (ör. <c>INVITE_EXPIRED</c>), <see cref="Status"/> HTTP durum kodudur.
/// </summary>
public sealed record Error(string Code, string Message, int Status = StatusCodes.Status400BadRequest)
{
    public static Error NotFound(string message = "Kayıt bulunamadı.") =>
        new(ErrorCodes.NotFound, message, StatusCodes.Status404NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, StatusCodes.Status409Conflict);
}
