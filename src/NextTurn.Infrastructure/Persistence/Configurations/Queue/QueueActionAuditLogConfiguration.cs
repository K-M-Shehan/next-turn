using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Domain.Queue.Enums;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;
using UserEntity = NextTurn.Domain.Auth.Entities.User;

namespace NextTurn.Infrastructure.Persistence.Configurations.Queue;

public sealed class QueueActionAuditLogConfiguration : IEntityTypeConfiguration<QueueActionAuditLog>
{
    public void Configure(EntityTypeBuilder<QueueActionAuditLog> builder)
    {
        builder.ToTable("QueueActionAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.QueueId)
            .IsRequired();

        builder.Property(x => x.QueueEntryId)
            .IsRequired();

        builder.Property(x => x.PerformedByUserId)
            .IsRequired();

        builder.Property(x => x.ActionType)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(x => x.Reason)
            .HasMaxLength(200);

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
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.QueueId, x.CreatedAt })
            .HasDatabaseName("IX_QueueActionAuditLogs_QueueId_CreatedAt");
    }
}