using FluentValidation;
using VeterinaryBackend.Business.DTOs.Section;

namespace VeterinaryBackend.Business.Validators;

public class UpdateSectionDtoValidator : AbstractValidator<UpdateSectionDto>
{
    public UpdateSectionDtoValidator()
    {
        // UZ is the canonical language — required. RU and EN are optional translations.
        RuleFor(x => x.TitleUz).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TitleRu).MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);
        RuleFor(x => x.Slug)
            .MaximumLength(120)
            .Matches("^[a-z0-9-]*$").WithMessage("Slug only allows lowercase a-z, 0-9, hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ParentId)
            .GreaterThan(0).WithMessage("ParentId must be a positive section id.")
            .When(x => x.ParentId.HasValue);
    }
}
