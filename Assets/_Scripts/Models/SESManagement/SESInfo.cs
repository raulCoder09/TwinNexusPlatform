using Amazon;

namespace _Scripts.Models.SESManagement
{
    using System;
    using System.Collections.Generic;

    [System.Serializable]
    public class SESInfo
    {
        public class EmailConfiguration
        {
            public string SenderEmail { get; set; }
            public string SenderName { get; set; }
            public RegionEndpoint Region { get; set; }
        }

        public class EmailData
        {
            public string ToEmail { get; set; }
            public string Subject { get; set; }
            public string TextBody { get; set; }
            public string HtmlBody { get; set; }
            public string MessageId { get; set; }
        }

        public class VerificationStatus
        {
            public List<string> VerifiedEmails { get; set; } = new List<string>();
            public bool IsVerified { get; set; }
        }

        public class UserPermissions
        {
            public bool CanSendEmails { get; set; }
            public int DailyEmailLimit { get; set; }
        }

        public enum EmailStatus
        {
            Pending,
            Sent,
            Failed
        }
    }
}