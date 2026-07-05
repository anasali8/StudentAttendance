const AttendanceApp = window.AttendanceApp || {};

AttendanceApp.ClassroomTerminal = (function () {
    let checkinCounter = 0;

    const updateClock = function () {
        const clockElement = document.getElementById('clock');
        if (clockElement) {
            const now = new Date();
            const timeString = now.toLocaleTimeString('en-GB', { hour12: false });
            clockElement.textContent = timeString;
        }
    };

    const setStatus = function (message, isError) {
        const statusBox = document.getElementById("statusBox");
        if (!statusBox) return;

        if (isError) {
            statusBox.style.borderColor = '#ef4444';
            statusBox.style.borderLeftColor = '#ef4444';
            statusBox.style.backgroundColor = '#fef2f2';
            statusBox.innerHTML = `<div class="fw-bold text-danger"><i class="bi bi-x-circle"></i> Scan Failed</div><div class="text-muted small mt-1">${message}</div>`;
            setTimeout(() => {
                statusBox.style.borderColor = '#22c55e';
                statusBox.style.borderLeftColor = '#22c55e';
                statusBox.style.backgroundColor = '#f0fdf4';
                statusBox.innerHTML = `<div class="fw-bold text-success"><i class="bi bi-check-lg"></i> Scanner Ready</div><div class="text-muted small mt-1">Waiting for finger placement...</div>`;
            }, 4000);
        } else {
            statusBox.style.borderColor = '#22c55e';
            statusBox.style.borderLeftColor = '#22c55e';
            statusBox.style.backgroundColor = '#f0fdf4';
            statusBox.innerHTML = `<div class="fw-bold text-success"><i class="bi bi-check-lg"></i> Scanner Ready</div><div class="text-muted small mt-1">${message}</div>`;
        }
    };


    const initSignalR = function () {
        if (typeof signalR === 'undefined') {
            console.error("SignalR is not loaded.");
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/attendanceHub")
            .build();

        connection.on("ReceiveAttendanceUpdate", function (studentId, studentName, classification, timestamp) {
            const container = document.getElementById("recentCheckinsContainer");
            if (container) {
                checkinCounter++;
                const bgClass = checkinCounter % 2 === 1 ? 'bg-light-custom' : '';
                let statusClass = 'text-warning';
                let displayStatus = classification;
                
                if (classification === "OnTime") {
                    statusClass = 'text-success';
                    displayStatus = 'On Time';
                }

                const checkinHtml = `
                    <div class="checkin-item ${bgClass}">
                        <div>
                            <div class="small text-muted">${timestamp}</div>
                            <div class="fw-bold">${studentName}</div>
                        </div>
                        <span class="${statusClass} small fw-bold">${displayStatus}</span>
                    </div>
                `;
                
                // Prepend to show latest at the top
                container.insertAdjacentHTML('afterbegin', checkinHtml);
                
                // Keep only top 10 items
                if (container.children.length > 10) {
                    container.removeChild(container.lastElementChild);
                }
            }
            setStatus(`Checked in: ${studentName}`);
        });

        connection.on("ReceiveScanError", function (message) {
            setStatus(message, true);
        });

        connection.start().catch(function (err) {
            console.error(err.toString());
        });
    };

    const initFormAjax = function () {
        const form = document.getElementById('manualCheckInForm');
        if (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                
                const studentIdInput = document.getElementById('studentIdInput');
                const studentId = studentIdInput.value;
                const url = form.getAttribute('action');
                
                setStatus("Processing...");
                
                // Using URLSearchParams for x-www-form-urlencoded
                const params = new URLSearchParams();
                params.append('studentId', studentId);

                fetch(url, {
                    method: 'POST',
                    body: params
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        studentIdInput.value = '';
                        // SignalR will handle the UI update
                    } else {
                        setStatus(data.error || "Unknown error occurred.", true);
                    }
                })
                .catch(error => {
                    console.error("Error submitting manual check-in:", error);
                    setStatus("Network error.", true);
                });
            });
        }
    };

    const init = function () {
        setInterval(updateClock, 1000);
        updateClock();
        initSignalR();
        initFormAjax();
    };

    return {
        init: init
    };
})();

document.addEventListener("DOMContentLoaded", AttendanceApp.ClassroomTerminal.init);
