// Generic click-to-enlarge lightbox. Attach class="photo-thumb" and data-full-src="..." to any
// <img> and clicking it opens that image full-size in the shared modal declared once in _Layout.
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var modalEl = document.getElementById('photoLightbox');
        var modalImage = document.getElementById('photoLightboxImage');
        if (!modalEl || !modalImage || typeof bootstrap === 'undefined') {
            return;
        }

        var modal = new bootstrap.Modal(modalEl);
        document.querySelectorAll('.photo-thumb').forEach(function (thumb) {
            thumb.addEventListener('click', function () {
                modalImage.src = thumb.getAttribute('data-full-src');
                modal.show();
            });
        });
    });
})();
