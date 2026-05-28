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

        private async void OnStateChanged()
        {
            await ReloadData();
        }

        private async Task ReloadData()
        {
            _loading = true;
            await LoadData();
            _loading = false;

            await InvokeAsync(StateHasChanged);
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