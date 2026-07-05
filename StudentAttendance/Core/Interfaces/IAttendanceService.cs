using System;
using System.Threading.Tasks;

namespace StudentAttendance.Core.Interfaces;

public interface IAttendanceService
{
    /// <summary>
    /// Processes a fingerprint check-in event.
    /// </summary>
    /// <param name="enrollNumber">
    /// The ZKTeco EnrollNumber string, which maps to Student.ExternalId in the database.
    /// </param>
    /// <param name="timestamp">The UTC time of the scan event.</param>
    Task ProcessCheckInAsync(string enrollNumber, DateTime timestamp);
}
