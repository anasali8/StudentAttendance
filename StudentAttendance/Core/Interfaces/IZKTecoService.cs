namespace StudentAttendance.Core.Interfaces;

public interface IZKTecoService
{
    bool Connect(string ipAddress, int port);
    void Disconnect();
    
    // Registering the event callback
    void RegisterAttendanceEventListener(Action<string, DateTime> onAttendanceScanned);
    
    // Fetching/Writing templates (for backup purposes)
    byte[] GetUserTemplate(string enrollNumber);
    bool SetUserTemplate(string enrollNumber, byte[]? templateData);
}
