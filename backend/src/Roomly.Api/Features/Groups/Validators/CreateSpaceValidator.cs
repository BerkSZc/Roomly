using FluentValidation;
using Roomly.Api.Features.Groups.Dtos;

namespace Roomly.Api.Features.Groups.Validators;

public sealed class CreateSpaceValidator : AbstractValidator<CreateSpaceRequest>
{
    public CreateSpaceValidator()
    {
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)
            && name.Trim().Length <= Group.NameMaxLength)
            .WithMessage($"Alan adı 1–{Group.NameMaxLength} karakter olmalı.");
        RuleFor(x => x.Type).NotNull().IsInEnum().WithMessage("Ev veya Yurt seçmelisin.");
        RuleFor(x => x.Capacity)
            .Must((request, capacity) => GroupCapacityRules.IsValid(request.Type!.Value, capacity))
            .WithMessage(request =>
            {
                var range = GroupCapacityRules.RangeFor(request.Type!.Value);
                return $"Kapasite {range.Min}–{range.Max} kişi olmalı.";
            })
            .When(x => x.Type is { } type && Enum.IsDefined(type));
    }
}
