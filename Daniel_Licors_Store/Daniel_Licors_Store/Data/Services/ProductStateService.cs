using System;
using Daniel_Licors_Store.Data.Services;

namespace Daniel_Licors_Store.Services
{
    public class ProductStateService
    {
        public event Action? OnChange;

        public void NotifyChange()
        {
            // El buscador comparte su índice entre clientes: se refresca al instante cuando el admin cambia una bebida.
            ProductSearchService.Invalidate();

            OnChange?.Invoke();
        }
    }
}