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
        [Inject] private IWebHostEnvironment Env { get; set; } = default!;

        // Secciones del index (una por categoría y marca registrada), armadas desde la BD.
        private List<CatalogSectionModel> _sections = new();

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
        private string heroLottieUrl = "animations/wine-bottle-animation.json";

        // Imágenes del carrusel (wwwroot/images/Carrusel).
        // "Antes" se ve por defecto; "Después" se descubre con el cursor.
        // Se resuelven en LoadCarouselImages() para tolerar cualquier extensión.
        private string buchanaAntes = "";
        private string buchanaDespues = "";
        private string cristalinoAntes = "";
        private string cristalinoDespues = "";

        private static readonly string[] _carouselImageExts =
            { ".jpg", ".jpeg", ".png", ".webp", ".jfif", ".avif" };

        protected override async Task OnInitializedAsync()
        {
            State.OnChange += OnStateChanged;
            LoadCarouselImages();
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

        private void LoadCarouselImages()
        {
            buchanaAntes = ResolveCarouselImage("buchanaAntes");
            buchanaDespues = ResolveCarouselImage("buchanaDespues");
            cristalinoAntes = ResolveCarouselImage("cristalinoAntes");
            cristalinoDespues = ResolveCarouselImage("cristalinoDespues");
        }

        // Busca la imagen en wwwroot/images/Carrusel con la extensión que tenga
        // y le agrega ?v=<fecha de modificación> para que el navegador no
        // muestre una versión vieja si reemplazas el archivo.
        private string ResolveCarouselImage(string baseName)
        {
            var dir = Path.Combine(Env.WebRootPath, "images", "Carrusel");

            foreach (var ext in _carouselImageExts)
            {
                var full = Path.Combine(dir, baseName + ext);
                if (File.Exists(full))
                {
                    var v = File.GetLastWriteTimeUtc(full).Ticks;
                    return $"images/Carrusel/{baseName}{ext}?v={v}";
                }
            }

            return $"images/Carrusel/{baseName}.jpg";
        }

        private async Task LoadData()
        {
            _sections = await CatalogService.GetCatalogSectionsAsync();
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