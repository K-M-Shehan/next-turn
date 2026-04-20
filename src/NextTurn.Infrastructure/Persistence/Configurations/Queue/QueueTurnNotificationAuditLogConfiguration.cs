using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueTurnNotificationAuditLog = NextTurn.Domain.Queue.Entities.QueueTurnNotificationAuditLog;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;
using UserEntity = NextTurn.Domain.Auth.Entities.User;

namespace NextTurn.Infrastructure.Persistence.Configurations.Queue;

public sealed class QueueTurnNotificationAuditLogConfiguration : IEntityTypeConfiguration<QueueTurnNotificationAuditLog>
{
    public void Configure(EntityTypeBuilder<QueueTurnNotificationAuditLog> builder)
    {
        builder.ToTable("QueueTurnNotificationAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.QueueId)
            .IsRequired();

        builder.Property(x => x.QueueEntryId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.PositionInQueue)
            .IsRequired();

        builder.Property(x => x.Threshold)
            .IsRequired();

        builder.Property(x => x.DeliveryStatus)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<QueueEntity>()
            .WithMany()
            .HasForeignKey(x => x.QueueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QueueEntry>()
            .WithMany()
            .HasForeignKey(x => x.QueueEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.QueueEntryId, x.UserId, x.DeliveryStatus })
            .HasDatabaseName("UX_QueueTurnNotificationAuditLogs_QueueEntry_User_Status")
            .IsUnique();

        builder.HasIndex(x => new { x.QueueId, x.CreatedAt })
            .HasDatabaseName("IX_QueueTurnNotificationAuditLogs_QueueId_CreatedAt");
    }
}
