namespace VeterinaryBackend.Business.DTOs.LabHead;

public class UpdateLabHeadDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ReceptionHours { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
