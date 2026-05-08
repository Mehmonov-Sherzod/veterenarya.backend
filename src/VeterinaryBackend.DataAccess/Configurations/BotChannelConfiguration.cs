using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class BotChannelConfiguration : IEntityTypeConfiguration<BotChannel>
{
    public void Configure(EntityTypeBuilder<BotChannel> builder)
    {
        builder.ToTable("bot_channels");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChatId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256);
        builder.Property(x => x.Username).HasMaxLength(64);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ChatId).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
