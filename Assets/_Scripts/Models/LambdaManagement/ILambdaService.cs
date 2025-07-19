using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _Scripts.Models.LambdaManagement
{
    public interface ILambdaService
    {
        Task<bool> ExecuteFunctionAsync(string functionName, object payload = null);
        Task<bool> ExecuteFunctionWithContextAsync(string functionName, object additionalPayload = null);
        List<string> GetAvailableFunctions();
        Task<List<string>> GetAvailableFunctionsAsync();
        Task<bool> TestConnectivityAsync();
        event Action<bool, string, object> OnExecutionComplete;
    }
}