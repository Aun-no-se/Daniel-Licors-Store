using Daniel_Licors_Store.Models.Combo;
using Microsoft.AspNetCore.Components;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class ComboDetail
    {
        [Parameter] public int ComboId { get; set; }

        ComboDetailDto? Combo;
        bool _loading = true;

        string ComboImage => Combo?.Products?.FirstOrDefault()?.ImageUrl
                     ?? "/images/combo-default.png";

        protected override async Task OnInitializedAsync()
        {
            await Load();
        }

        private async Task Load()
        {
            _loading = true;

            try
            {
                Combo = await ComboService.GetComboDetailAsync(ComboId);
            }
            finally
            {
                _loading = false;
            }
        }

        void GoToProduct(int productId)
        {
            Nav.NavigateTo($"/producto/{productId}");
        }
    }
}