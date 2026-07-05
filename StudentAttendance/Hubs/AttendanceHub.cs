using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace StudentAttendance.Hubs;

// We can strongly type the Hub by defining an interface for the client methods
public interface IAttendanceHubClient
{
    Task ReceiveAttendanceUpdate(int studentId, string studentName, string classification, string timestamp);
    Task ReceiveScanError(string message);
}

public class AttendanceHub : Hub<IAttendanceHubClient>
{
    // The Hub manages connections. The Teacher Dashboard will connect here.
    // We don't need to define methods for clients to call the server in this scenario,
    // because the server (background service) pushes to the clients.
}
