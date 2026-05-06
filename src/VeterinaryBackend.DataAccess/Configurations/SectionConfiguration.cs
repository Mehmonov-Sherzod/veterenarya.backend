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
        builder.HasIndex(x => x.ParentId);

        builder.HasMany(x => x.Contents)
            .WithOne(c => c.Section)
            .HasForeignKey(c => c.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-reference: a section may belong to a parent section. Restrict deletion so a parent
        // with active children cannot be silently removed — the service layer surfaces a clear error.
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
