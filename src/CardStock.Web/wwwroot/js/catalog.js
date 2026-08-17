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
