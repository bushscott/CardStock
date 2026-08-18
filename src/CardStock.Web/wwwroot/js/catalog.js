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

// Focus trap for BrowseFilterPopover (D-110 spec §5: role=dialog + Esc + focus trap). Tab and
// Shift+Tab must never carry focus out of the open popover onto the page behind it. Blazor's
// @onkeydown:preventDefault directive is static per element (always on or always off) and
// can't be scoped to just Tab -- Esc and every other key inside the popover still need to
// reach BrowseFilterPopover.razor's KeyDown handler unmodified -- so only plain JS can single
// out Tab here. preventDefault() cancels just the browser's default focus move; it does not
// stop propagation, so Blazor's own delegated listener still sees the keydown afterward.
// Installed once per popover instance on first render, same shape as
// installGripCapture/installHeaderKeyGuard above. When focus sits on the dialog root itself
// (FocusAsync's initial target) Tab enters the content at the first focusable element and
// Shift+Tab wraps to the last, matching common focus-trap convention.
export function installFocusTrap(dialogElement) {
    dialogElement.addEventListener('keydown', e => {
        if (e.key !== 'Tab') { return; }

        const focusable = Array.from(dialogElement.querySelectorAll(
            'button:not([disabled]), [tabindex]:not([tabindex="-1"])'));
        if (focusable.length === 0) { return; }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const current = document.activeElement;

        if (e.shiftKey && (current === first || current === dialogElement)) {
            e.preventDefault();
            last.focus();
        } else if (!e.shiftKey && (current === last || current === dialogElement)) {
            e.preventDefault();
            first.focus();
        }
    });
}
