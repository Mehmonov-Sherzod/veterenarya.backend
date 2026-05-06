using VeterinaryBackend.Business.DTOs.Section;
using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Business.Services;

public interface ISectionService
{
    Task<IReadOnlyList<SectionDto>> GetAllAsync(Language language, bool onlyActive, CancellationToken ct = default);
    Task<IReadOnlyList<SectionDto>> GetTreeAsync(Language language, bool onlyActive, CancellationToken ct = default);
    Task<IReadOnlyList<SectionWithContentsDto>> GetAllWithContentsAsync(Language language, bool onlyActive, CancellationToken ct = default);
    Task<IReadOnlyList<SectionWithContentsDto>> GetTreeWithContentsAsync(Language language, bool onlyActive, CancellationToken ct = default);
    Task<SectionDetailDto> GetDetailByIdAsync(int id, CancellationToken ct = default);
    Task<SectionDetailDto> CreateAsync(CreateSectionDto dto, CancellationToken ct = default);
    Task<SectionDetailDto> UpdateAsync(int id, UpdateSectionDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
