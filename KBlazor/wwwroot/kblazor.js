// KBlazor FlexTable JavaScript
// Include via: <script src="_content/KBlazor/kblazor.js"></script>

var dotNetReference = null;

function ActivateTableResize(reference) {
    dotNetReference = reference;
    var thElm;
    var startOffset;

    Array.prototype.forEach.call(
        document.querySelectorAll(".resizeColumn"),
        function (th) {
            th.style.position = 'relative';

            var grip = document.createElement('div');
            grip.innerHTML = "&nbsp;";
            grip.style.top = 0;
            grip.style.right = 0;
            grip.style.bottom = 0;
            grip.style.width = '5px';
            grip.style.position = 'absolute';
            grip.style.cursor = 'col-resize';
            grip.style.userSelect = 'none';
            grip.addEventListener('mousedown', function (e) {
                thElm = th.firstElementChild;
                thElm.style.backgroundColor = '#43a047';
                thElm.style.color = '';
                startOffset = th.firstElementChild.clientWidth - e.pageX;
            });
            th.appendChild(grip);
        });

    document.addEventListener('mousemove', function (e) {
        if (thElm) {
            thElm.style.width = startOffset + e.pageX + 'px';
        }
    });

    document.addEventListener('mouseup', function () {
        if (thElm) {
            var width = thElm.style.width;
            thElm.style.backgroundColor = '';
            thElm.style.color = '';
            var id = thElm.id;
            dotNetReference.invokeMethodAsync("DivWidthChanged", width, id);
            thElm = undefined;
        }
    });
}

function GetComputedFont(elementId) {
    var elem = document.getElementById(elementId);
    var computedStyle = window.getComputedStyle(elem, null);
    var font = computedStyle.getPropertyValue("font-family").split(",")[0].replaceAll('"', '');
    var fontSize = computedStyle.getPropertyValue("font-size");
    return font + ", " + fontSize;
}

// ── Namespaced helpers (1.1.0+) ─────────────────────────────────────────
// Kept separate from the legacy globals above so host pages that already
// reference ActivateTableResize / GetComputedFont keep working.
window.KBlazor = window.KBlazor || {};

// Minutes to ADD to a UTC DateTime to get the browser's local time.
// JS getTimezoneOffset() is local→UTC (positive west of UTC), so negate it.
window.KBlazor.getTimezoneOffsetMinutes = function () {
    return -new Date().getTimezoneOffset();
};

// Measure an array of strings with a canvas, in CSS pixels.
// One call per auto-size; returns widths in the same order as `texts`.
window.KBlazor.measureText = function (texts, fontFamily, fontSizePx) {
    var canvas = document.createElement('canvas');
    var ctx = canvas.getContext('2d');
    ctx.font = fontSizePx + 'px "' + fontFamily + '"';
    return (texts || []).map(function (t) {
        return ctx.measureText(t == null ? '' : String(t)).width;
    });
};
