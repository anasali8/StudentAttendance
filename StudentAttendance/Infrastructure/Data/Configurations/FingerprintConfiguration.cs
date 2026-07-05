using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class FingerprintConfiguration : IEntityTypeConfiguration<Fingerprint>
{
    public void Configure(EntityTypeBuilder<Fingerprint> builder)
    {
        builder.HasKey(f => f.StudentId);
    }
}
