using FluentValidation;
using VeterinaryBackend.Business.DTOs.SectionHead;

namespace VeterinaryBackend.Business.Validators;

public class UpdateSectionHeadDtoValidator : AbstractValidator<UpdateSectionHeadDto>
{
    public UpdateSectionHeadDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email)
            .MaximumLength(200)
            .EmailAddress().WithMessage("Email format noto'g'ri.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.WorkingHours).MaximumLength(200);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.SectionId)
            .GreaterThan(0).WithMessage("Bo'lim tanlanishi shart.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
