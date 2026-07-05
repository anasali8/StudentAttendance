using System;
using StudentAttendance.Core.Enums;
using StudentAttendance.Core.Interfaces;

namespace StudentAttendance.Core.Services;

public class LatenessEngine : ILatenessEngine
{
    public LatenessClassification CalculateLateness(DateTime scheduledStart, DateTime checkInTime)
    {
        var delay = checkInTime - scheduledStart;

        if (delay.TotalMinutes <= 0)
        {
            return LatenessClassification.OnTime;
        }
        else if (delay.TotalMinutes <= 10)
        {
            return LatenessClassification.LateTier1;
        }
        else if (delay.TotalMinutes <= 20)
        {
            return LatenessClassification.LateTier2;
        }
        else
        {
            return LatenessClassification.LateTier3;
        }
    }
}
