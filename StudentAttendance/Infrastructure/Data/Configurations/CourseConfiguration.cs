using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasMany(c => c.Students)
            .WithMany(s => s.Courses)
            .UsingEntity(j => j.ToTable("CourseStudents"));

        builder.HasOne(c => c.Teacher)
            .WithMany()
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.CourseCode).IsUnique();
    }
}
