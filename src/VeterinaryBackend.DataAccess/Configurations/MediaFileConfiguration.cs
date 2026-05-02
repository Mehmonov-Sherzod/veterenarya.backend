using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.StoredFileName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RelativePath).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Extension).HasMaxLength(20);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ContentType);
    }
}
