using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class BotMessageConfiguration : IEntityTypeConfiguration<BotMessage>
{
    public void Configure(EntityTypeBuilder<BotMessage> builder)
    {
        builder.ToTable("bot_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChatId).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(64);
        builder.Property(x => x.FirstName).HasMaxLength(128);
        builder.Property(x => x.ContactName).HasMaxLength(128);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Text).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.IsRead).HasDefaultValue(false);
        builder.Property(x => x.Status).HasDefaultValue(Domain.Entities.BotMessageStatus.Pending).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ChatId);
    }
}
