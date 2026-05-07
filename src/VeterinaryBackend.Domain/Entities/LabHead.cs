using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

public class LabHead : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ReceptionHours { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Department { get; set; }

    /// <summary>
    /// Optional link to the section this head leads. When set, the public section page
    /// renders this head as the section's "Bo'lim boshlig'i" card. Null means the head
    /// is shown only on the global /lab-heads listing.
    /// </summary>
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
