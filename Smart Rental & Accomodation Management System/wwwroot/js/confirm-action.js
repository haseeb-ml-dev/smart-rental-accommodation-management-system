// Generic "are you sure?" gate for critical actions. Attach data-confirm-title / data-confirm-body
// to any submit button inside a <form> and clicking it shows a shared Bootstrap modal instead of
// submitting immediately; only clicking the modal's own Confirm button submits the form. Replaces
// native confirm() popups, which are visually inconsistent and can block the whole page.
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var modalEl = document.getElementById('confirmActionModal');
        if (!modalEl || typeof bootstrap === 'undefined') {
            return;
        }

        var modal = new bootstrap.Modal(modalEl);
        var titleEl = document.getElementById('confirmActionModalTitle');
        var bodyEl = document.getElementById('confirmActionModalBody');
        var confirmBtn = document.getElementById('confirmActionModalConfirm');
        var pendingForm = null;

        document.querySelectorAll('[data-confirm-title]').forEach(function (trigger) {
            trigger.addEventListener('click', function (e) {
                var form = trigger.closest('form');
                if (!form) {
                    return;
                }

                e.preventDefault();
                titleEl.textContent = trigger.getAttribute('data-confirm-title') || 'Are you sure?';
                bodyEl.textContent = trigger.getAttribute('data-confirm-body') || '';
                pendingForm = form;
                modal.show();
            });
        });

        confirmBtn.addEventListener('click', function () {
            modal.hide();
            if (pendingForm) {
                HTMLFormElement.prototype.submit.call(pendingForm);
                pendingForm = null;
            }
        });
    });
})();
