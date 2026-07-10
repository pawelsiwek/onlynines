// Gauge mount animation: fill width + knob left animate 0 → data-value when in view.
(function () {
    var animate = function (gauge) {
        if (gauge.dataset.animated) return;
        gauge.dataset.animated = "1";
        var value = gauge.getAttribute("data-value") + "%";
        var fill = gauge.querySelector(".gauge-fill");
        var knob = gauge.querySelector(".gauge-knob");
        setTimeout(function () {
            if (fill) fill.style.width = value;
            if (knob) knob.style.left = value;
        }, 260);
    };

    var observer = "IntersectionObserver" in window
        ? new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    animate(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.4 })
        : null;

    var scan = function () {
        document.querySelectorAll("[data-gauge]:not([data-animated])").forEach(function (g) {
            if (observer) observer.observe(g); else animate(g);
        });
    };

    // Called by Blazor components after interactive renders; also runs on initial load.
    window.onlyninesRefreshGauges = scan;
    scan();
})();

window.onlyninesCopyText = function (text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        return navigator.clipboard.writeText(text);
    }
    // http://localhost fallback (Clipboard API requires a secure context)
    var ta = document.createElement("textarea");
    ta.value = text;
    ta.style.position = "fixed";
    ta.style.opacity = "0";
    document.body.appendChild(ta);
    ta.select();
    document.execCommand("copy");
    document.body.removeChild(ta);
};
