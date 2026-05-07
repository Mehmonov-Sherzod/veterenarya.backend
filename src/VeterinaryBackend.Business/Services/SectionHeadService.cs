using FluentValidation;
using VeterinaryBackend.Business.DTOs.SectionHead;
using VeterinaryBackend.Business.Mappers;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class SectionHeadService : ISectionHeadService
{
    private readonly ISectionHeadRepository _repository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSectionHeadDto> _createValidator;
    private readonly IValidator<UpdateSectionHeadDto> _updateValidator;

    public SectionHeadService(
        ISectionHeadRepository repository,
        ISectionRepository sectionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateSectionHeadDto> createValidator,
        IValidator<UpdateSectionHeadDto> updateValidator)
    {
        _repository = repository;
        _sectionRepository = sectionRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<SectionHeadDto>> GetAllAsync(Language language, bool onlyActive, CancellationToken ct = default)
    {
        var items = await _repository.GetAllAsync(onlyActive, ct);
        return items.Select(x => x.ToDto(language)).ToList();
    }

    public async Task<SectionHeadDto?> GetBySectionAsync(int sectionId, Language language, bool onlyActive, CancellationToken ct = default)
    {
        var entity = await _repository.GetBySectionAsync(sectionId, onlyActive, ct);
        return entity?.ToDto(language);
    }

    public async Task<SectionHeadDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithSectionAsync(id, ct)
            ?? throw new NotFoundException(nameof(SectionHead), id);
        return entity.ToDto();
    }

    public async Task<SectionHeadDto> CreateAsync(CreateSectionHeadDto dto, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(dto, ct);
        if (!result.IsValid) throw BuildValidationError(result);

        await EnsureSectionExistsAsync(dto.SectionId, ct);
        await EnsureNoExistingHeadAsync(dto.SectionId, currentId: null, ct);

        var entity = dto.ToEntity();
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var fresh = await _repository.GetByIdWithSectionAsync(entity.Id, ct) ?? entity;
        return fresh.ToDto();
    }

    public async Task<SectionHeadDto> UpdateAsync(int id, UpdateSectionHeadDto dto, CancellationToken ct = default)
    {
        var result = await _updateValidator.ValidateAsync(dto, ct);
        if (!result.IsValid) throw BuildValidationError(result);

        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(SectionHead), id);

        await EnsureSectionExistsAsync(dto.SectionId, ct);
        if (entity.SectionId != dto.SectionId)
            await EnsureNoExistingHeadAsync(dto.SectionId, currentId: id, ct);

        dto.ApplyTo(entity);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        var fresh = await _repository.GetByIdWithSectionAsync(entity.Id, ct) ?? entity;
        return fresh.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(SectionHead), id);

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EnsureSectionExistsAsync(int sectionId, CancellationToken ct)
    {
        if (!await _sectionRepository.ExistsAsync(sectionId, ct))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["SectionId"] = new[] { $"Section #{sectionId} not found." }
            });
    }

    private async Task EnsureNoExistingHeadAsync(int sectionId, int? currentId, CancellationToken ct)
    {
        var existing = await _repository.GetBySectionAsync(sectionId, onlyActive: false, ct);
        if (existing is not null && existing.Id != currentId)
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["SectionId"] = new[] { "Bu bo'limga avvaldan boshliq biriktirilgan." }
            });
    }

    private static ValidationAppException BuildValidationError(FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}
