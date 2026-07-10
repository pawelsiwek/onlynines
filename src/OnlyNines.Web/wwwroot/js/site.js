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

// Interactive hero gauge: drag/click recomputes SLA <-> nines live. The dial IS the pitch.
(function () {
    var setup = function (gauge) {
        var card = gauge.closest(".gauge-card");
        if (!card) return;
        var track = gauge.querySelector(".gauge-track");
        var fill = gauge.querySelector(".gauge-fill");
        var knob = gauge.querySelector(".gauge-knob");
        var figure = card.querySelector(".gauge-figure");
        var sub = card.querySelector(".gauge-subfigure");

        var fmtPercent = function (sla) {
            // Never display 100%: precision grows instead of rounding up.
            var t = (sla * 100).toFixed(3);
            if (t.indexOf("100") === 0) t = (sla * 100).toFixed(6);
            return t.replace(/0+$/, "").replace(/\.$/, "");
        };
        var fmtDowntime = function (hours) {
            if (hours >= 1) return hours.toFixed(1) + " h/yr";
            if (hours >= 1 / 60) return (hours * 60).toFixed(1) + " min/yr";
            return (hours * 3600).toFixed(0) + " s/yr";
        };

        var set = function (clientX) {
            var rect = track.getBoundingClientRect();
            var p = Math.min(1, Math.max(0, (clientX - rect.left) / rect.width));
            var nines = 1 + 4 * p;
            var sla = 1 - Math.pow(10, -nines);
            var pos = (p * 100) + "%";
            fill.style.width = pos;
            knob.style.left = pos;
            figure.innerHTML = fmtPercent(sla) + '<span class="accent">%</span>';
            sub.textContent = "≈ " + fmtDowntime((1 - sla) * 8760) +
                " downtime budget · " + nines.toFixed(1) + " nines";
        };

        track.addEventListener("pointerdown", function (e) {
            e.preventDefault();
            gauge.classList.add("gauge-dragging");
            set(e.clientX);
            var move = function (ev) { set(ev.clientX); };
            var up = function () {
                gauge.classList.remove("gauge-dragging");
                window.removeEventListener("pointermove", move);
                window.removeEventListener("pointerup", up);
            };
            window.addEventListener("pointermove", move);
            window.addEventListener("pointerup", up);
        });
    };

    document.querySelectorAll("[data-gauge-interactive]").forEach(setup);
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
