using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class Index : IDisposable
    {
        [Inject] private ICatalogService CatalogService { get; set; } = default!;
        [Inject] private ProductStateService State { get; set; } = default!;

        private List<ProductResponseModel> cervezas = new();
        private List<ProductResponseModel> rones = new();

        private bool arrows = true;
        private bool bullets = true;
        private bool enableSwipeGesture = true;
        private bool autocycle = true;
        private Transition transition = Transition.Fade;

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            State.OnChange += OnStateChanged;
            await LoadData();
            _loading = false;
        }

        // FIX CRÍTICO: Era "async void" — eso hace que las excepciones
        // no sean capturadas y el circuit Blazor se rompe sin posibilidad
        // de recovery. Cambiar a Task no funciona directo en un event handler
        // de Action, así que usamos InvokeAsync para volver al hilo del circuit.
        private void OnStateChanged()
        {
            InvokeAsync(async () =>
            {
                await ReloadData();
            });
        }

        private async Task ReloadData()
        {
            _loading = true;
            StateHasChanged(); // Notifica el estado de carga primero

            await LoadData();

            _loading = false;
            StateHasChanged();
        }

        private async Task LoadData()
        {
            cervezas = await CatalogService.GetProductsByCategoryAsync(5);
            rones = await CatalogService.GetProductsByCategoryAsync(1);
        }

        private void PedirDemo() { }
        private void VerMasDemo() { }

        public void Dispose()
        {
            State.OnChange -= OnStateChanged;
        }
    }
}
