using FluentValidation;
using Roomly.Api.Features.Groups.Dtos;

namespace Roomly.Api.Features.Groups.Validators;

/// <summary>Yalnızca boşluk kontrolü; biçim ve geçerlilik servisin işi (INVITE_INVALID).</summary>
public sealed class JoinGroupRequestValidator : AbstractValidator<JoinGroupRequest>
{
    public JoinGroupRequestValidator()
    {
        RuleFor(x => x.Code)
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Davet kodu zorunludur.")
            .MaximumLength(100).WithMessage("Davet kodu çok uzun.");
    }
}
