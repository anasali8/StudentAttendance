namespace StudentAttendance.Core.Enums;

public enum UserRole
{
    Admin = 1,
    Teacher = 2,
    Student = 3
}

public enum LatenessClassification
{
    OnTime = 1,
    LateTier1 = 2,  // 0-10 min
    LateTier2 = 3,  // 10-20 min
    LateTier3 = 4,  // >20 min
    Absent = 5
}

public enum RiskFlag
{
    Low = 1,
    Medium = 2,
    High = 3
}
