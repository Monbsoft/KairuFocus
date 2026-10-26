// JS interop of the Community page and of the leaderboard section of the settings.
// Three things Blazor cannot do on its own:
//   - know when the tab is hidden (to pause the presence polling);
//   - scroll to and focus a section reached through an anchor, once the section has loaded;
//   - keep the focus inside a modal.
window.kairufocusCommunity = (function () {
    'use strict';

    var visibilityHandler = null;

    // Forwards `visibilitychange` to the component (method OnVisibilityChanged(bool hidden)).
    // Returns the current state so that the component can start in the right mode.
    function observeVisibility(dotNetRef) {
        unobserveVisibility();
        visibilityHandler = function () {
            // The component may have been disposed meanwhile: nothing to report then.
            dotNetRef.invokeMethodAsync('OnVisibilityChanged', document.hidden).catch(function () { });
        };
        document.addEventListener('visibilitychange', visibilityHandler);
        return document.hidden;
    }

    function unobserveVisibility() {
        if (visibilityHandler) {
            document.removeEventListener('visibilitychange', visibilityHandler);
            visibilityHandler = null;
        }
    }

    // When the URL fragment is `#sectionId`, scrolls to the section and moves the focus to its title.
    // Returns whether it did. The title must be focusable (tabindex="-1").
    function focusSectionIfRequested(sectionId, titleId) {
        if (window.location.hash !== '#' + sectionId) { return false; }
        var section = document.getElementById(sectionId);
        var title = document.getElementById(titleId);
        if (!section || !title) { return false; }
        section.scrollIntoView({ block: 'start' });
        title.focus({ preventScroll: true });
        return true;
    }

    var FOCUSABLE = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), ' +
        'textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

    // Keeps Tab and Shift+Tab inside the container. The listener dies with the element.
    // The container itself must be focusable (tabindex="-1"): it holds the focus when nothing inside can,
    // e.g. while its buttons are disabled during a request.
    function trapFocus(container) {
        if (!container) { return; }
        container.addEventListener('keydown', function (event) {
            if (event.key !== 'Tab') { return; }
            var items = container.querySelectorAll(FOCUSABLE);
            if (!items.length) {
                // Nothing focusable inside right now: the focus stays on the container instead of leaving it.
                event.preventDefault();
                container.focus();
                return;
            }
            var first = items[0];
            var last = items[items.length - 1];
            if (event.shiftKey && (document.activeElement === first || document.activeElement === container)) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        });
    }

    return {
        observeVisibility: observeVisibility,
        unobserveVisibility: unobserveVisibility,
        focusSectionIfRequested: focusSectionIfRequested,
        trapFocus: trapFocus
    };
})();
