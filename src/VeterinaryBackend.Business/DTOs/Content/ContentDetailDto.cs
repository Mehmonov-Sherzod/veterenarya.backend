namespace VeterinaryBackend.Business.DTOs.Content;

public class ContentDetailDto
{
    public int Id { get; set; }
    public int? SectionId { get; set; }

    public string TitleUz { get; set; } = string.Empty;
    public string TitleRu { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;

    public string DescriptionUz { get; set; } = string.Empty;
    public string DescriptionRu { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePosition { get; set; } = "top";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
