using Microsoft.EntityFrameworkCore;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentNotificationAuditLog = NextTurn.Domain.Appointment.Entities.AppointmentNotificationAuditLog;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;
using NextTurn.Domain.Auth.Entities;
using StaffOfficeAssignment = NextTurn.Domain.Auth.Entities.StaffOfficeAssignment;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;
using QueueEntity        = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry         = NextTurn.Domain.Queue.Entities.QueueEntry;
using QueueActionAuditLog = NextTurn.Domain.Queue.Entities.QueueActionAuditLog;
using QueueTurnNotificationAuditLog = NextTurn.Domain.Queue.Entities.QueueTurnNotificationAuditLog;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;
using ServiceOfficeAssignment = NextTurn.Domain.Service.Entities.ServiceOfficeAssignment;

namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ApplicationDbContext exposed to the Application layer.
/// Application handlers depend on this interface, never on the concrete DbContext.
/// This keeps the Application layer free of direct EF Core coupling.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User>             Users        { get; }
    DbSet<OrganisationEntity> Organisations { get; }
    DbSet<OfficeEntity> Offices { get; }
    DbSet<QueueEntity>      Queues       { get; }
    DbSet<QueueEntry>       QueueEntries { get; }
    DbSet<QueueActionAuditLog> QueueActionAuditLogs { get; }
    DbSet<QueueTurnNotificationAuditLog> QueueTurnNotificationAuditLogs { get; }
    DbSet<AppointmentEntity> Appointments { get; }
    DbSet<AppointmentNotificationAuditLog> AppointmentNotificationAuditLogs { get; }
    DbSet<AppointmentProfile> AppointmentProfiles { get; }
    DbSet<AppointmentScheduleRule> AppointmentScheduleRules { get; }
    DbSet<StaffOfficeAssignment> StaffOfficeAssignments { get; }
    DbSet<ServiceEntity> Services { get; }
    DbSet<ServiceOfficeAssignment> ServiceOfficeAssignments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
