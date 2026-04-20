using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithRecentChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AppointmentBookedNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AppointmentCancelledNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AppointmentRescheduledNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "QueueTurnApproachingNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "QueueNotificationThreshold",
                table: "Organisations",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "AppointmentNotificationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    SlotStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SlotEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OfficeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNotificationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentNotificationAuditLogs_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentNotificationAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QueueTurnNotificationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionInQueue = table.Column<int>(type: "int", nullable: false),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueTurnNotificationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueTurnNotificationAuditLogs_QueueEntries_QueueEntryId",
                        column: x => x.QueueEntryId,
                        principalTable: "QueueEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueueTurnNotificationAuditLogs_Queues_QueueId",
                        column: x => x.QueueId,
                        principalTable: "Queues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QueueTurnNotificationAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotificationAuditLogs_Appointment_Type_Status",
                table: "AppointmentNotificationAuditLogs",
                columns: new[] { "AppointmentId", "NotificationType", "DeliveryStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotificationAuditLogs_Organisation_CreatedAt",
                table: "AppointmentNotificationAuditLogs",
                columns: new[] { "OrganisationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotificationAuditLogs_UserId",
                table: "AppointmentNotificationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueTurnNotificationAuditLogs_QueueId_CreatedAt",
                table: "QueueTurnNotificationAuditLogs",
                columns: new[] { "QueueId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueTurnNotificationAuditLogs_UserId",
                table: "QueueTurnNotificationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_QueueTurnNotificationAuditLogs_QueueEntry_User_Status",
                table: "QueueTurnNotificationAuditLogs",
                columns: new[] { "QueueEntryId", "UserId", "DeliveryStatus" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentNotificationAuditLogs");

            migrationBuilder.DropTable(
                name: "QueueTurnNotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "AppointmentBookedNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AppointmentCancelledNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AppointmentRescheduledNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "QueueTurnApproachingNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "QueueNotificationThreshold",
                table: "Organisations");
        }
    }
}
