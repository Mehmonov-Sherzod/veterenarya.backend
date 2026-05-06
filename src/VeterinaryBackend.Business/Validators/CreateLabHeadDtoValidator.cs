using FluentValidation;
using VeterinaryBackend.Business.DTOs.LabHead;

namespace VeterinaryBackend.Business.Validators;

public class CreateLabHeadDtoValidator : AbstractValidator<CreateLabHeadDto>
{
    public CreateLabHeadDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReceptionHours).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.Department).MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
