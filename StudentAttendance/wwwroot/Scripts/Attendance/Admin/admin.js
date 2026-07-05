const AttendanceApp = window.AttendanceApp || {};

AttendanceApp.Admin = (function () {
    const init = function () {
        const btnSaveUser = document.getElementById('btnSaveUser');
        
        if (btnSaveUser) {
            btnSaveUser.addEventListener('click', function (e) {
                e.preventDefault();
                
                // Form validation
                const username = document.getElementById('NewUsername').value;
                const role = document.getElementById('NewUserRole').value;
                const assocId = document.getElementById('AssociatedId').value;
                
                if (!username || username.trim() === '') {
                    alert('Username is required.');
                    return;
                }
                if (role !== 'Admin' && (!assocId || assocId.trim() === '')) {
                    alert('Associated ID is required for Students and Teachers.');
                    return;
                }
                
                alert('User saved successfully! (Mock)');
                
                // Hide modal gracefully if bootstrap is loaded
                if (typeof bootstrap !== 'undefined') {
                    const modalEl = document.getElementById('addUserModal');
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) {
                        modal.hide();
                    }
                }
            });
        }

        // Action Buttons: Edit and Toggle Status
        const editButtons = document.querySelectorAll('.btn-edit-user');
        editButtons.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const userId = this.getAttribute('data-userid');
                alert('Opening edit form for User ID: ' + userId + ' (Mock)');
            });
        });

        const toggleButtons = document.querySelectorAll('.btn-toggle-status');
        toggleButtons.forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const userId = this.getAttribute('data-userid');
                const currentStatus = this.getAttribute('data-status');
                const newStatus = currentStatus === 'True' ? 'Deactivate' : 'Activate';
                if (confirm('Are you sure you want to ' + newStatus + ' user ' + userId + '?')) {
                    alert('User status toggled! (Mock)');
                }
            });
        });
    };

    return {
        init: init
    };
})();

document.addEventListener("DOMContentLoaded", AttendanceApp.Admin.init);
