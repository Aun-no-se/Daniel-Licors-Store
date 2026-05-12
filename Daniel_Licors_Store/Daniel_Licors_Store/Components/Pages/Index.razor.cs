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

        // 🔥 UI STATE (LO QUE TE FALTABA)
        private bool arrows = true;
        private bool bullets = true;
        private bool enableSwipeGesture = true;
        private bool autocycle = true;
        private Transition transition = Transition.Fade;

        protected override async Task OnInitializedAsync()
        {
            State.OnChange += ReloadData;
            await LoadData();
        }

        private async void ReloadData()
        {
            await LoadData();
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
            State.OnChange -= ReloadData;
        }
    }
}