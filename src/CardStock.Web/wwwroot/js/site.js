// Card page lightbox (card.md §5.3 / OQ-8): return focus to the art thumbnail when the
// lightbox closes, since dismissing an overlay should never strand keyboard focus.
function focusElement(id) {
    var el = document.getElementById(id);
    if (el) {
        el.focus();
    }
}
