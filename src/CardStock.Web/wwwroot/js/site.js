// Card page lightbox (card.md §5.3 / OQ-8): return focus to the art thumbnail when the
// lightbox closes, since dismissing an overlay should never strand keyboard focus.
function focusElement(id) {
    var el = document.getElementById(id);
    if (el) {
        el.focus();
    }
}

// Sales ledger group popovers (card.md §4.4/§4.8/§5.8): close every open popover on a
// mousedown outside all elements matching `selector`. `selector` matches the group WRAPPER
// (button + popover together, marked [data-lg-pop]), not just the popover panel -- otherwise
// a mousedown on the toggle button itself would count as "outside" and fight its own @onclick
// reopen. Generic over selector/callback name so any future outside-dismiss popover (e.g. the
// watchlist picker) can reuse it.
function watchOutsideMousedown(selector, dotnetRef) {
    document.addEventListener("mousedown", function (e) {
        if (!e.target.isConnected) {
            return;
        }
        var wrappers = document.querySelectorAll(selector);
        for (var i = 0; i < wrappers.length; i++) {
            if (wrappers[i].contains(e.target)) {
                return;
            }
        }
        dotnetRef.invokeMethodAsync("CloseGroups");
    });
}

// Sales ledger column resize (card.md §5.5 #31, R-7): drag a header grip to resize its column.
// The clamp to 40-420px is enforced on the .NET side (SalesLedger.SetColumnWidth) since that is
// also where it is unit-testable without a real pointer; this helper only reports raw deltas.
function startColumnDrag(dotnetRef, key, startWidth, startX) {
    function onMove(e) {
        dotnetRef.invokeMethodAsync("SetColumnWidth", key, startWidth + (e.clientX - startX));
    }
    function onUp() {
        window.removeEventListener("mousemove", onMove);
        window.removeEventListener("mouseup", onUp);
    }
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
}
