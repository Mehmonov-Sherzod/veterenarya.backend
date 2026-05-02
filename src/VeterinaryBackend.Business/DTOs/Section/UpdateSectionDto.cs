namespace VeterinaryBackend.Business.DTOs.Section;

public class UpdateSectionDto
{
    public string? Slug { get; set; }
    public string TitleUz { get; set; } = string.Empty;
    public string TitleRu { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
