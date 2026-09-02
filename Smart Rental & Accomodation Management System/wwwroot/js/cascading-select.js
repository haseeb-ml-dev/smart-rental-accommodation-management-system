// Generic cascading <select> behavior. Attach data-cascade-source="#idOfSourceSelect" and
// data-cascade-options='{"City A":["Area 1","Area 2"], ...}' to the dependent <select> and it
// repopulates its own options from the map whenever the source select changes, keeping the
// previously selected value if it's still valid for the new source value.
(function () {
    function initCascade(target) {
        var sourceSelector = target.getAttribute('data-cascade-source');
        var source = sourceSelector ? document.querySelector(sourceSelector) : null;
        if (!source) {
            return;
        }

        var optionsMap = {};
        try {
            optionsMap = JSON.parse(target.getAttribute('data-cascade-options') || '{}');
        } catch (e) {
            optionsMap = {};
        }

        var placeholder = target.getAttribute('data-cascade-placeholder') || 'Any';
        var initialValue = target.getAttribute('data-cascade-initial') || '';

        function rebuild(selectedValue) {
            var values = optionsMap[source.value] || [];
            target.innerHTML = '';

            var placeholderOption = document.createElement('option');
            placeholderOption.value = '';
            placeholderOption.textContent = placeholder;
            target.appendChild(placeholderOption);

            values.forEach(function (value) {
                var option = document.createElement('option');
                option.value = value;
                option.textContent = value;
                if (value === selectedValue) {
                    option.selected = true;
                }
                target.appendChild(option);
            });

            target.disabled = values.length === 0;
        }

        rebuild(initialValue);
        source.addEventListener('change', function () { rebuild(''); });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var targets = document.querySelectorAll('[data-cascade-source]');
        for (var i = 0; i < targets.length; i++) {
            initCascade(targets[i]);
        }
    });
})();
