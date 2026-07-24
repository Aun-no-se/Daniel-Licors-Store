using Microsoft.AspNetCore.Components;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class StockGeneral : AdminPageBase
    {
        protected override string[] AllowedRoles => new[] { "Admin", "Empleado" };

        [Inject] public StockGeneralService StockSvc { get; set; } = default!;

        bool _loading = true;
        int _tabIndex = 0;
        string? _search;
        int? _catFilter;

        StockSummary _summary = new();
        List<StockProductRow> _productos = new();
        List<StockProductRow> _productosFiltered = new();
        List<StockPromoRow> _promos = new();
        List<StockComboRow> _combos = new();
        List<(string Name, int Id)> _categorias = new();

        // El chequeo de auth ya lo hizo AdminPageBase antes de llegar aquí.
        protected override async Task OnInitializedAuthorizedAsync()
        {
            await RecargarTodo();
        }

        async Task RecargarTodo()
        {
            _loading = true;
            StateHasChanged();

            _summary = await StockSvc.GetSummaryAsync();
            _productos = await StockSvc.GetProductsAsync();
            _promos = await StockSvc.GetPromotionsAsync();
            _combos = await StockSvc.GetCombosAsync();
            _categorias = await StockSvc.GetCategoriesAsync();

            _productosFiltered = _productos;
            _loading = false;
        }

        void FiltrarProductos()
        {
            _productosFiltered = _productos
                .Where(p => (string.IsNullOrWhiteSpace(_search) ||
                             p.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                             (p.Brand != null && p.Brand.Contains(_search, StringComparison.OrdinalIgnoreCase)))
                         && (!_catFilter.HasValue || p.CategoryId == _catFilter.Value))
                .ToList();
        }

        void OnCatChanged(int? val) { _catFilter = val; FiltrarProductos(); }

        string RowClass(StockProductRow row, int _)
            => row.Stock == 0 ? "sg-row-agotado" : row.Stock <= 5 ? "sg-row-low" : "";
    }
}
