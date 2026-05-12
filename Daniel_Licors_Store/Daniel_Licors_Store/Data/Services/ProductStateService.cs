using System;

namespace Daniel_Licors_Store.Services
{
    public class ProductStateService
    {
        public event Action? OnChange;

        public void NotifyChange()
        {
            OnChange?.Invoke();
        }
    }
}