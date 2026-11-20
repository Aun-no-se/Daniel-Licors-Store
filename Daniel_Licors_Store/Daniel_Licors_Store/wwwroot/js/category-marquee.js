/* ==========================================================================
   category-marquee.js — Barra infinita (marquee) de categorías y marcas
   Daniel Liquor Store

   Qué hace:
   - Mueve la barra [data-marquee] en bucle infinito y continuo.
   - Se pausa con el cursor encima (escritorio) y mientras hay un dedo/clic
     sostenido; al soltar, retoma suave.
   - Arrastre con mouse o dedo (con inercia), trackpad horizontal y teclado.
     El scroll vertical de la página sigue funcionando con el dedo.
   - Clic en un chip: scroll suave al elemento de la página cuyo id sea el
     data-target del chip ("categoria-5", "marca-3"…). Si esa sección aún
     no existe, el chip hace una pequeña sacudida.

   Cómo se integra con Blazor:
   - Blazor pinta el grupo de chips DOS veces (el 2º es copia aria-hidden).
     JS solo mueve el track con transform: no inserta ni quita nodos dentro
     del DOM de Blazor (evita el error insertBefore).
   - Un escaneo cada 500 ms inicializa las barras nuevas y cada instancia se
     apaga sola si su elemento sale del DOM.
   ========================================================================== */
