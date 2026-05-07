using FluentValidation;
using VeterinaryBackend.Business.DTOs.LabHead;

namespace VeterinaryBackend.Business.Validators;

public class CreateLabHeadDtoValidator : AbstractValidator<CreateLabHeadDto>
{
    public CreateLabHeadDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email)
            .MaximumLength(200)
            .EmailAddress().WithMessage("Email format noto'g'ri.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.ReceptionHours).MaximumLength(200);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.Department).MaximumLength(200);
        RuleFor(x => x.SectionId)
            .GreaterThan(0).WithMessage("SectionId must be a positive section id.")
            .When(x => x.SectionId.HasValue);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
