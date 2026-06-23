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
        private Transition transition = Transition.Fade;
        private bool _loading = true;

        // Referencia al carrusel hero para avanzar slides manualmente
        private MudCarousel<object>? _heroCarousel;

        // Timer que reemplaza AutoCycle — controlado 100% por Blazor
        private System.Threading.Timer? _cycleTimer;
        private bool _disposed = false;

        protected override async Task OnInitializedAsync()
        {
            State.OnChange += OnStateChanged;
            await LoadData();
            _loading = false;
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_disposed)
            {
                // Solo arranca el timer DESPUÉS del primer render completo.
                // A diferencia del AutoCycle de MudCarousel que arranca
                // en OnAfterRender compitiendo con los datos que cargan,
                // este timer empieza cuando la página ya está estable.
                _cycleTimer = new System.Threading.Timer(
                    callback: _ => AdvanceSlide(),
                    state: null,
                    dueTime: TimeSpan.FromSeconds(16),
                    period: TimeSpan.FromSeconds(16)
                );
            }
            return Task.CompletedTask;
        }

        private void AdvanceSlide()
        {
            if (_disposed || _heroCarousel == null) return;
            InvokeAsync(() =>
            {
                try
                {
                    _heroCarousel?.Next();
                    StateHasChanged();
                }
                catch { /* circuit cerrado — ignorar */ }
            });
        }

        private void OnStateChanged()
        {
            InvokeAsync(async () =>
            {
                _loading = true;
                StateHasChanged();
                await LoadData();
                _loading = false;
                StateHasChanged();
            });
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
            _disposed = true;
            _cycleTimer?.Dispose();
            _cycleTimer = null;
            State.OnChange -= OnStateChanged;
        }
    }
}
