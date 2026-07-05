using System;
using StudentAttendance.Core.Enums;

namespace StudentAttendance.Core.Interfaces;

public interface ILatenessEngine
{
    LatenessClassification CalculateLateness(DateTime scheduledStart, DateTime checkInTime);
}
