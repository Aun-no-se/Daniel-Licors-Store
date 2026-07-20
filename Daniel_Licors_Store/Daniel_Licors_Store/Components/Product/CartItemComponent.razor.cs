using Microsoft.AspNetCore.Components;
using Daniel_Licors_Store.Models.Cart;
using System;
using System.Threading.Tasks;

namespace Daniel_Licors_Store.Components.Product;

public partial class CartItemComponent
{
    [Parameter] public object? Item { get; set; }
    [Parameter] public string ItemType { get; set; } = "product";
    [Parameter] public EventCallback<(int, string)> OnRemove { get; set; }
    [Parameter] public EventCallback<(int, int, string)> OnQuantityChange { get; set; }
    [Parameter] public EventCallback<(int, bool, string)> OnSelectionChange { get; set; }

    private bool _isSelected = false;
    private int _quantity = 1;

    private string CardClass => _isSelected
        ? "cart-item-card-modern mb-3 selected"
        : "cart-item-card-modern mb-3";

    protected override void OnInitialized()
    {
        if (Item == null) return;

        if (Item is CartItemDisplayDto product)
        {
            _quantity = product.Quantity > 0 ? product.Quantity : 1;
            _isSelected = product.IsSelected;
        }
        else if (Item is CartComboDisplayDto combo)
        {
            _quantity = combo.Quantity > 0 ? combo.Quantity : 1;
            _isSelected = combo.IsSelected;
        }
    }

    private async Task ToggleSelection()
    {
        _isSelected = !_isSelected;
        await OnSelectionChange.InvokeAsync((GetItemId(), _isSelected, ItemType));
    }

    private string GetItemName() => Item switch
    {
        CartItemDisplayDto product => product.ProductName,
        CartComboDisplayDto combo => combo.ComboName,
        _ => "Artículo"
    };

    private string GetCategory() => Item switch
    {
        CartItemDisplayDto product => product.Category,
        CartComboDisplayDto combo => combo.Category,
        _ => "General"
    };

    private string? GetImage() => Item switch
    {
        CartItemDisplayDto product => product.ProductImage,
        CartComboDisplayDto combo => combo.ComboImage,
        _ => null
    };

    private decimal GetUnitPrice() => Item switch
    {
        CartItemDisplayDto product => product.UnitPrice,
        CartComboDisplayDto combo => combo.UnitPrice,
        _ => 0
    };

    private decimal GetSubTotal() => Item switch
    {
        CartItemDisplayDto product => product.SubTotal,
        CartComboDisplayDto combo => combo.SubTotal,
        _ => 0
    };

    private int GetItemId() => Item switch
    {
        CartItemDisplayDto product => product.CartItemId,
        CartComboDisplayDto combo => combo.CartComboId,
        _ => 0
    };

    private string GetItemTypeLabel() => Item switch
    {
        CartItemDisplayDto => "Producto",
        CartComboDisplayDto => "Combo",
        _ => "Promoción"
    };

    private string GetBadgeClass() => Item switch
    {
        CartItemDisplayDto => "badge-product",
        CartComboDisplayDto combo when combo.ComboName.Contains("Promo", StringComparison.OrdinalIgnoreCase) => "badge-promo",
        CartComboDisplayDto => "badge-combo",
        _ => "badge-product"
    };

    private async Task IncreaseQuantity()
    {
        _quantity++;
        await OnQuantityChange.InvokeAsync((GetItemId(), _quantity, ItemType));
    }

    private async Task DecreaseQuantity()
    {
        if (_quantity > 1)
        {
            _quantity--;
            await OnQuantityChange.InvokeAsync((GetItemId(), _quantity, ItemType));
        }
    }

    private async Task QuantityChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int newQuantity) && newQuantity > 0)
        {
            _quantity = newQuantity;
            await OnQuantityChange.InvokeAsync((GetItemId(), _quantity, ItemType));
        }
    }

    private async Task Remove() => await OnRemove.InvokeAsync((GetItemId(), ItemType));

    private async Task OnSelectionChanged(bool value)
    {
        _isSelected = value;
        await OnSelectionChange.InvokeAsync((GetItemId(), value, ItemType));
    }
}
