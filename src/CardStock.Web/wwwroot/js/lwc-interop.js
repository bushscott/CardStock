// The PriceChart wrapper around TradingView Lightweight Charts v5.2.1 (see
// lib/lightweight-charts/VERSION.txt for the vendoring/API-surface notes). card.md
// §2.4/§2.4.1/§2.4.2/§5.4. Loaded as a classic script (LWC's standalone build first, this
// module second, both before blazor.webassembly.js) so Blazor's IJSRuntime can reach
// `lwcInterop.*` by dotted name -- no ES module import wiring needed.
//
// Interop-layer adaptation from the task-17 brief's shorthand (`init(elementId, dotnetRef)`,
// `setData(shape)`, `dotY(value, tier)`, `applyTheme()`, `dispose(elementId)`): every function
// here takes `elementId` explicitly, because this one module tracks per-chart state in a Map
// keyed by element id rather than assuming a single global chart. The C# shaper contract
// (ChartShape/ShapedSeries/ShapedPoint) is untouched by this -- only the JS call shape adapts.
window.lwcInterop = (function () {
    "use strict";

    /** @type {Map<string, object>} elementId -> { chart, dotnetRef, seriesByTier, monthIndex, lastShape, onResize } */
    const instances = new Map();

    function resolveColor(color) {
        if (typeof color === "string" && color.startsWith("--")) {
            const resolved = getComputedStyle(document.documentElement).getPropertyValue(color).trim();
            return resolved || "#888888";
        }
        return color;
    }

    // A crosshair event's `param.time` comes back as whatever shape the library normalizes
    // "yyyy-MM-dd" strings into -- a plain string in some builds, a {year,month,day} business-day
    // object in others. Normalize both to "yyyy-MM-dd" so it can be looked up against the
    // month -> index map built in setData().
    function timeKey(time) {
        if (time == null) {
            return null;
        }
        if (typeof time === "string") {
            return time;
        }
        if (typeof time === "object" && "year" in time) {
            const month = String(time.month).padStart(2, "0");
            const day = String(time.day).padStart(2, "0");
            return `${time.year}-${month}-${day}`;
        }
        return null;
    }

    function init(elementId, dotnetRef) {
        const el = document.getElementById(elementId);
        if (!el || !window.LightweightCharts) {
            return;
        }

        const chart = LightweightCharts.createChart(el, {
            width: el.clientWidth,
            height: 230,
            layout: {
                background: { color: "transparent" },
                textColor: resolveColor("--ink"),
                attributionLogo: false,
            },
            grid: {
                vertLines: { visible: false },
                horzLines: { visible: false },
            },
            rightPriceScale: { visible: false },
            leftPriceScale: { visible: false },
            timeScale: { visible: true, borderVisible: false },
            handleScroll: false,
            handleScale: false,
            crosshair: {
                mode: LightweightCharts.CrosshairMode.Magnet,
                horzLine: { visible: false, labelVisible: false },
                vertLine: { labelVisible: false },
            },
        });

        const state = {
            chart,
            dotnetRef,
            seriesByTier: new Map(),
            monthIndex: new Map(),
            lastShape: null,
            onResize: () => chart.applyOptions({ width: el.clientWidth }),
        };
        instances.set(elementId, state);

        chart.subscribeCrosshairMove((param) => {
            const key = timeKey(param.time);
            const idx = key != null && state.monthIndex.has(key) ? state.monthIndex.get(key) : null;
            dotnetRef.invokeMethodAsync("OnCrosshairMonth", idx);
        });

        window.addEventListener("resize", state.onResize);
    }

    function addLine(chart, color, lineWidth, dashed) {
        return chart.addSeries(LightweightCharts.LineSeries, {
            color,
            lineWidth,
            lineStyle: dashed ? LightweightCharts.LineStyle.Dashed : LightweightCharts.LineStyle.Solid,
            priceLineVisible: false,
            lastValueVisible: false,
            crosshairMarkerVisible: true,
        });
    }

    function toLwcPoint(p) {
        return p.value == null ? { time: p.time } : { time: p.time, value: p.value };
    }

    function setData(elementId, shape) {
        const state = instances.get(elementId);
        if (!state) {
            return;
        }
        const { chart } = state;

        for (const s of state.seriesByTier.values()) {
            chart.removeSeries(s.main);
            if (s.dash) {
                chart.removeSeries(s.dash);
            }
            if (s.markers) {
                chart.removeSeries(s.markers);
            }
        }
        state.seriesByTier.clear();
        state.monthIndex.clear();
        state.lastShape = shape;

        // C1 fix: index by insertion order into the Map itself (state.monthIndex.size), not a
        // separately-incremented counter. The dashed tail's first point always duplicates the
        // last closed month's time (setData() re-notes it) -- the old counter advanced on that
        // duplicate anyway, so the current month (the tail's second point) landed one slot past
        // its true index. Skipping an already-seen time now costs nothing, so duplicates from
        // repeated series (every visible tier shares the same 11 closed months) or a dashed tail
        // never inflate the index.
        const noteTime = (time) => {
            if (!state.monthIndex.has(time)) {
                state.monthIndex.set(time, state.monthIndex.size);
            }
        };

        for (const series of shape.series) {
            const color = resolveColor(series.color);

            const main = addLine(chart, color, series.lineWidth, false);
            main.setData(series.points.map((p) => { noteTime(p.time); return toLwcPoint(p); }));

            let dash = null;
            if (series.dashedTail) {
                dash = addLine(chart, color, series.lineWidth, true);
                dash.setData(series.dashedTail.map((p) => { noteTime(p.time); return toLwcPoint(p); }));
            }

            let markers = null;
            if (series.isolatedPoints && series.isolatedPoints.length > 0) {
                markers = chart.addSeries(LightweightCharts.LineSeries, {
                    color,
                    lineWidth: 1,
                    lineVisible: false,
                    pointMarkersVisible: true,
                    pointMarkersRadius: 3,
                    priceLineVisible: false,
                    lastValueVisible: false,
                    crosshairMarkerVisible: false,
                });
                markers.setData(series.isolatedPoints.map((p) => toLwcPoint(p)));
            }

            state.seriesByTier.set(series.tier, { main, dash, markers });
        }

        chart.timeScale().fitContent();
    }

    function dotY(elementId, tier, value) {
        const state = instances.get(elementId);
        if (!state) {
            return null;
        }
        const s = state.seriesByTier.get(tier);
        if (!s) {
            return null;
        }
        const y = s.main.priceToCoordinate(value);
        return y == null ? null : y;
    }

    function applyTheme(elementId) {
        const state = instances.get(elementId);
        if (!state) {
            return;
        }
        state.chart.applyOptions({ layout: { textColor: resolveColor("--ink") } });
        if (!state.lastShape) {
            return;
        }
        for (const series of state.lastShape.series) {
            const s = state.seriesByTier.get(series.tier);
            if (!s) {
                continue;
            }
            const color = resolveColor(series.color);
            s.main.applyOptions({ color });
            if (s.dash) {
                s.dash.applyOptions({ color });
            }
            if (s.markers) {
                s.markers.applyOptions({ color });
            }
        }
    }

    function dispose(elementId) {
        const state = instances.get(elementId);
        if (!state) {
            return;
        }
        window.removeEventListener("resize", state.onResize);
        state.chart.remove();
        instances.delete(elementId);
    }

    return { init, setData, dotY, applyTheme, dispose };
})();
