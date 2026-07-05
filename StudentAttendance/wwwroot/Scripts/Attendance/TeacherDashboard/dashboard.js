const AttendanceApp = window.AttendanceApp || {};

AttendanceApp.TeacherDashboard = (function () {
    const getKpiEl = (id) => document.getElementById(id);

    const updateKpiCards = function (classification) {
        // Increment the "Present" counter on every check-in (on-time or late)
        const presentEl = document.getElementById('kpi-present');
        if (presentEl) {
            presentEl.textContent = parseInt(presentEl.textContent || '0', 10) + 1;
        }

        // Also increment "Late" if applicable
        if (classification !== 'OnTime') {
            const lateEl = document.getElementById('kpi-late');
            if (lateEl) {
                lateEl.textContent = parseInt(lateEl.textContent || '0', 10) + 1;
            }
        }

        // Decrement "Absent" 
        const absentEl = document.getElementById('kpi-absent');
        if (absentEl) {
            const current = parseInt(absentEl.textContent || '0', 10);
            if (current > 0) absentEl.textContent = current - 1;
        }
    };

    const showNotification = function (message, isError) {
        const banner = document.getElementById('signalr-notification');
        if (!banner) return;
        banner.textContent = message;
        banner.className = isError
            ? 'alert alert-danger alert-dismissible py-2 px-3 mb-0 small'
            : 'alert alert-success alert-dismissible py-2 px-3 mb-0 small';
        banner.style.display = 'block';
        setTimeout(() => { banner.style.display = 'none'; }, 4000);
    };

    const init = function () {
        if (typeof signalR === 'undefined') {
            console.error("SignalR is not loaded.");
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/attendanceHub")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveAttendanceUpdate", function (studentId, studentName, classification, timestamp) {
            // 1. Update the row in the attendance table
            const statusCell = document.getElementById("status-" + studentId);
            if (statusCell) {
                if (classification === "OnTime") {
                    statusCell.innerHTML = '<span class="status-badge status-ontime">On Time</span>';
                } else {
                    statusCell.innerHTML = `<span class="status-badge status-late">${classification}</span>`;
                }
            }

            const timeCell = document.getElementById("time-" + studentId);
            if (timeCell) {
                timeCell.textContent = timestamp;
            }

            // 2. Update KPI cards live
            updateKpiCards(classification);

            // 3. Show a brief success notification
            showNotification(`\u2713 ${studentName} checked in at ${timestamp}`, false);
        });

        connection.on("ReceiveScanError", function (message) {
            showNotification(`\u26a0 ${message}`, true);
        });

        connection.start().catch(function (err) {
            console.error(err.toString());
        });
    };

    return {
        init: init
    };
})();

document.addEventListener("DOMContentLoaded", AttendanceApp.TeacherDashboard.init);

