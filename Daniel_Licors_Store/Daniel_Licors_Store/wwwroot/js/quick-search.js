/* ==========================================================================
   quick-search.js — apoyo del buscador rápido (Daniel Liquor Store)

   Qué hace:
   - prime(): en iPhone/Android el teclado solo sale si el foco se pide DENTRO del
     toque del cliente. Como el panel lo dibuja Blazor un instante después, aquí se
     enfoca un input invisible en el mismo toque y luego el foco pasa al campo real.
   - lock(): bloquea el scroll de la página de fondo mientras el panel está abierto y
     ajusta el alto del panel al área visible (así el teclado no tapa los resultados).
   - registerHotkeys(): "/" o Ctrl/Cmd+K abren y cierran el buscador en PC.
   Nada de esto toca el scroll táctil del resto de la tienda.
   ========================================================================== */
(function () {
    'use strict';

    if (window.quickSearch) return;

    var primeEl = null;
    var locked = false;
    var savedHtmlOverflow = '';
    var savedBodyOverflow = '';
    var hotkeysReady = false;

    function removePrime() {
        if (primeEl && primeEl.parentNode) primeEl.parentNode.removeChild(primeEl);
        primeEl = null;
    }

    function prime() {
        try {
            if (document.getElementById('qs-input') || primeEl) return;
            primeEl = document.createElement('input');
            primeEl.type = 'text';
            primeEl.tabIndex = -1;
            primeEl.setAttribute('aria-hidden', 'true');
            primeEl.style.cssText = 'position:fixed;top:0;left:0;width:1px;height:1px;opacity:0;' +
                'font-size:16px;border:0;padding:0;pointer-events:none;';
            document.body.appendChild(primeEl);
            primeEl.focus({ preventScroll: true });
            setTimeout(removePrime, 2500);
        } catch (e) { /* sin teclado previo: el foco normal igual funciona en PC */ }
    }

    // Capturando: corre dentro del mismo toque, antes que Blazor
    document.addEventListener('click', function (e) {
        var t = e.target;
        if (t && t.closest && t.closest('[data-qs-open]')) prime();
    }, true);

    function syncViewport() {
        var vv = window.visualViewport;
        if (!vv) return;
        var root = document.documentElement.style;
        root.setProperty('--qs-vh', Math.round(vv.height) + 'px');
        root.setProperty('--qs-top', Math.round(vv.offsetTop) + 'px');
    }

    function unsyncViewport() {
        var root = document.documentElement.style;
        root.removeProperty('--qs-vh');
        root.removeProperty('--qs-top');
    }

    window.quickSearch = {
        focus: function (id) {
            var tries = 0;
            (function attempt() {
                var el = document.getElementById(id);
                if (el) {
                    el.focus({ preventScroll: true });
                    removePrime(); // el foco ya está en el campo real: el teclado se mantiene
                    return;
                }
                if (tries++ < 30) requestAnimationFrame(attempt);
            })();
        },

        setValue: function (id, value) {
            var el = document.getElementById(id);
            if (!el) return;
            el.value = value || '';
            el.focus({ preventScroll: true });
            try { el.setSelectionRange(el.value.length, el.value.length); } catch (e) { }
        },

        lock: function (on) {
            var vv = window.visualViewport;
            if (on && !locked) {
                locked = true;
                savedHtmlOverflow = document.documentElement.style.overflow;
                savedBodyOverflow = document.body.style.overflow;
                document.documentElement.style.overflow = 'hidden';
                document.body.style.overflow = 'hidden';
                if (vv) {
                    vv.addEventListener('resize', syncViewport);
                    vv.addEventListener('scroll', syncViewport);
                    syncViewport();
                }
            } else if (!on && locked) {
                locked = false;
                document.documentElement.style.overflow = savedHtmlOverflow;
                document.body.style.overflow = savedBodyOverflow;
                if (vv) {
                    vv.removeEventListener('resize', syncViewport);
                    vv.removeEventListener('scroll', syncViewport);
                }
                unsyncViewport();
                removePrime();
            }
        },

        scrollActive: function () {
            var el = document.querySelector('.qs-row-active');
            if (el && el.scrollIntoView) el.scrollIntoView({ block: 'nearest' });
        },

        registerHotkeys: function (dotNetRef) {
            if (hotkeysReady) return;
            hotkeysReady = true;

            document.addEventListener('keydown', function (e) {
                var t = e.target;
                var typing = t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' ||
                    t.tagName === 'SELECT' || t.isContentEditable);
                var slash = e.key === '/' && !typing && !e.ctrlKey && !e.metaKey && !e.altKey;
                var ctrlK = (e.ctrlKey || e.metaKey) && !e.altKey && String(e.key).toLowerCase() === 'k';

                if (slash || ctrlK) {
                    e.preventDefault();
                    dotNetRef.invokeMethodAsync('ToggleFromHotkey');
                }
            });
        }
    };
})();
