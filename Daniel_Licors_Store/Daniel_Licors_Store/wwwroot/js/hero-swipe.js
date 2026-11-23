/* hero-swipe.js — swipe horizontal del carrusel hero, sin bloquear el scroll vertical.
   Los listeners son pasivos y nunca llaman preventDefault(): el navegador sigue
   haciendo el scroll de la página con el dedo, y esto solo mira hacia dónde fue el gesto. */
(function () {
    'use strict';
    if (window.heroSwipe) return;

    var MIN_DX = 50;     // px mínimos de recorrido horizontal
    var RATIO = 1.5;     // el gesto debe ser claramente más horizontal que vertical
    var MAX_MS = 700;    // gestos más lentos se consideran scroll/arrastre, no swipe

    window.heroSwipe = {
        init: function (selector, dotNetRef) {
            var el = document.querySelector(selector);
            if (!el || el.dataset.swipeInit) return;
            el.dataset.swipeInit = 'true';

            var x0 = 0, y0 = 0, t0 = 0, tracking = false;

            el.addEventListener('touchstart', function (e) {
                if (e.touches.length !== 1) { tracking = false; return; }
                x0 = e.touches[0].clientX;
                y0 = e.touches[0].clientY;
                t0 = Date.now();
                tracking = true;
            }, { passive: true });

            el.addEventListener('touchend', function (e) {
                if (!tracking) return;
                tracking = false;
                var t = e.changedTouches[0];
                var dx = t.clientX - x0;
                var dy = t.clientY - y0;
                if (Date.now() - t0 > MAX_MS) return;
                if (Math.abs(dx) < MIN_DX || Math.abs(dx) < Math.abs(dy) * RATIO) return;
                dotNetRef.invokeMethodAsync(dx < 0 ? 'SwipeNext' : 'SwipePrevious');
            }, { passive: true });

            el.addEventListener('touchcancel', function () { tracking = false; }, { passive: true });
        }
    };
})();
