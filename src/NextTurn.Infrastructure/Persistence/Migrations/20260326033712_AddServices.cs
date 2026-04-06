using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextTurn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Offices_OrganisationId_Id",
                table: "Offices",
                columns: new[] { "OrganisationId", "Id" });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.UniqueConstraint("AK_Services_OrganisationId_Id", x => new { x.OrganisationId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "ServiceOfficeAssignments",
                columns: table => new
                {
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOfficeAssignments", x => new { x.OrganisationId, x.ServiceId, x.OfficeId });
                    table.ForeignKey(
                        name: "FK_ServiceOfficeAssignments_Offices_OrganisationId_OfficeId",
                        columns: x => new { x.OrganisationId, x.OfficeId },
                        principalTable: "Offices",
                        principalColumns: new[] { "OrganisationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceOfficeAssignments_Services_OrganisationId_ServiceId",
                        columns: x => new { x.OrganisationId, x.ServiceId },
                        principalTable: "Services",
                        principalColumns: new[] { "OrganisationId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfficeAssignments_OrganisationId_OfficeId",
                table: "ServiceOfficeAssignments",
                columns: new[] { "OrganisationId", "OfficeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_OrganisationId_Code",
                table: "Services",
                columns: new[] { "OrganisationId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceOfficeAssignments");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Offices_OrganisationId_Id",
                table: "Offices");
        }
    }
}
