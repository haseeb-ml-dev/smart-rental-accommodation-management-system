// Generic, keyless address autocomplete. Attach to any <input data-autocomplete="address">
// and it will suggest real-world addresses as the user types, using Photon
// (https://photon.komoot.io), a free geocoder built on OpenStreetMap data.
// Works worldwide, no API key, no region bias. Fails silently on any network
// or API error so typing a plain address by hand always still works.
//
// Optionally, when the input also carries data-lat-input / data-lng-input / data-confirmed-input
// (CSS selectors for hidden fields) and data-map-target (id of a map container), picking a
// suggestion also captures that suggestion's own coordinates (Photon already returns them, no
// extra request needed), shows a small Leaflet preview map with a draggable marker, and marks
// the location "confirmed" so the server trusts it instead of re-geocoding. Dragging the marker
// re-confirms the corrected position. Typing further after a location was confirmed clears the
// flag, since the old coordinate may no longer match the edited text.
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

    function getRelatedInput(referenceEl, attr) {
        var selector = referenceEl.getAttribute(attr);
        return selector ? document.querySelector(selector) : null;
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

        var latInput = getRelatedInput(input, 'data-lat-input');
        var lngInput = getRelatedInput(input, 'data-lng-input');
        var confirmedInput = getRelatedInput(input, 'data-confirmed-input');
        var mapTargetId = input.getAttribute('data-map-target');
        var mapCardSelector = input.getAttribute('data-map-card');
        var map = null;
        var marker = null;

        function showMapCard() {
            if (mapCardSelector) {
                var card = document.querySelector(mapCardSelector);
                if (card) {
                    card.classList.remove('d-none');
                }
            }
        }

        function ensureMap(lat, lng) {
            if (!mapTargetId || typeof L === 'undefined') {
                return;
            }

            showMapCard();

            if (!map) {
                var el = document.getElementById(mapTargetId);
                if (!el) {
                    return;
                }

                map = L.map(mapTargetId).setView([lat, lng], 15);
                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: '&copy; OpenStreetMap contributors',
                    maxZoom: 19
                }).addTo(map);

                marker = L.marker([lat, lng], { draggable: true }).addTo(map);
                marker.on('dragend', function () {
                    var pos = marker.getLatLng();
                    setCoordinates(pos.lat, pos.lng, true);
                });
            } else {
                map.setView([lat, lng], 15);
                marker.setLatLng([lat, lng]);
            }
        }

        function setCoordinates(lat, lng, confirmed) {
            if (latInput) {
                latInput.value = lat;
            }
            if (lngInput) {
                lngInput.value = lng;
            }
            if (confirmedInput) {
                confirmedInput.value = confirmed ? 'true' : 'false';
            }
            ensureMap(lat, lng);
        }

        // Edit mode: coordinates already exist server-side, show the map (with a draggable
        // marker) immediately instead of waiting for a fresh suggestion pick.
        if (latInput && lngInput && latInput.value && lngInput.value) {
            ensureMap(parseFloat(latInput.value), parseFloat(lngInput.value));
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

                            var coords = feature.geometry && feature.geometry.coordinates;
                            if (coords && coords.length === 2) {
                                // GeoJSON order is [lon, lat].
                                setCoordinates(coords[1], coords[0], true);
                            }
                        });
                        list.appendChild(item);
                    });

                    list.style.display = list.children.length ? 'block' : 'none';
                })
                .catch(function () { hideList(); });
        }, 350);

        input.setAttribute('autocomplete', 'off');
        input.addEventListener('input', function () {
            runSearch(input.value.trim());
            if (confirmedInput && confirmedInput.value === 'true') {
                confirmedInput.value = 'false';
            }
        });
        input.addEventListener('blur', function () { setTimeout(hideList, 150); });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var inputs = document.querySelectorAll('[data-autocomplete="address"]');
        for (var i = 0; i < inputs.length; i++) {
            initAutocomplete(inputs[i]);
        }
    });
})();
