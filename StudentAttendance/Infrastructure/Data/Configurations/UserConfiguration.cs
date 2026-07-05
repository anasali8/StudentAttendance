using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Role)
            .HasConversion<string>();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        // AssociatedId is optional — empty string for Admin users with no linked record.
        builder.Property(u => u.AssociatedId)
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(u => u.Username).IsUnique();
    }
}
