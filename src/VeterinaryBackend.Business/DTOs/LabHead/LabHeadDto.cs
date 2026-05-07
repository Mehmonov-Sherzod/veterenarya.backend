namespace VeterinaryBackend.Business.DTOs.LabHead;

public class LabHeadDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ReceptionHours { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Department { get; set; }

    /// <summary>Optional id of the section this head leads. Null = unassigned (only on /lab-heads page).</summary>
    public int? SectionId { get; set; }

    /// <summary>Localized title of the linked section, included for convenience so clients don't double-fetch.</summary>
    public string? SectionTitle { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
