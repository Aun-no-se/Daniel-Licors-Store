using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Daniel_Licors_Store.Components.Shared
{
    public partial class MiniCarousel<TItem>
    {
        [Parameter] public List<TItem> Items { get; set; } = new();
        [Parameter] public RenderFragment<TItem> ItemTemplate { get; set; } = default!;
        [Parameter] public string StageHeight { get; set; } = "360px";

        // ── Carrusel: índice activo ─────────────────────────────────────────
        private int _activeIndex = 0;

        // ── Touch / drag ─────────────────────────────────────────────────────
        private double _touchStartX = 0;
        private double _mouseStartX = 0;
        private bool _isDragging = false;
        private const double SwipeThreshold = 40;

        // Touch
        private void OnTouchStart(TouchEventArgs e)
        {
            if (e.Touches.Length > 0)
                _touchStartX = e.Touches[0].ClientX;
        }

        private void OnTouchEnd(TouchEventArgs e)
        {
            if (e.ChangedTouches.Length == 0) return;
            double delta = e.ChangedTouches[0].ClientX - _touchStartX;
            if (Math.Abs(delta) >= SwipeThreshold)
            {
                if (delta < 0) Next();
                else Prev();
            }
        }

        // Mouse drag (para probar en escritorio)
        private void OnMouseDown(MouseEventArgs e)
        {
            _mouseStartX = e.ClientX;
            _isDragging = true;
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            double delta = e.ClientX - _mouseStartX;
            if (Math.Abs(delta) >= SwipeThreshold)
            {
                if (delta < 0) Next();
                else Prev();
            }
        }

        private void OnMouseLeave(MouseEventArgs e)
        {
            _isDragging = false;
        }

        // ── Lógica del carrusel infinito ─────────────────────────────────────
        private int GetPosition(int index)
        {
            if (Items == null || Items.Count == 0) return 99;
            int count = Items.Count;
            int raw = index - _activeIndex;
            while (raw > count / 2) raw -= count;
            while (raw < -count / 2) raw += count;
            return raw;
        }

        private string GetCardClass(int pos) => pos switch
        {
            0 => "mc-pos-center",
            1 => "mc-pos-right1",
            -1 => "mc-pos-left1",
            _ => "mc-pos-hidden"
        };

        private void Next()
        {
            if (Items == null || Items.Count == 0) return;
            _activeIndex = (_activeIndex + 1) % Items.Count;
        }

        private void Prev()
        {
            if (Items == null || Items.Count == 0) return;
            _activeIndex = (_activeIndex - 1 + Items.Count) % Items.Count;
        }

        private void GoTo(int index) => _activeIndex = index;

        // Clic sobre una tarjeta que NO está centrada: solo la centra.
        // Nunca navega — navegar es responsabilidad exclusiva del botón
        // "Ver detalle" dentro del ItemTemplate.
        private void CardClicked(int i)
        {
            if (i != _activeIndex) _activeIndex = i;
        }
    }
}
