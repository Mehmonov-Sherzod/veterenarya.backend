using VeterinaryBackend.Business.DTOs.Content;

namespace VeterinaryBackend.Business.DTOs.Section;

public class SectionWithContentsDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public IReadOnlyList<ContentDto> Items { get; set; } = new List<ContentDto>();

    /// <summary>
    /// Populated only by tree endpoints. Flat-list endpoints leave this empty.
    /// </summary>
    public IReadOnlyList<SectionWithContentsDto> Children { get; set; } = new List<SectionWithContentsDto>();
}
