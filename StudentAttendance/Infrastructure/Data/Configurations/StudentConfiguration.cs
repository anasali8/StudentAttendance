using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasOne(s => s.Fingerprint)
            .WithOne(f => f.Student)
            .HasForeignKey<Fingerprint>(f => f.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ExternalId must be unique — no two students can share the same ZKTeco EnrollNumber
        builder.Property(s => s.ExternalId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.ExternalId).IsUnique();
    }
}