(function () {
    'use strict';

    if (window.__licMarquee) return;
    window.__licMarquee = true;

    var BASE_SPEED = 46;          // px por segundo (hacia la izquierda)
    var DRAG_THRESHOLD = 6;       // px antes de considerar que es arrastre
    var RESUME_DELAY = 700;       // ms de espera tras soltar antes de retomar
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

    function clamp(v, lo, hi) { return Math.min(hi, Math.max(lo, v)); }

    function shake(el) {
        if (!el.animate || reduceMotion.matches) return;
        el.animate([
            { transform: 'translateX(0)' },
            { transform: 'translateX(-5px)' },
            { transform: 'translateX(5px)' },
            { transform: 'translateX(-3px)' },
            { transform: 'translateX(0)' }
        ], { duration: 360, easing: 'ease-in-out' });
    }

    function goTo(chip) {
        var id = chip.getAttribute('data-target');
        var target = id ? document.getElementById(id) : null;

        if (!target) { shake(chip); return; }

        var bar = document.querySelector('.topbar');
        var offset = (bar ? bar.getBoundingClientRect().height : 72) + 12;
        target.style.scrollMarginTop = offset + 'px';
        target.scrollIntoView({ behavior: reduceMotion.matches ? 'auto' : 'smooth', block: 'start' });

        if (target.animate && !reduceMotion.matches) {
            target.animate([
                { boxShadow: 'inset 0 0 0 0 rgba(232,196,104,0)' },
                { boxShadow: 'inset 0 0 140px 0 rgba(232,196,104,0.14)', offset: 0.35 },
                { boxShadow: 'inset 0 0 0 0 rgba(232,196,104,0)' }
            ], { duration: 1600, easing: 'ease-out' });
        }
    }

    function init(vp) {
        if (vp.__licMarqueeInit) return;

        var track = vp.querySelector('.lic-marquee-track');
        var sets = track ? track.querySelectorAll('.lic-marquee-set') : null;
        if (!track || !sets || sets.length < 2) return;
        vp.__licMarqueeInit = true;

        var x = 0;                 // desplazamiento actual (≤ 0)
        var period = 0;            // ancho de un grupo (distancia entre copias)
        var vel = 0;               // velocidad por inercia (px/s)
        var auto = 1;              // 0..1, qué tanto avanza solo (suaviza pausa/retoma)
        var hover = false;
        var focused = false;
        var pressed = false;       // dedo/clic sostenido
        var dragging = false;
        var moved = false;
        var suppressClick = false;
        var resumeAt = 0;
        var visible = true;
        var pid = null;
        var startX = 0, lastX = 0, lastMoveT = 0, dragVel = 0;
        var last = 0;
        var rafId = 0;
        var dead = false;

        function measure() {
            period = sets[1].offsetLeft - sets[0].offsetLeft;
        }
        measure();

        var ro = null;
        if (window.ResizeObserver) {
            ro = new ResizeObserver(measure);
            ro.observe(sets[0]);
        }

        var io = null;
        if (window.IntersectionObserver) {
            io = new IntersectionObserver(function (entries) {
                visible = entries[0].isIntersecting;
            });
            io.observe(vp);
        }

        function wrap() {
            if (period > 0) {
                x = x % period;
                if (x > 0) x -= period;
            }
        }

        function render() {
            track.style.transform = 'translate3d(' + x.toFixed(2) + 'px,0,0)';
        }

        function destroy() {
            dead = true;
            if (rafId) cancelAnimationFrame(rafId);
            if (ro) ro.disconnect();
            if (io) io.disconnect();
        }

        function frame(t) {
            rafId = 0;
            if (dead) return;
            if (!vp.isConnected) { destroy(); return; }

            var dt = last ? Math.min((t - last) / 1000, 0.05) : 0.016;
            last = t;

            var canAuto = !hover && !focused && !pressed && !dragging &&
                          !reduceMotion.matches && performance.now() >= resumeAt;
            var targetAuto = canAuto ? 1 : 0;
            auto += (targetAuto - auto) * (1 - Math.exp(-dt * (targetAuto ? 3 : 9)));

            if (!dragging && visible) {
                x += (-BASE_SPEED * auto + vel) * dt;
                vel *= Math.exp(-dt * 2.6);
                if (Math.abs(vel) < 2) vel = 0;
            }

            wrap();
            render();
            rafId = requestAnimationFrame(frame);
        }

        /* ── Pointer: mouse y dedo ───────────────────────────────────── */
        vp.addEventListener('pointerdown', function (e) {
            if (e.pointerType === 'mouse' && e.button !== 0) return;
            pressed = true;
            moved = false;
            pid = e.pointerId;
            startX = lastX = e.clientX;
            lastMoveT = performance.now();
            dragVel = 0;
            vel = 0;
        });

        vp.addEventListener('pointermove', function (e) {
            if (!pressed || e.pointerId !== pid) return;

            if (!moved) {
                if (Math.abs(e.clientX - startX) < DRAG_THRESHOLD) return;
                moved = true;
                dragging = true;
                vp.classList.add('is-dragging');
                // Capturamos solo al empezar a arrastrar: un simple clic sigue
                // llegando al chip con normalidad.
                try { vp.setPointerCapture(pid); } catch (_) { }
                lastX = e.clientX;
                lastMoveT = performance.now();
                return;
            }

            var now = performance.now();
            var dx = e.clientX - lastX;
            var dtm = Math.max(now - lastMoveT, 1) / 1000;
            x += dx;
            dragVel = dragVel * 0.6 + (dx / dtm) * 0.4;
            lastX = e.clientX;
            lastMoveT = now;
            wrap();
            render();
        });

        function release(e) {
            if (!pressed) return;
            if (e && e.pointerId !== pid) return;
            pressed = false;

            if (moved) {
                suppressClick = true;
                setTimeout(function () { suppressClick = false; }, 80);
                vel = clamp(dragVel, -2800, 2800);
            }
            dragging = false;
            moved = false;
            vp.classList.remove('is-dragging');
            try { vp.releasePointerCapture(pid); } catch (_) { }
            resumeAt = performance.now() + RESUME_DELAY;
        }

        vp.addEventListener('pointerup', release);
        vp.addEventListener('pointercancel', release);

        /* ── Hover (solo mouse): pausa ───────────────────────────────── */
        vp.addEventListener('pointerenter', function (e) {
            if (e.pointerType === 'mouse') hover = true;
        });
        vp.addEventListener('pointerleave', function (e) {
            if (e.pointerType === 'mouse') {
                hover = false;
                resumeAt = performance.now() + 250;
            }
        });

        /* ── Click: ir a la sección (o ignorar si fue arrastre) ──────── */
        vp.addEventListener('click', function (e) {
            if (suppressClick) {
                e.preventDefault();
                e.stopPropagation();
                suppressClick = false;
                return;
            }
            var chip = e.target.closest ? e.target.closest('[data-target]') : null;
            if (chip) goTo(chip);
        }, true);

        /* ── Trackpad / rueda horizontal ─────────────────────────────── */
        vp.addEventListener('wheel', function (e) {
            if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) {
                e.preventDefault();
                x -= e.deltaX;
                wrap();
                render();
                resumeAt = performance.now() + 900;
            }
        }, { passive: false });

        /* ── Teclado: el chip enfocado se mantiene a la vista ────────── */
        vp.addEventListener('focusin', function (e) {
            focused = true;
            vp.scrollLeft = 0;
            var r = e.target.getBoundingClientRect();
            var v = vp.getBoundingClientRect();
            var pad = 60;
            if (r.left < v.left + pad) x += (v.left + pad - r.left);
            else if (r.right > v.right - pad) x -= (r.right - (v.right - pad));
            wrap();
            render();
        });
        vp.addEventListener('focusout', function () {
            focused = false;
            resumeAt = performance.now() + RESUME_DELAY;
        });
        vp.addEventListener('scroll', function () { vp.scrollLeft = 0; });

        rafId = requestAnimationFrame(frame);
    }

    function scan() {
        var bars = document.querySelectorAll('[data-marquee]');
        for (var i = 0; i < bars.length; i++) init(bars[i]);
    }

    setInterval(scan, 500);
    if (document.readyState !== 'loading') scan();
    else document.addEventListener('DOMContentLoaded', scan);
})();
