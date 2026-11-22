using FluentValidation;
using Roomly.Api.Features.Auth.Dtos;

namespace Roomly.Api.Features.Auth.Validators;

/// <summary>Kurallar spec 0004 AC-2; mobil istemci aynı kuralları uygular.</summary>
public static class AuthRules
{
    public const int DisplayNameMin = 2;
    public const int DisplayNameMax = AppUser.DisplayNameMaxLength;
    public const int EmailMax = 256;
    public const int PasswordMin = 8;
    public const int PasswordMax = 128;
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Ad zorunludur.")
            .Must(n => n!.Trim().Length is >= AuthRules.DisplayNameMin and <= AuthRules.DisplayNameMax)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName))
            .WithMessage($"Ad {AuthRules.DisplayNameMin} ile {AuthRules.DisplayNameMax} karakter olmalıdır.");

        RuleFor(x => x.Email).SetValidator(new EmailRule());

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .Length(AuthRules.PasswordMin, AuthRules.PasswordMax)
            .WithMessage($"Şifre {AuthRules.PasswordMin} ile {AuthRules.PasswordMax} karakter olmalıdır.")
            .Must(p => p!.Any(char.IsLetter)).WithMessage("Şifre en az bir harf içermelidir.")
            .Must(p => p!.Any(char.IsDigit)).WithMessage("Şifre en az bir rakam içermelidir.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).SetValidator(new EmailRule());
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre zorunludur.");
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token zorunludur.");
    }
}

internal sealed class EmailRule : AbstractValidator<string?>
{
    public EmailRule()
    {
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(e => !string.IsNullOrWhiteSpace(e)).WithMessage("E-posta zorunludur.")
            .Must(e => e!.Trim().Length <= AuthRules.EmailMax).WithMessage("E-posta en fazla 256 karakter olabilir.")
            .Must(e => EmailFormat.IsValid(e!.Trim())).WithMessage("Geçerli bir e-posta adresi girin.");
    }
}

internal static class EmailFormat
{
    private static readonly System.Text.RegularExpressions.Regex Pattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static bool IsValid(string email) => Pattern.IsMatch(email);
}
