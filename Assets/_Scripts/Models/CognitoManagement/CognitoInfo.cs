namespace _Scripts.Models.CognitoManagement
{
    using System;
    using System.Collections.Generic;

    [System.Serializable]
    public class CognitoInfo
    {
        public class TokenData
        {
            public string AccessToken { get; set; }
            public string IdToken { get; set; }
            public string RefreshToken { get; set; }
        }

        public class Credentials
        {
            public string AccessKeyId { get; set; }
            public string SecretKey { get; set; }
            public string SessionToken { get; set; }
        }

        public class UserInfo
        {
            public string Username { get; set; }
            public bool IsAuthenticated { get; set; }
            public string PendingUsername { get; set; }
            public string PendingEmail { get; set; }
            public List<string> UserGroups { get; set; } = new List<string>();
            public string CurrentUserGroup { get; set; }
        }

        public enum AuthStatus
        {
            Pending,
            Authenticated,
            Failed
        }
    }
}