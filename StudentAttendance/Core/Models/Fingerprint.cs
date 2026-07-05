namespace StudentAttendance.Core.Models;

public class Fingerprint
{
    // The StudentId acts as both PK and FK (1-to-1)
    // This will also map to the ZKTeco 'EnrollNumber'
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public byte[] EncryptedTemplate { get; set; } = Array.Empty<byte>();
    public DateTime EnrollmentDate { get; set; }
}
