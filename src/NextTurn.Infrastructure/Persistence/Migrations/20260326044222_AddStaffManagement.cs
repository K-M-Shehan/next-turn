using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CounterName",
                table: "Users",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ShiftEnd",
                table: "Users",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ShiftStart",
                table: "Users",
                type: "time",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_TenantId_Id",
                table: "Users",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "StaffOfficeAssignments",
                columns: table => new
                {
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffOfficeAssignments", x => new { x.OrganisationId, x.StaffUserId, x.OfficeId });
                    table.ForeignKey(
                        name: "FK_StaffOfficeAssignments_Offices_OrganisationId_OfficeId",
                        columns: x => new { x.OrganisationId, x.OfficeId },
                        principalTable: "Offices",
                        principalColumns: new[] { "OrganisationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffOfficeAssignments_Users_OrganisationId_StaffUserId",
                        columns: x => new { x.OrganisationId, x.StaffUserId },
                        principalTable: "Users",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffOfficeAssignments_Org_Staff",
                table: "StaffOfficeAssignments",
                columns: new[] { "OrganisationId", "StaffUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffOfficeAssignments_OrganisationId_OfficeId",
                table: "StaffOfficeAssignments",
                columns: new[] { "OrganisationId", "OfficeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffOfficeAssignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_TenantId_Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CounterName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShiftEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShiftStart",
                table: "Users");
        }
    }
}
