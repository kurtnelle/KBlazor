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
