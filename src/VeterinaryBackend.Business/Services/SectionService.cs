using FluentValidation;
using VeterinaryBackend.Business.DTOs.Section;
using VeterinaryBackend.Business.Helpers;
using VeterinaryBackend.Business.Mappers;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class SectionService : ISectionService
{
    private readonly ISectionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSectionDto> _createValidator;
    private readonly IValidator<UpdateSectionDto> _updateValidator;

    public SectionService(
        ISectionRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateSectionDto> createValidator,
        IValidator<UpdateSectionDto> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<SectionDto>> GetAllAsync(Language language, bool onlyActive, CancellationToken ct = default)
    {
        var sections = await _repository.GetAllWithContentsAsync(onlyActive, ct);
        return sections.Select(s => s.ToLocalizedDto(language)).ToList();
    }

    public async Task<IReadOnlyList<SectionWithContentsDto>> GetAllWithContentsAsync(Language language, bool onlyActive, CancellationToken ct = default)
    {
        var sections = await _repository.GetAllWithContentsAsync(onlyActive, ct);
        return sections.Select(s => s.ToWithContentsDto(language)).ToList();
    }

    public async Task<SectionDetailDto> GetDetailByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);
        return entity.ToDetailDto();
    }

    public async Task<SectionDetailDto> CreateAsync(CreateSectionDto dto, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(dto, ct);
        if (!result.IsValid) throw BuildValidationError(result);

        var slug = await ResolveSlugAsync(dto.Slug, dto.TitleEn, dto.TitleUz, excludeId: null, ct);
        var entity = dto.ToEntity(slug);

        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDetailDto();
    }

    public async Task<SectionDetailDto> UpdateAsync(int id, UpdateSectionDto dto, CancellationToken ct = default)
    {
        var result = await _updateValidator.ValidateAsync(dto, ct);
        if (!result.IsValid) throw BuildValidationError(result);

        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);

        var slug = await ResolveSlugAsync(dto.Slug, dto.TitleEn, dto.TitleUz, excludeId: id, ct);
        dto.ApplyTo(entity, slug);

        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDetailDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Section), id);

        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<string> ResolveSlugAsync(string? requested, string fallbackEn, string fallbackUz, int? excludeId, CancellationToken ct)
    {
        var seed = !string.IsNullOrWhiteSpace(requested)
            ? requested
            : (!string.IsNullOrWhiteSpace(fallbackEn) ? fallbackEn : fallbackUz);

        var baseSlug = SlugHelper.Generate(seed);
        if (string.IsNullOrEmpty(baseSlug))
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Slug"] = new[] { "Slug could not be generated. Provide TitleEn or TitleUz." }
            });

        var slug = baseSlug;
        var n = 2;
        while (await _repository.SlugExistsAsync(slug, excludeId, ct))
        {
            slug = $"{baseSlug}-{n++}";
            if (n > 1000) break;
        }
        return slug;
    }

    private static ValidationAppException BuildValidationError(FluentValidation.Results.ValidationResult result)
        => new(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}
