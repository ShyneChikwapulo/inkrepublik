// ============================================================================
// Inkrepublik — small site-wide JS helpers.
// Kept minimal on purpose. Only things that genuinely need the browser.
// ============================================================================

/**
 * Toggles a CSS class on an element based on window scroll position.
 * Used by the header to fade in a background once the user scrolls.
 *
 * @param {HTMLElement} element - target element
 * @param {string} className - class to toggle when scrollY exceeds threshold
 * @param {number} threshold - pixels before toggling (default 40)
 */
export function initScrollToggle(element, className, threshold = 40) {
    if (!element) return;

    const onScroll = () => {
        if (window.scrollY > threshold) {
            element.classList.add(className);
        } else {
            element.classList.remove(className);
        }
    };

    // Apply immediately in case the page loads already scrolled
    onScroll();

    // Stash the listener so we can remove it later
    element._inkScrollHandler = onScroll;
    window.addEventListener('scroll', onScroll, { passive: true });
}

/**
 * Removes the scroll listener installed by initScrollToggle.
 *
 * @param {HTMLElement} element - same element passed to initScrollToggle
 */
export function disposeScrollToggle(element) {
    if (!element) return;

    if (element._inkScrollHandler) {
        window.removeEventListener('scroll', element._inkScrollHandler);
        delete element._inkScrollHandler;
    }
}