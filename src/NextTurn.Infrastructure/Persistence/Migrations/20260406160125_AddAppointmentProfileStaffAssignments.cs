using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentProfileStaffAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentProfileStaffAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProfileStaffAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProfileStaffAssignments_OrganisationId",
                table: "AppointmentProfileStaffAssignments",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "UX_AppointmentProfileStaffAssignments_ProfileId_StaffUserId",
                table: "AppointmentProfileStaffAssignments",
                columns: new[] { "AppointmentProfileId", "StaffUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentProfileStaffAssignments");
        }
    }
}
