namespace Daniel_Licors_Store.Components.Shared;

/// <summary>
/// Lo único que comparten el botón de la barra superior y el panel del buscador:
/// si el panel está abierto o cerrado. El panel vive en MainLayout (fuera de la barra,
/// que tiene backdrop-filter y rompería el position:fixed).
/// </summary>
public class QuickSearchState
{
    public bool IsOpen { get; private set; }
    public event Action? OnChange;

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        OnChange?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        OnChange?.Invoke();
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }
}
