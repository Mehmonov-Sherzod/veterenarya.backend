using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class LabHeadConfiguration : IEntityTypeConfiguration<LabHead>
{
    public void Configure(EntityTypeBuilder<LabHead> builder)
    {
        builder.ToTable("lab_heads");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ReceptionHours).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);
        builder.Property(x => x.Department).HasMaxLength(200);

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.IsActive);
    }
}
