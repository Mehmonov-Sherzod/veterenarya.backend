namespace VeterinaryBackend.Business.DTOs.Content;

public class ContentDto
{
    public int Id { get; set; }
    public int? SectionId { get; set; }
    public string? SectionTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePosition { get; set; } = "top";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
