using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAttendance.Core.Models;

namespace StudentAttendance.Infrastructure.Data.Configurations;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.Property(p => p.RiskFlag)
            .HasConversion<string>();

        // ML.NET probabilities have 4 meaningful decimal places (e.g., 0.8734).
        // precision=5, scale=4 stores values from 0.0000 to 9.9999.
        // decimal(18,2) would silently truncate 0.8734 → 0.87, corrupting predictions.
        builder.Property(p => p.AbsenceProbability)
            .HasPrecision(5, 4);
    }
}
