using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTurn.Domain.Auth.Entities;

namespace NextTurn.Infrastructure.Persistence.Configurations.Auth;

public sealed class StaffOfficeAssignmentConfiguration : IEntityTypeConfiguration<StaffOfficeAssignment>
{
    public void Configure(EntityTypeBuilder<StaffOfficeAssignment> builder)
    {
        builder.ToTable("StaffOfficeAssignments");

        builder.HasKey(x => new { x.OrganisationId, x.StaffUserId, x.OfficeId });

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => new { x.OrganisationId, x.StaffUserId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NextTurn.Domain.Office.Entities.Office>()
            .WithMany()
            .HasForeignKey(x => new { x.OrganisationId, x.OfficeId })
            .HasPrincipalKey(x => new { x.OrganisationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganisationId, x.StaffUserId })
            .HasDatabaseName("IX_StaffOfficeAssignments_Org_Staff");
    }
}
