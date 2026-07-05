using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using StudentAttendance.Core.Models;
using StudentAttendance.Core.Enums;
using StudentAttendance.Infrastructure.Data;

namespace StudentAttendance.Infrastructure.Services;

public class PredictionHostedService : BackgroundService
{
    private readonly ILogger<PredictionHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLContext _mlContext;

    public PredictionHostedService(ILogger<PredictionHostedService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mlContext = new MLContext(seed: 0); // Seed for deterministic training results
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Prediction AI Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPredictionPipelineAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the ML.NET prediction pipeline execution.");
            }

            // Runs weekly as specified (e.g., Sunday 2 AM)
            _logger.LogInformation("Prediction pipeline completed. Sleeping until next week.");
            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }

    private async Task RunPredictionPipelineAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-56); // Historical 8 weeks

        _logger.LogInformation($"Extracting attendance data from {cutoffDate:yyyy-MM-dd} to today...");

        // 1. Gather Raw Data & Extract Features
        var rawData = await dbContext.Attendances
            .Include(a => a.LectureSession)
            .Where(a => a.LectureSession.ScheduledStart >= cutoffDate)
            .ToListAsync(cancellationToken);

        var allStudents = await dbContext.Students.Include(s => s.Courses).ToListAsync(cancellationToken);
        var trainingData = new List<AttendanceData>();
        var predictionTargets = new List<(int StudentId, int CourseId, AttendanceData Features)>();

        foreach (var student in allStudents)
        {
            foreach (var course in student.Courses)
            {
                var studentCourseAttendances = rawData
                    .Where(a => a.StudentId == student.Id && a.LectureSession.CourseId == course.Id)
                    .OrderBy(a => a.LectureSession.ScheduledStart)
                    .ToList();

                if (!studentCourseAttendances.Any()) continue;

                // Feature Extraction
                float totalAbsences = studentCourseAttendances.Count(a => a.LatenessClassification == LatenessClassification.Absent);
                float totalLates = studentCourseAttendances.Count(a => a.LatenessClassification == LatenessClassification.LateTier1 || 
                                                                       a.LatenessClassification == LatenessClassification.LateTier2 || 
                                                                       a.LatenessClassification == LatenessClassification.LateTier3);
                
                float maxConsecutiveAbsences = 0;
                float currentConsecutive = 0;
                foreach (var att in studentCourseAttendances)
                {
                    if (att.LatenessClassification == LatenessClassification.Absent) currentConsecutive++;
                    else currentConsecutive = 0;
                    
                    if (currentConsecutive > maxConsecutiveAbsences) maxConsecutiveAbsences = currentConsecutive;
                }

                float absentOnMondays = studentCourseAttendances
                    .Count(a => a.LectureSession.ScheduledStart.DayOfWeek == DayOfWeek.Monday && 
                                a.LatenessClassification == LatenessClassification.Absent);

                var features = new AttendanceData
                {
                    TotalAbsencesInLast8Weeks = totalAbsences,
                    TotalLatesInLast8Weeks = totalLates,
                    ConsecutiveAbsences = maxConsecutiveAbsences,
                    AbsentOnMondays = absentOnMondays,
                    // In a true dynamic model, we'd use weeks 1-7 to predict week 8. For simulation:
                    WillBeAbsent = totalAbsences > 3 
                };

                trainingData.Add(features);
                predictionTargets.Add((student.Id, course.Id, features));
            }
        }

        if (!trainingData.Any()) return;

        // 2. Train the ML.NET Model
        _logger.LogInformation("Training ML.NET Binary Classification model...");
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(AttendanceData.TotalAbsencesInLast8Weeks), 
                nameof(AttendanceData.TotalLatesInLast8Weeks), 
                nameof(AttendanceData.ConsecutiveAbsences),
                nameof(AttendanceData.AbsentOnMondays))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(AttendanceData.WillBeAbsent), featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);

        // 3. Make Predictions and Update Database (Predictions Table)
        _logger.LogInformation("Evaluating current students against the trained model...");
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<AttendanceData, AttendancePrediction>(model);

        foreach (var target in predictionTargets)
        {
            var prediction = predictionEngine.Predict(target.Features);
            
            // Apply Threshold for RiskFlag
            RiskFlag riskFlag = RiskFlag.Low;
            if (prediction.Probability > 0.70f) riskFlag = RiskFlag.High;
            else if (prediction.Probability > 0.40f) riskFlag = RiskFlag.Medium;

            var existingPrediction = await dbContext.Predictions
                .FirstOrDefaultAsync(p => p.StudentId == target.StudentId && p.CourseId == target.CourseId, cancellationToken);

            if (existingPrediction != null)
            {
                existingPrediction.AbsenceProbability = (decimal)prediction.Probability;
                existingPrediction.RiskFlag = riskFlag;
                existingPrediction.NextLectureDate = DateTime.UtcNow.AddDays(7);
            }
            else
            {
                dbContext.Predictions.Add(new Prediction
                {
                    StudentId = target.StudentId,
                    CourseId = target.CourseId,
                    AbsenceProbability = (decimal)prediction.Probability,
                    RiskFlag = riskFlag,
                    NextLectureDate = DateTime.UtcNow.AddDays(7)
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AI Prediction Pipeline completed successfully. Risk flags updated.");
    }
}
