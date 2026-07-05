using System.Runtime.InteropServices;
using StudentAttendance.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace StudentAttendance.Infrastructure.Services;

/// <summary>
/// Note: This class requires the zkemkeeper.dll COM reference to be added to the project.
/// If you haven't added it yet, right-click Dependencies -> Add COM Reference -> ZKEMKEEPER.
/// We use dynamic here to avoid compile errors if the DLL is not yet referenced, but strongly-typed is preferred in production.
/// </summary>
public class ZKTecoService : IZKTecoService
{
    private dynamic? _zkemClient;
    private Action<string, DateTime>? _onAttendanceScanned;
    private bool _isConnected = false;
    private readonly ILogger<ZKTecoService> _logger;

    public ZKTecoService(ILogger<ZKTecoService> logger)
    {
        _logger = logger;
        // Dynamically instantiate the COM object. 
        // If strongly-typed interop is used, it would be: _zkemClient = new zkemkeeper.CZKEMClass();
#pragma warning disable CA1416 // ZKTeco COM interop is intentionally Windows-only
        var type = Type.GetTypeFromProgID("zkemkeeper.CZKEM");
#pragma warning restore CA1416
        if (type != null)
        {
            _zkemClient = Activator.CreateInstance(type);
        }
        else
        {
            _logger.LogWarning("zkemkeeper is not registered on this machine");
        }
    }

    public bool Connect(string ipAddress, int port)
    {
        if (_zkemClient == null) return false;

        _isConnected = _zkemClient.Connect_Net(ipAddress, port);
        
        if (_isConnected)
        {
            // Register real-time events on the device
            _zkemClient.RegEvent(1, 65535); 
            
            _zkemClient.OnAttTransactionEx += new Action<string, int, int, int, int, int, int, int, int, int, int>(HandleAttTransactionEx);
        }
        return _isConnected;
    }

    private void HandleAttTransactionEx(string enrollNumber, int isInValid, int attState, int verifyMethod, int year, int month, int day, int hour, int minute, int second, int workCode)
    {
        if (isInValid == 1) return;

        var timestamp = new DateTime(year, month, day, hour, minute, second);
        _onAttendanceScanned?.Invoke(enrollNumber, timestamp);
    }

    public void Disconnect()
    {
        if (_isConnected && _zkemClient != null)
        {
            _zkemClient!.Disconnect();
            _isConnected = false;
        }
    }

    public void RegisterAttendanceEventListener(Action<string, DateTime> onAttendanceScanned)
    {
        _onAttendanceScanned = onAttendanceScanned;
    }

    public byte[] GetUserTemplate(string enrollNumber)
    {
        if (!_isConnected || _zkemClient == null) return Array.Empty<byte>();

        // ZKTeco returns templates as string or byte ref depending on the SDK version (e.g., GetUserTmpExStr)
        int dwMachineNumber = 1;
        int dwFingerIndex = 0;
        int dwFlag = 1;
        string tmpData = "";
        int tmpLength = 0;

        bool success = _zkemClient!.GetUserTmpExStr(dwMachineNumber, enrollNumber, dwFingerIndex, out dwFlag, out tmpData, out tmpLength);
        
        if (success && !string.IsNullOrEmpty(tmpData))
        {
            return Convert.FromBase64String(tmpData);
        }

        return Array.Empty<byte>();
    }

    public bool SetUserTemplate(string enrollNumber, byte[]? templateData)
    {
#pragma warning disable CS8602
        if (!_isConnected || _zkemClient == null || templateData == null || templateData.Length == 0) return false;
#pragma warning restore CS8602

        int dwMachineNumber = 1;
        int dwFingerIndex = 0;
        int dwFlag = 1;
        string tmpData = Convert.ToBase64String(templateData!);

        return _zkemClient!.SetUserTmpExStr(dwMachineNumber, enrollNumber, dwFingerIndex, dwFlag, tmpData);
    }
}
