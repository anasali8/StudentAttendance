using Microsoft.EntityFrameworkCore;
using StudentAttendance.Core.Models;
using System.Reflection;

namespace StudentAttendance.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Fingerprint> Fingerprints { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<LectureSession> LectureSessions { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Prediction> Predictions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all IEntityTypeConfiguration classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
