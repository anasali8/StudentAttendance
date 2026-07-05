const AttendanceApp = window.AttendanceApp || {};

AttendanceApp.Enrollment = (function () {
    const resetScanner = function () {
        const fingerprintData = document.getElementById("fingerprintData");
        if (fingerprintData) fingerprintData.value = "";
        
        const statusBox = document.getElementById("scannerStatus");
        if (statusBox) {
            statusBox.className = "status-box";
            statusBox.innerHTML = '<span class="text-success fw-bold">✓ Ready</span> <span class="text-muted">— Waiting to sync with device...</span>';
        }
    };

    const pullFromDevice = function () {
        const studentId = document.getElementById("StudentIdString")?.value;
        if (!studentId) {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'warning',
                title: 'Please enter a Student ID first',
                showConfirmButton: false,
                timer: 3000
            });
            return;
        }

        const statusBox = document.getElementById("scannerStatus");
        if (statusBox) {
            statusBox.className = "status-box";
            statusBox.innerHTML = '<span class="text-primary fw-bold">↻ Syncing...</span> <span class="text-muted">Contacting ZKTeco device over network...</span>';
        }

        // Call our new backend API
        fetch(`/api/fingerprint/sync/${studentId}`)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    const fingerprintData = document.getElementById("fingerprintData");
                    if (fingerprintData) fingerprintData.value = data.templateBase64;
                    
                    if (statusBox) {
                        statusBox.className = "status-box success";
                        statusBox.innerHTML = '<span class="fw-bold">✓ Template Synced</span> <span class="text-success">— Fingerprint template successfully pulled from device. Ready to enroll.</span>';
                    }
                    Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: 'Fingerprint pulled successfully!', showConfirmButton: false, timer: 3000 });
                } else {
                    // Fallback to mock for testing if device not found
                    console.warn("Device sync failed, falling back to mock:", data.message);
                    fetch(`/api/fingerprint/mock`)
                        .then(r => r.json())
                        .then(mockData => {
                            const fingerprintData = document.getElementById("fingerprintData");
                            if (fingerprintData) fingerprintData.value = mockData.templateBase64;
                            
                            if (statusBox) {
                                statusBox.className = "status-box warning";
                                statusBox.innerHTML = `<span class="fw-bold">⚠ Device Offline (MOCK MODE)</span> <span class="text-warning">— Could not connect to device: ${data.message}. Using mock template for testing.</span>`;
                            }
                            Swal.fire({ toast: true, position: 'top-end', icon: 'info', title: 'Using Mock Template (Testing Mode)', showConfirmButton: false, timer: 3000 });
                        });
                }
            })
            .catch(err => {
                console.error("Fetch error:", err);
                if (statusBox) {
                    statusBox.className = "status-box danger";
                    statusBox.innerHTML = '<span class="fw-bold">✗ Network Error</span> <span class="text-danger">— Failed to communicate with server API.</span>';
                }
            });
    };

    const init = function () {
        const syncButton = document.querySelector('.fingerprint-icon-circle');
        if (syncButton) {
            syncButton.addEventListener('click', pullFromDevice);
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
