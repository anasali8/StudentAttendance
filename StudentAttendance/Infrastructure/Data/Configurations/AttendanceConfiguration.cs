using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.LectureSession)
            .WithMany(ls => ls.Attendances)
            .HasForeignKey(a => a.LectureSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.LatenessClassification)
            .HasConversion<string>();

        builder.HasIndex(a => new { a.LectureSessionId, a.StudentId }).IsUnique();
    }
}
