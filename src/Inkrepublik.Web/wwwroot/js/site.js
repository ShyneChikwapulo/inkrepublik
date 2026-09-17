// ============================================================================
// Inkrepublik — small site-wide JS helpers.
// Kept minimal on purpose. Only things that genuinely need the browser.
// ============================================================================

/**
 * Toggles a CSS class on an element based on window scroll position.
 * Used by the header to fade in a background once the user scrolls.
 *
 * @param {HTMLElement} element - target element (usually the header)
 * @param {string} className - class to toggle when scrollY exceeds threshold
 * @param {number} threshold - pixels before toggling (default 40)
 * @returns {Function} cleanup function to remove the listener
 */
export function initScrollToggle(element, className, threshold = 40) {
    if (!element) return () => { };

    const onScroll = () => {
        if (window.scrollY > threshold) {
            element.classList.add(className);
        } else {
            element.classList.remove(className);
        }
    };

    // Apply immediately in case the page loads already scrolled
    onScroll();

    window.addEventListener('scroll', onScroll, { passive: true });

    return () => window.removeEventListener('scroll', onScroll);
}