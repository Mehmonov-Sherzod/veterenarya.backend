namespace VeterinaryBackend.Business.DTOs.SectionHead;

public class UpdateSectionHeadDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WorkingHours { get; set; }
    public string? PhotoUrl { get; set; }
    public int SectionId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
