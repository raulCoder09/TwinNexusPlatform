using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _scripts.models.awsServices;
using Amazon;
using Amazon.CognitoIdentityProvider;
using AmazonWebServices;
using UnityEngine;

namespace _scripts.controllers
{
    public class AWS : MonoBehaviour
    {
        #region AWS parámetros fundamentales
            [Header("AWS")]
            [SerializeField] private RegionEndpoint _region = RegionEndpoint.USEast1;
            
            [SerializeField] private string _identityPoolID = "us-east-1:e962d906-6f36-4e52-8771-a2de6a11b19a";
            [SerializeField] private string _userPoolID     = "us-east-1_eyRKiPuWJ";
            [SerializeField] private string _clientID       = "62ham6j5iav40urvcooai5vka4";

            private string _idToken, _accessToken, _refreshToken;
            private IdentityContext _ctx;
        #endregion

        #region Servicios
            private Cognito _cognito;
            private SimpleEmailService _ses;
            private CloudWatch _cloudwatch;
            #endregion

            #region Parámetros de SES
            [Header("SES")]
            [SerializeField] private string _sourceEmail     = "mechar09@outlook.com";
            [SerializeField] private string _destinationMail = "mechar09@yahoo.com";
            private string _subjectMail = null;
        #endregion

        #region Logging (CloudWatch)
            private const string LOG_GROUP         = "TwinNexusPlatform";
            private const string LOG_STREAM        = "appMonitoring";
            private const int    LOG_RETENTION_DAYS = 1;
        #endregion

        private string _statusCognitoMessage;
        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();

            _cognito = Cognito.GetInstance(
                _clientID,
                new AmazonCognitoIdentityProviderClient(_region)
            );
        }

        private void OnDisable()
        {
            try { _cts?.Cancel(); } catch { /* ignore */ }
            _cts?.Dispose();
            _cts = null;
        }
        
        private async Task ActivateAwsServicesAsync()
        {
            _ctx = new IdentityContext(_region, _identityPoolID, _userPoolID).AsUser(_idToken);

            _ses        = SimpleEmailService.GetInstance(_ctx);
            _cloudwatch = CloudWatch.GetInstance(_ctx);

            await _cloudwatch.InitAsync(LOG_GROUP, LOG_STREAM, LOG_RETENTION_DAYS);
        }

        #region Métodos de Cognito
        
        internal async Task<(bool ok, string msg)> Login(string username, string password)
        {
            (_idToken, _accessToken, _refreshToken) = _cognito.Login(username, password);

            if (_idToken?.StartsWith("error") == true)
            {
                _statusCognitoMessage = $"login failed: {_idToken}";
                return (false, _statusCognitoMessage);
            }

            _subjectMail          = "login";
            _statusCognitoMessage = $"User {username} logged at {DateTime.Now}";

            await ActivateAwsServicesAsync();
            
            _ = SendEmailAsync(_sourceEmail, _destinationMail, _subjectMail, _statusCognitoMessage);
            _ = SendLogAsync($"auth | {username} | {_statusCognitoMessage}");
            return (true, _statusCognitoMessage);
        }

        internal (bool ok, string msg) Register(string username, string password, string email)
        {
            var (ok, error, medium, dest) = _cognito.SignUp(
                username,
                password,
                new Dictionary<string, string>
                {
                    ["email"] = email,
                    ["preferred_username"] = username
                }
            );

            if (string.IsNullOrEmpty(error))
            {
                _statusCognitoMessage = "all ok";
                return (true, _statusCognitoMessage);
            }

            _statusCognitoMessage = $"Error: {error}";
            return (false, _statusCognitoMessage);
        }

        internal (bool ok, string msg) ConfirmSignUp(string username, string code)
        {
            var result = _cognito.ConfirmSignUp(username, code);

            if (result.ok)
            {
                _statusCognitoMessage = "Usuario confirmado";
                return (true, _statusCognitoMessage);
            }

            _statusCognitoMessage = $"Error: {result.error}";
            return (false, _statusCognitoMessage);
        }

        #endregion

        #region Métodos SES
        
        internal Task<(bool ok, string error, string messageId)> SendEmailAsync(
            string source,
            string destination,
            string subject,
            string htmlBody)
        {
            return _ses.SendEmailAsync(
                from: source,
                to: destination,
                subject: subject,
                htmlBody: htmlBody,
                textBody: null,
                replyTo: null,
                configurationSet: null,
                tags: null,
                ct: _cts?.Token ?? default
            );
        }

        #endregion

        #region Métodos CloudWatch
        
        internal async Task SendLogAsync(string message)
        {
            if (_cloudwatch == null) return;

            var (ok, error) = await _cloudwatch.LogAsync(message);
            // Debug.Log(ok ? "Log enviado correctamente a CloudWatch"
            //              : $"Error al enviar log: {error}");
        }
        
        internal Task SendLogAsync(string topic, string itemName, string value)
            => SendLogAsync($"{topic} | {itemName}: {value}");
        internal Task SendLogAsync(string topic, string itemName, bool value)
            => SendLogAsync($"{topic} | {itemName}: {value}");
        #endregion
    }
}
