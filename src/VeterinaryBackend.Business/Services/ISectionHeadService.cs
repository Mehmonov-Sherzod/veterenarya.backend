using VeterinaryBackend.Business.DTOs.SectionHead;
using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Business.Services;

public interface ISectionHeadService
{
    Task<IReadOnlyList<SectionHeadDto>> GetAllAsync(Language language, bool onlyActive, CancellationToken ct = default);
    Task<SectionHeadDto?> GetBySectionAsync(int sectionId, Language language, bool onlyActive, CancellationToken ct = default);
    Task<SectionHeadDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SectionHeadDto> CreateAsync(CreateSectionHeadDto dto, CancellationToken ct = default);
    Task<SectionHeadDto> UpdateAsync(int id, UpdateSectionHeadDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
