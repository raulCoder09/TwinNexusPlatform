using _Scripts.Controllers.WelcomeController;

namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    [System.Serializable]
    public class WelcomeInfo
    {
        public class UIConfiguration
        {
            public Dictionary<IWelcomeOps.PanelType, PanelData> Panels { get; set; } = new Dictionary<IWelcomeOps.PanelType, PanelData>();
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }

            [System.Serializable]
            public class PanelData
            {
                public VisualElement Panel { get; set; }
                public string ShowClass { get; set; }
                public string HideClass { get; set; }
            }
        }

        public class UserData
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public bool IsAuthenticated { get; set; }
        }

        public class AuthState
        {
            public bool IsInitialized { get; set; }
            public event Action OnAuthenticationSuccess;
            public event Action<string> OnAuthenticationFailure;
        }
    }
}