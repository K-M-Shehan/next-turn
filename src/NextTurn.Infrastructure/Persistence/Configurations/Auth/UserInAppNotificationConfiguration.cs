using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTurn.Domain.Auth.Entities;

namespace NextTurn.Infrastructure.Persistence.Configurations.Auth;

public sealed class UserInAppNotificationConfiguration : IEntityTypeConfiguration<UserInAppNotification>
{
    public void Configure(EntityTypeBuilder<UserInAppNotification> builder)
    {
        builder.ToTable("UserInAppNotifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.NotificationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ReadAt);

        builder.Property(x => x.QueueId);

        builder.Property(x => x.QueueEntryId);

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_UserInAppNotifications_UserId_IsRead_CreatedAt");

        builder.HasIndex(x => new { x.UserId, x.NotificationType, x.QueueEntryId, x.IsRead })
            .HasDatabaseName("IX_UserInAppNotifications_Dedupe");
    }
}
