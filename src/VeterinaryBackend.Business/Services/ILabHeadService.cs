using VeterinaryBackend.Business.DTOs.LabHead;
using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Business.Services;

public interface ILabHeadService
{
    Task<IReadOnlyList<LabHeadDto>> GetAllAsync(Language language, bool onlyActive, CancellationToken ct = default);
    Task<IReadOnlyList<LabHeadDto>> GetBySectionAsync(int? sectionId, Language language, bool onlyActive, CancellationToken ct = default);
    Task<LabHeadDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<LabHeadDto> CreateAsync(CreateLabHeadDto dto, CancellationToken ct = default);
    Task<LabHeadDto> UpdateAsync(int id, UpdateLabHeadDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
