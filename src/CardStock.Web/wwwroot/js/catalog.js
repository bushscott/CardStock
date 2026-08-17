// Column-resize support for RosterTable: pointer capture keeps move events flowing to the
// grip after the cursor leaves it mid-drag. One delegated listener installed once per table
// (in OnAfterRenderAsync's firstRender branch) rather than a JS call per grip per pointerdown --
// the event target is still the pressed .rt-grip when this fires, so capture attaches to the
// exact element the drag started on.
export function installGripCapture(tableElement) {
    tableElement.addEventListener('pointerdown', e => {
        const grip = e.target.closest('.rt-grip');
        if (grip) { grip.setPointerCapture(e.pointerId); }
    });
}

// Sortable headers sort on Enter/Space (RosterTable.razor's HeaderKeyAsync), but a focusable,
// non-form-control element's browser default action for Space is to scroll the viewport --
// so without this, a keyboard user sorting a column also scrolls the page. Blazor's
// @onkeydown:preventDefault directive is static per element (always on or always off) and
// would swallow Tab too, breaking keyboard navigation off the header entirely; suppressing
// only Space/Enter, and only on sortable headers, needs a per-key check that only plain JS
// can do here. preventDefault() cancels just the browser's default action -- it does not stop
// propagation -- so Blazor's own listener still sees the event and HeaderKeyAsync still runs
// the sort; this only silences the scroll.
export function installHeaderKeyGuard(tableElement) {
    tableElement.addEventListener('keydown', e => {
        if ((e.key === 'Enter' || e.key === ' ') && e.target.closest('.rt-head-cell.sortable')) {
            e.preventDefault();
        }
    });
}
