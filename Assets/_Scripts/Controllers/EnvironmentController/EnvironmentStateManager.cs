using System;

namespace _Scripts.Controllers.EnvironmentController
{
    public static class EnvironmentStateManager
    {
        private static string _selectedEnvironment = "Menu environment";
        public static event Action<string> OnEnvironmentChanged;

        public static string SelectedEnvironment
        {
            get => _selectedEnvironment;
            set
            {
                if (_selectedEnvironment != value)
                {
                    _selectedEnvironment = value;
                    OnEnvironmentChanged?.Invoke(_selectedEnvironment);
                    UnityEngine.Debug.Log($"[EnvironmentStateManager] Environment changed to: {_selectedEnvironment}");
                }
            }
        }

        public static bool IsValidEnvironment => _selectedEnvironment != "Menu environment";
    }
}