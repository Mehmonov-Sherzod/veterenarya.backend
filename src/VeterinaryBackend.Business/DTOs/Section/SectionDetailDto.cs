namespace VeterinaryBackend.Business.DTOs.Section;

public class SectionDetailDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleUz { get; set; } = string.Empty;
    public string TitleRu { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
