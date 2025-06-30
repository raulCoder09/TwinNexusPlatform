using System.Collections.Generic;
using System.Threading.Tasks;

namespace _Scripts.Models.CognitoManagement
{
    public interface ICognitoOperations
    {
        Task<bool> SignInAsync(string username, string password);
        Task<bool> SignUpAsync(string username, string password, string email, string phoneNumber = null);
        Task<bool> ConfirmSignUpAsync(string username, string confirmationCode);
        Task<bool> ForgotPasswordAsync(string username);
        Task<bool> ConfirmForgotPasswordAsync(string username, string confirmationCode, string newPassword);
        void SignOut();
        Task<bool> RefreshTokenAsync();
        Task<bool> GetAWSCredentialsAsync();
        Task<List<string>> GetUserGroupsAsync();
        bool IsUserInGroup(string groupName);
        string GetUserRole();
    }
}