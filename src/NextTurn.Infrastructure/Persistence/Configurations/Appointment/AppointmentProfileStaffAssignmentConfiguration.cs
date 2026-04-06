using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppointmentProfileStaffAssignment = NextTurn.Domain.Appointment.Entities.AppointmentProfileStaffAssignment;

namespace NextTurn.Infrastructure.Persistence.Configurations.Appointment;

public sealed class AppointmentProfileStaffAssignmentConfiguration : IEntityTypeConfiguration<AppointmentProfileStaffAssignment>
{
    public void Configure(EntityTypeBuilder<AppointmentProfileStaffAssignment> builder)
    {
        builder.ToTable("AppointmentProfileStaffAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.OrganisationId)
            .IsRequired();

        builder.Property(a => a.AppointmentProfileId)
            .IsRequired();

        builder.Property(a => a.StaffUserId)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasIndex(a => a.OrganisationId)
            .HasDatabaseName("IX_AppointmentProfileStaffAssignments_OrganisationId");

        builder.HasIndex(a => new { a.AppointmentProfileId, a.StaffUserId })
            .IsUnique()
            .HasDatabaseName("UX_AppointmentProfileStaffAssignments_ProfileId_StaffUserId");
    }
}