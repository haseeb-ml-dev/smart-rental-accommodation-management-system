// Generic, keyless address autocomplete. Attach to any <input data-autocomplete="address">
// and it will suggest real-world addresses as the user types, using Photon
// (https://photon.komoot.io), a free geocoder built on OpenStreetMap data.
// Works worldwide, no API key, no region bias. Fails silently on any network
// or API error so typing a plain address by hand always still works.
(function () {
    function debounce(fn, delayMs) {
        var timer;
        return function () {
            var args = arguments;
            var self = this;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(self, args); }, delayMs);
        };
    }

    function formatSuggestion(props) {
        var streetPart = props.housenumber && props.street
            ? props.street + ' ' + props.housenumber
            : (props.street || props.housenumber);

        var parts = [props.name, streetPart, props.city, props.state, props.country];
        var seen = {};
        var unique = [];

        for (var i = 0; i < parts.length; i++) {
            var value = parts[i];
            if (value && !seen[value]) {
                seen[value] = true;
                unique.push(value);
            }
        }

        return unique.join(', ');
    }

    function initAutocomplete(input) {
        var wrapper = document.createElement('div');
        wrapper.className = 'position-relative';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        var list = document.createElement('div');
        list.className = 'list-group position-absolute w-100 shadow-sm';
        list.style.zIndex = '1050';
        list.style.display = 'none';
        list.style.maxHeight = '220px';
        list.style.overflowY = 'auto';
        wrapper.appendChild(list);

        function hideList() {
            list.style.display = 'none';
            list.innerHTML = '';
        }

        var runSearch = debounce(function (query) {
            if (!query || query.length < 3) {
                hideList();
                return;
            }

            fetch('https://photon.komoot.io/api/?q=' + encodeURIComponent(query) + '&limit=5')
                .then(function (response) { return response.ok ? response.json() : null; })
                .then(function (data) {
                    if (!data || !data.features || !data.features.length) {
                        hideList();
                        return;
                    }

                    list.innerHTML = '';
                    data.features.forEach(function (feature) {
                        var label = formatSuggestion(feature.properties || {});
                        if (!label) {
                            return;
                        }

                        var item = document.createElement('button');
                        item.type = 'button';
                        item.className = 'list-group-item list-group-item-action';
                        item.textContent = label;
                        item.addEventListener('mousedown', function (e) {
                            // mousedown (not click) so this fires before the input's blur handler hides the list.
                            e.preventDefault();
                            input.value = label;
                            hideList();
                            input.dispatchEvent(new Event('change', { bubbles: true }));
                        });
                        list.appendChild(item);
                    });

                    list.style.display = list.children.length ? 'block' : 'none';
                })
                .catch(function () { hideList(); });
        }, 350);

        input.setAttribute('autocomplete', 'off');
        input.addEventListener('input', function () { runSearch(input.value.trim()); });
        input.addEventListener('blur', function () { setTimeout(hideList, 150); });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var inputs = document.querySelectorAll('[data-autocomplete="address"]');
        for (var i = 0; i < inputs.length; i++) {
            initAutocomplete(inputs[i]);
        }
    });
})();
