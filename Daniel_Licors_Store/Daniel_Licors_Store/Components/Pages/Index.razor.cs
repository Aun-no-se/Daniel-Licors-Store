using Microsoft.AspNetCore.Components;
using MudBlazor;
using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class Index
    {
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private bool arrows = true;
        private bool bullets = true;
        private bool enableSwipeGesture = true;
        private bool autocycle = true;
        private Transition transition = Transition.Fade;

        // 🔥 DATA TEMPORAL (luego esto viene de API o DB)
        private List<ProductResponseModel> cervezas = new()
        {
            new() { ProductId = 1, Name = "Presidente", Category = "Cervezas", ImageUrl = "img/presidente.png" },
            new() { ProductId = 2, Name = "Corona", Category = "Cervezas", ImageUrl = "img/corona.png" },
            new() { ProductId = 3, Name = "Modelo", Category = "Cervezas", ImageUrl = "img/modelo.png" },
            new() { ProductId = 4, Name = "One", Category = "Cervezas", ImageUrl = "img/modelo.png" },
            new() { ProductId = 5, Name = "La tuya", Category = "Cervezas", ImageUrl = "img/modelo.png" },
            new() { ProductId = 6, Name = "Republica", Category = "Cervezas", ImageUrl = "img/modelo.png" }
        };

        private List<ProductResponseModel> rones = new()
        {
            new() { ProductId = 7, Name = "Bacardí", Category = "Rones", ImageUrl = "img/bacardi.png" },
            new() { ProductId = 8, Name = "Brugal", Category = "Rones", ImageUrl = "img/brugal.png" },
            new() { ProductId = 9, Name = "Barcelo", Category = "Rones", ImageUrl = "img/brugal.png" },
            new() { ProductId = 10, Name = "Don julio", Category = "Rones", ImageUrl = "img/brugal.png" },
            new() { ProductId = 11, Name = "new", Category = "Rones", ImageUrl = "img/brugal.png" }
        };

        void PedirDemo() { }
        void VerMasDemo() { }
    }
}