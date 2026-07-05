using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class LectureSessionConfiguration : IEntityTypeConfiguration<LectureSession>
{
    public void Configure(EntityTypeBuilder<LectureSession> builder)
    {
        builder.HasOne(ls => ls.Course)
            .WithMany(c => c.LectureSessions)
            .HasForeignKey(ls => ls.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
