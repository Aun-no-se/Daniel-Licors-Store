/* ==========================================================================
   image-reveal.js — Cursor Reveal Effect
   Daniel Liquor Store

   Qué hace:
   Cada elemento con el atributo [data-reveal] muestra por defecto su imagen
   "antes". Al pasar el cursor, la imagen "después" se descubre dentro de un
   círculo suave que sigue al mouse (con inercia). Al salir, el círculo se
   cierra solo.

   Cómo funciona (a propósito sin JS interop de Blazor):
   - Un único loop global busca los [data-reveal] que existan EN ESE MOMENTO.
     Así funciona con cada slide nuevo que MudCarousel crea al cambiar, sin
     enganchar listeners por slide ni coordinar nada con el circuito de Blazor.
   - Solo escribe variables CSS (--reveal-x, --reveal-y, --reveal-r,
     --reveal-max, --reveal-s) en el slide. El diseño vive en Index.razor.css.
   - Si el slide cambia con el cursor quieto encima, el reveal se activa
     solo (un chequeo cada 250 ms).
   - En pantallas táctiles (sin hover) el círculo hace un recorrido suave
     automático para que la imagen "después" también se vea.
   - Respeta prefers-reduced-motion.
   ========================================================================== */
(function () {
    'use strict';

    if (window.__licImageReveal) return;
    window.__licImageReveal = true;

    var SELECTOR = '[data-reveal]';
    var finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

    var pointer = { x: 0, y: 0, inside: false };
    var states = new WeakMap();
    var rafId = 0;
    var lastTime = 0;

    function clamp(v, lo, hi) {
        return Math.min(hi, Math.max(lo, v));
    }

    function stateFor(el) {
        var s = states.get(el);
        if (!s) {
            s = {
                x: 0, y: 0, tx: 0, ty: 0,
                r: 0, tr: 0, max: 0,
                hasPos: false,
                wx: NaN, wy: NaN, wr: NaN, ws: NaN, wmax: NaN
            };
            states.set(el, s);
        }
        return s;
    }

    function write(el, s) {
        var st = el.style;
        var x = Math.round(s.x * 10) / 10;
        var y = Math.round(s.y * 10) / 10;
        var r = Math.max(Math.round(s.r * 10) / 10, 0.1);
        var sc = s.max > 0 ? Math.round((s.r / s.max) * 1000) / 1000 : 0;

        if (s.max !== s.wmax) { st.setProperty('--reveal-max', s.max + 'px'); s.wmax = s.max; }
        if (x !== s.wx) { st.setProperty('--reveal-x', x + 'px'); s.wx = x; }
        if (y !== s.wy) { st.setProperty('--reveal-y', y + 'px'); s.wy = y; }
        if (r !== s.wr) { st.setProperty('--reveal-r', r + 'px'); s.wr = r; }
        if (sc !== s.ws) { st.setProperty('--reveal-s', String(sc)); s.ws = sc; }
    }

    function frame(now) {
        rafId = 0;

        var slides = document.querySelectorAll(SELECTOR);
        if (!slides.length) { lastTime = 0; return; }

        var dt = lastTime ? Math.min(now - lastTime, 64) : 16.7;
        lastTime = now;

        var fine = finePointer.matches;
        var reduce = reduceMotion.matches;
        var follow = reduce ? 1 : 1 - Math.pow(1 - 0.2, dt / 16.7);
        var grow = reduce ? 1 : 1 - Math.pow(1 - 0.12, dt / 16.7);

        // Slide que está debajo del cursor ahora mismo (el activo siempre va arriba)
        var hit = null;
        if (fine && pointer.inside) {
            var under = document.elementFromPoint(pointer.x, pointer.y);
            hit = under && under.closest ? under.closest(SELECTOR) : null;
        }

        var busy = false;

        for (var i = 0; i < slides.length; i++) {
            var el = slides[i];
            var s = stateFor(el);
            var rect = el.getBoundingClientRect();
            var visible = rect.bottom > 0 && rect.top < window.innerHeight;

            s.max = clamp(Math.min(rect.width, rect.height) * 0.34, 150, 300);

            if (fine) {
                if (el === hit) {
                    var px = pointer.x - rect.left;
                    var py = pointer.y - rect.top;
                    if (!s.hasPos) { s.x = px; s.y = py; s.hasPos = true; }
                    s.tx = px;
                    s.ty = py;
                    s.tr = s.max;
                } else {
                    s.tr = 0;
                }
            } else if (visible) {
                // Táctil: recorrido automático suave (estático si hay reduced-motion)
                var t = reduce ? 0 : now / 1000;
                var ax = rect.width * (0.5 + 0.28 * Math.sin(t * 0.7));
                var ay = rect.height * (0.5 + 0.22 * Math.sin(t * 1.1 + 1.3));
                if (!s.hasPos) { s.x = ax; s.y = ay; s.hasPos = true; }
                s.tx = ax;
                s.ty = ay;
                s.tr = s.max * 0.9;
            } else {
                s.tr = 0;
            }

            s.x += (s.tx - s.x) * follow;
            s.y += (s.ty - s.y) * follow;
            s.r += (s.tr - s.r) * grow;

            if (Math.abs(s.tx - s.x) < 0.1) s.x = s.tx;
            if (Math.abs(s.ty - s.y) < 0.1) s.y = s.ty;
            if (Math.abs(s.tr - s.r) < 0.4) s.r = s.tr;

            // Cerrado del todo: la próxima vez arranca directo en el cursor
            if (s.tr === 0 && s.r === 0) s.hasPos = false;

            write(el, s);

            if (s.x !== s.tx || s.y !== s.ty || s.r !== s.tr) busy = true;
        }

        if (busy) rafId = requestAnimationFrame(frame);
    }

    function wake() {
        if (!rafId) rafId = requestAnimationFrame(frame);
    }

    document.addEventListener('pointermove', function (e) {
        if (e.pointerType === 'touch') return;
        pointer.x = e.clientX;
        pointer.y = e.clientY;
        pointer.inside = true;
        wake();
    }, { passive: true });

    document.documentElement.addEventListener('mouseleave', function () {
        pointer.inside = false;
        wake();
    });

    window.addEventListener('blur', function () {
        pointer.inside = false;
        wake();
    });

    window.addEventListener('scroll', wake, { passive: true });
    window.addEventListener('resize', wake);
    document.addEventListener('visibilitychange', wake);

    // Detecta slides nuevos (cambio de carrusel) aunque el cursor esté quieto
    setInterval(wake, 250);
    wake();
})();