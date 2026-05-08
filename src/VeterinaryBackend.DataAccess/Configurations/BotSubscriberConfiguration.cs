using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Configurations;

public class BotSubscriberConfiguration : IEntityTypeConfiguration<BotSubscriber>
{
    public void Configure(EntityTypeBuilder<BotSubscriber> builder)
    {
        builder.ToTable("bot_subscribers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChatId).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(64);
        builder.Property(x => x.FirstName).HasMaxLength(128);
        builder.Property(x => x.LastName).HasMaxLength(128);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.ChatId).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
