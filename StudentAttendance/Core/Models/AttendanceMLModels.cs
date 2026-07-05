using Microsoft.ML.Data;

namespace StudentAttendance.Core.Models;

/// <summary>
/// The input data structure fed into the ML.NET training pipeline.
/// </summary>
public class AttendanceData
{
    // The target we want to predict (e.g., Did they miss the 9th week?)
    [LoadColumn(0)]
    public bool WillBeAbsent { get; set; }

    // Features extracted from the last 8 weeks to predict the target
    [LoadColumn(1)]
    public float TotalAbsencesInLast8Weeks { get; set; }

    [LoadColumn(2)]
    public float TotalLatesInLast8Weeks { get; set; }

    [LoadColumn(3)]
    public float ConsecutiveAbsences { get; set; }

    [LoadColumn(4)]
    public float AbsentOnMondays { get; set; }
}

/// <summary>
/// The output prediction structure returned by ML.NET after evaluation.
/// </summary>
public class AttendancePrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedAbsence { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}
