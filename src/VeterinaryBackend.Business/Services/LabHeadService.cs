using FluentValidation;
using VeterinaryBackend.Business.DTOs.LabHead;
using VeterinaryBackend.Business.Mappers;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Entities;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class LabHeadService : ILabHeadService
{
    private readonly ILabHeadRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateLabHeadDto> _createValidator;
    private readonly IValidator<UpdateLabHeadDto> _updateValidator;

    public LabHeadService(
        ILabHeadRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateLabHeadDto> createValidator,
        IValidator<UpdateLabHeadDto> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<LabHeadDto>> GetAllAsync(bool onlyActive, CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(onlyActive, ct);
        return items.Select(x => x.ToDto()).ToList();
    }

    public async Task<LabHeadDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(LabHead), id);
        return entity.ToDto();
    }

    public async Task<LabHeadDto> CreateAsync(CreateLabHeadDto dto, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(dto, ct);
        if (!result.IsValid) throw BuildValidationError(result);

        var entity = dto.ToEntity();
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDto();
    }

    public async Task<LabHeadDto> UpdateAsync(int id, UpdateLabHeadDto dto, CancellationToken ct = default)
    {
        var result = await _updateValidator.ValidateAsync(dto, ct);
        if (!result.IsValid) throw BuildValidationError(result);

        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(LabHead), id);

        dto.ApplyTo(entity);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(LabHead), id);

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static ValidationAppException BuildValidationError(FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}
