using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("sections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(120);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(x => x.TitleUz).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TitleRu).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(200);

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.IsActive);

        builder.HasMany(x => x.Contents)
            .WithOne(c => c.Section)
            .HasForeignKey(c => c.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
