using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentNotificationAuditLog = NextTurn.Domain.Appointment.Entities.AppointmentNotificationAuditLog;
using UserEntity = NextTurn.Domain.Auth.Entities.User;

namespace NextTurn.Infrastructure.Persistence.Configurations.Appointment;

public sealed class AppointmentNotificationAuditLogConfiguration : IEntityTypeConfiguration<AppointmentNotificationAuditLog>
{
    public void Configure(EntityTypeBuilder<AppointmentNotificationAuditLog> builder)
    {
        builder.ToTable("AppointmentNotificationAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.AppointmentId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.NotificationType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.DeliveryStatus)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.RecipientEmail)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(x => x.SlotStart)
            .IsRequired();

        builder.Property(x => x.SlotEnd)
            .IsRequired();

        builder.Property(x => x.OfficeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ServiceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<AppointmentEntity>()
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AppointmentId, x.NotificationType, x.DeliveryStatus })
            .HasDatabaseName("IX_AppointmentNotificationAuditLogs_Appointment_Type_Status");

        builder.HasIndex(x => new { x.OrganisationId, x.CreatedAt })
            .HasDatabaseName("IX_AppointmentNotificationAuditLogs_Organisation_CreatedAt");
    }
}
