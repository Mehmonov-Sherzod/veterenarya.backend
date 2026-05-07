using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

/// <summary>
/// "Bo'lim raxbari" — leader of a specific dynamic section. Distinct from
/// <see cref="LabHead"/> which represents top-level Rahbariyat (general
/// leadership). Section heads always belong to one section and surface on
/// that section's public page as a centered hero card.
/// </summary>
public class SectionHead : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WorkingHours { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>Required link to the section this head leads. Cascade-delete when the section is removed.</summary>
    public int SectionId { get; set; }
    public Section? Section { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
