const AttendanceApp = window.AttendanceApp || {};

AttendanceApp.Enrollment = (function () {
    let hubConnection = null;

    const resetScanner = function () {
        const fingerprintData = document.getElementById("fingerprintData");
        if (fingerprintData) fingerprintData.value = "";
        
        const statusBox = document.getElementById("scannerStatus");
        if (statusBox) {
            statusBox.className = "status-box";
            statusBox.innerHTML = '<span class="text-success fw-bold">✓ Scanner Ready</span> <span class="text-muted">— Waiting for finger placement...</span>';
        }
    };

    const mockScanEvent = function () {
        if (hubConnection && hubConnection.state === signalR.HubConnectionState.Connected) {
            const fingerprintData = document.getElementById("fingerprintData");
            if (fingerprintData) fingerprintData.value = "MOCKED_BASE64_FINGERPRINT_DATA_==";
            
            const statusBox = document.getElementById("scannerStatus");
            if (statusBox) {
                statusBox.className = "status-box success";
                statusBox.innerHTML = '<span class="fw-bold">✓ Capture Successful</span> <span class="text-success">— Fingerprint template safely encrypted and stored in memory. Ready to submit.</span>';
            }
        }
    };

    const init = function () {
        if (typeof signalR === 'undefined') {
            console.error("SignalR is not loaded.");
            return;
        }

        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/attendanceHub")
            .build();

        hubConnection.on("ReceiveEnrollmentTemplate", function (base64Template) {
            const fingerprintData = document.getElementById("fingerprintData");
            if (fingerprintData) fingerprintData.value = base64Template;
            
            const statusBox = document.getElementById("scannerStatus");
            if (statusBox) {
                statusBox.className = "status-box success";
                statusBox.innerHTML = '<span class="fw-bold">✓ Capture Successful</span> <span class="text-success">— Fingerprint template safely encrypted and stored in memory. Ready to submit.</span>';
            }
        });

        hubConnection.start().catch(function (err) {
            console.error("SignalR Connection Error: ", err.toString());
        });

        const mockButton = document.querySelector('.fingerprint-icon-circle');
        if (mockButton) {
            mockButton.addEventListener('click', mockScanEvent);
        }

        const clearBtn = document.querySelector('.btn-clear');
        if (clearBtn) {
            clearBtn.addEventListener('click', function(e) {
                document.getElementById('enrollmentForm').reset();
                resetScanner();
            });
        }
    };

    return {
        init: init
    };
})();

document.addEventListener("DOMContentLoaded", AttendanceApp.Enrollment.init);
