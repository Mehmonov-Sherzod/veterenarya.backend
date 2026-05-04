using VeterinaryBackend.Business.DTOs.LabHead;

namespace VeterinaryBackend.Business.Services;

public interface ILabHeadService
{
    Task<IReadOnlyList<LabHeadDto>> GetAllAsync(bool onlyActive, CancellationToken ct = default);
    Task<LabHeadDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<LabHeadDto> CreateAsync(CreateLabHeadDto dto, CancellationToken ct = default);
    Task<LabHeadDto> UpdateAsync(int id, UpdateLabHeadDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
