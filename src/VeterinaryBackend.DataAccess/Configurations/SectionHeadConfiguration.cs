using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class SectionHeadConfiguration : IEntityTypeConfiguration<SectionHead>
{
    public void Configure(EntityTypeBuilder<SectionHead> builder)
    {
        builder.ToTable("section_heads");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.WorkingHours).HasMaxLength(200);
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.IsActive);

        // One section can have at most one head — enforced by a unique index on SectionId.
        // Cascade delete: removing a section also removes its head.
        builder.HasIndex(x => x.SectionId).IsUnique();
        builder.HasOne(x => x.Section)
            .WithMany()
            .HasForeignKey(x => x.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
