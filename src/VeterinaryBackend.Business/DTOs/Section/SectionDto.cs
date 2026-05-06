namespace VeterinaryBackend.Business.DTOs.Section;

public class SectionDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ContentCount { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Populated only by tree endpoints. Flat-list endpoints leave this empty —
    /// use <see cref="ParentId"/> with the flat list to reconstruct the hierarchy.
    /// </summary>
    public IReadOnlyList<SectionDto> Children { get; set; } = new List<SectionDto>();
}
