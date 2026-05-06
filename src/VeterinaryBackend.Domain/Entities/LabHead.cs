using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

public class LabHead : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ReceptionHours { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Department { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
