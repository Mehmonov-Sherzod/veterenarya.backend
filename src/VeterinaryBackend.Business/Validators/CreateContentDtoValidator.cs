using FluentValidation;
using VeterinaryBackend.Business.DTOs.Content;

namespace VeterinaryBackend.Business.Validators;

public class CreateContentDtoValidator : AbstractValidator<CreateContentDto>
{
    public CreateContentDtoValidator()
    {
        RuleFor(x => x.SectionId)
            .NotNull().WithMessage("SectionId is required.")
            .GreaterThan(0).WithMessage("SectionId must be a positive integer.");

        // UZ is the canonical language — required. RU and EN are optional translations.
        RuleFor(x => x.TitleUz).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleRu).MaximumLength(500);
        RuleFor(x => x.TitleEn).MaximumLength(500);

        RuleFor(x => x.DescriptionUz).NotEmpty();
        // DescriptionRu / DescriptionEn: optional, no length limit (PostgreSQL text).

        // ImageUrl: optional cover image (auto-derived from first image block in admin UI).
        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000);

        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
