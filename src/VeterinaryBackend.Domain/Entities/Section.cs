using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Domain.Entities;

public class Section : BaseEntity
{
    public string Slug { get; set; } = string.Empty;

    public string TitleUz { get; set; } = string.Empty;
    public string TitleRu { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
