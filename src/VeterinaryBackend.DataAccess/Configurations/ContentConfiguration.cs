using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.ToTable("contents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TitleUz).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleRu).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(500);

        // Unlimited length: PostgreSQL `text` — accepts plain text, HTML, JSON, anything.
        builder.Property(x => x.DescriptionUz).IsRequired().HasColumnType("text");
        builder.Property(x => x.DescriptionRu).IsRequired().HasColumnType("text");
        builder.Property(x => x.DescriptionEn).IsRequired().HasColumnType("text");

        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.ImagePosition).IsRequired().HasMaxLength(20).HasDefaultValue("top");

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.SectionId);
    }
}
