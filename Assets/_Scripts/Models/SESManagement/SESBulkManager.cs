using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.Runtime;
using UnityEngine;
using Newtonsoft.Json;
using _Scripts.Models.CognitoManagement;

namespace _Scripts.Models.SESManagement
{
    /// <summary>
    /// Maneja el envío masivo de emails de forma eficiente
    /// Permite enviar a múltiples destinatarios con límites y control de errores
    /// </summary>
    public class SESBulkManager
    {
        private readonly AmazonSimpleEmailServiceClient _sesClient;
        private readonly SESInfo.EmailConfiguration _config;
        private readonly bool _enableDebugLogs = true;

        // Límites de SES
        private const int MAX_RECIPIENTS_PER_CALL = 50; // Límite de SES para bulk emails
        private const int MAX_RETRIES = 3;
        private const int DELAY_BETWEEN_BATCHES_MS = 1000; // 1 segundo entre lotes

        // Tracking de envíos
        private readonly Dictionary<string, BulkEmailJob> _activeJobs = new();

        public SESBulkManager(SESInfo.EmailConfiguration config, AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sesClient = new AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
        }

        /// <summary>
        /// Envía email masivo simple a múltiples destinatarios
        /// </summary>
        public async Task<BulkEmailResult> SendBulkEmailAsync(
            List<string> recipients, 
            string subject, 
            string textBody, 
            string htmlBody = null,
            string jobId = null)
        {
            try
            {
                if (!CanUserSendBulkEmails())
                {
                    return new BulkEmailResult
                    {
                        Success = false,
                        Message = "User does not have permission to send bulk emails",
                        TotalRecipients = recipients.Count
                    };
                }

                jobId ??= Guid.NewGuid().ToString();
                LogDebug($"Starting bulk email job {jobId} for {recipients.Count} recipients");

                var job = new BulkEmailJob
                {
                    JobId = jobId,
                    TotalRecipients = recipients.Count,
                    StartTime = DateTime.UtcNow,
                    Status = BulkEmailStatus.InProgress
                };

                _activeJobs[jobId] = job;

                // Validar emails
                var validEmails = recipients.Where(IsValidEmail).ToList();
                var invalidEmails = recipients.Except(validEmails).ToList();

                if (invalidEmails.Any())
                {
                    LogDebug($"Found {invalidEmails.Count} invalid emails, skipping them");
                    job.InvalidEmails.AddRange(invalidEmails);
                }

                // Dividir en lotes
                var batches = CreateBatches(validEmails, MAX_RECIPIENTS_PER_CALL);
                job.TotalBatches = batches.Count;

                var result = new BulkEmailResult
                {
                    JobId = jobId,
                    TotalRecipients = recipients.Count,
                    ValidRecipients = validEmails.Count,
                    InvalidEmails = invalidEmails
                };

                // Procesar cada lote
                foreach (var batch in batches)
                {
                    try
                    {
                        var batchResult = await SendBatchEmailAsync(batch, subject, textBody, htmlBody);
                        
                        result.SuccessfulSends.AddRange(batchResult.SuccessfulSends);
                        result.FailedSends.AddRange(batchResult.FailedSends);
                        
                        job.ProcessedBatches++;
                        job.SuccessfulSends += batchResult.SuccessfulSends.Count;
                        job.FailedSends += batchResult.FailedSends.Count;

                        LogDebug($"Batch {job.ProcessedBatches}/{job.TotalBatches} completed. Success: {batchResult.SuccessfulSends.Count}, Failed: {batchResult.FailedSends.Count}");

                        // Delay entre lotes para respetar rate limits
                        if (job.ProcessedBatches < job.TotalBatches)
                        {
                            await Task.Delay(DELAY_BETWEEN_BATCHES_MS);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing batch {job.ProcessedBatches + 1}: {ex.Message}");
                        result.FailedSends.AddRange(batch.Select(email => new FailedEmail { Email = email, Reason = ex.Message }));
                        job.FailedSends += batch.Count;
                    }
                }

                // Finalizar job
                job.EndTime = DateTime.UtcNow;
                job.Status = BulkEmailStatus.Completed;
                
                result.Success = result.SuccessfulSends.Count > 0;
                result.Message = $"Bulk email completed. Success: {result.SuccessfulSends.Count}, Failed: {result.FailedSends.Count}";
                result.CompletedAt = job.EndTime;

                LogDebug($"Bulk email job {jobId} completed in {(job.EndTime.Value - job.StartTime).TotalSeconds:F2} seconds");

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error in bulk email job {jobId}: {ex.Message}");
                
                if (_activeJobs.ContainsKey(jobId))
                {
                    _activeJobs[jobId].Status = BulkEmailStatus.Failed;
                    _activeJobs[jobId].EndTime = DateTime.UtcNow;
                }

                return new BulkEmailResult
                {
                    JobId = jobId,
                    Success = false,
                    Message = ex.Message,
                    TotalRecipients = recipients.Count
                };
            }
        }

        /// <summary>
        /// Envía email masivo usando template
        /// </summary>
        public async Task<BulkEmailResult> SendBulkTemplatedEmailAsync(
            List<BulkRecipient> recipients, 
            string templateName,
            Dictionary<string, string> defaultTemplateData = null,
            string jobId = null)
        {
            try
            {
                if (!CanUserSendBulkEmails())
                {
                    return new BulkEmailResult
                    {
                        Success = false,
                        Message = "User does not have permission to send bulk emails",
                        TotalRecipients = recipients.Count
                    };
                }

                jobId ??= Guid.NewGuid().ToString();
                LogDebug($"Starting bulk templated email job {jobId} for {recipients.Count} recipients using template '{templateName}'");

                var job = new BulkEmailJob
                {
                    JobId = jobId,
                    TotalRecipients = recipients.Count,
                    StartTime = DateTime.UtcNow,
                    Status = BulkEmailStatus.InProgress
                };

                _activeJobs[jobId] = job;

                // Validar emails
                var validRecipients = recipients.Where(r => IsValidEmail(r.Email)).ToList();
                var invalidEmails = recipients.Where(r => !IsValidEmail(r.Email)).Select(r => r.Email).ToList();

                if (invalidEmails.Any())
                {
                    LogDebug($"Found {invalidEmails.Count} invalid emails, skipping them");
                    job.InvalidEmails.AddRange(invalidEmails);
                }

                // Dividir en lotes
                var batches = CreateBatches(validRecipients, MAX_RECIPIENTS_PER_CALL);
                job.TotalBatches = batches.Count;

                var result = new BulkEmailResult
                {
                    JobId = jobId,
                    TotalRecipients = recipients.Count,
                    ValidRecipients = validRecipients.Count,
                    InvalidEmails = invalidEmails
                };

                // Procesar cada lote
                foreach (var batch in batches)
                {
                    try
                    {
                        var batchResult = await SendBatchTemplatedEmailAsync(batch, templateName, defaultTemplateData ?? new Dictionary<string, string>());
                        
                        result.SuccessfulSends.AddRange(batchResult.SuccessfulSends);
                        result.FailedSends.AddRange(batchResult.FailedSends);
                        
                        job.ProcessedBatches++;
                        job.SuccessfulSends += batchResult.SuccessfulSends.Count;
                        job.FailedSends += batchResult.FailedSends.Count;

                        LogDebug($"Templated batch {job.ProcessedBatches}/{job.TotalBatches} completed. Success: {batchResult.SuccessfulSends.Count}, Failed: {batchResult.FailedSends.Count}");

                        // Delay entre lotes
                        if (job.ProcessedBatches < job.TotalBatches)
                        {
                            await Task.Delay(DELAY_BETWEEN_BATCHES_MS);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing templated batch {job.ProcessedBatches + 1}: {ex.Message}");
                        result.FailedSends.AddRange(batch.Select(r => new FailedEmail { Email = r.Email, Reason = ex.Message }));
                        job.FailedSends += batch.Count;
                    }
                }

                // Finalizar job
                job.EndTime = DateTime.UtcNow;
                job.Status = BulkEmailStatus.Completed;
                
                result.Success = result.SuccessfulSends.Count > 0;
                result.Message = $"Bulk templated email completed. Success: {result.SuccessfulSends.Count}, Failed: {result.FailedSends.Count}";
                result.CompletedAt = job.EndTime;

                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error in bulk templated email job {jobId}: {ex.Message}");
                
                if (_activeJobs.ContainsKey(jobId))
                {
                    _activeJobs[jobId].Status = BulkEmailStatus.Failed;
                    _activeJobs[jobId].EndTime = DateTime.UtcNow;
                }

                return new BulkEmailResult
                {
                    JobId = jobId,
                    Success = false,
                    Message = ex.Message,
                    TotalRecipients = recipients.Count
                };
            }
        }

        /// <summary>
        /// Obtiene el estado de un job de email masivo
        /// </summary>
        public BulkEmailJob GetJobStatus(string jobId)
        {
            return _activeJobs.ContainsKey(jobId) ? _activeJobs[jobId] : null;
        }

        /// <summary>
        /// Obtiene todos los jobs activos
        /// </summary>
        public List<BulkEmailJob> GetActiveJobs()
        {
            return _activeJobs.Values.ToList();
        }

        /// <summary>
        /// Cancela un job en progreso (si es posible)
        /// </summary>
        public bool CancelJob(string jobId)
        {
            if (_activeJobs.ContainsKey(jobId) && _activeJobs[jobId].Status == BulkEmailStatus.InProgress)
            {
                _activeJobs[jobId].Status = BulkEmailStatus.Cancelled;
                _activeJobs[jobId].EndTime = DateTime.UtcNow;
                LogDebug($"Job {jobId} cancelled");
                return true;
            }
            return false;
        }

        #region Private Methods

        private async Task<BatchEmailResult> SendBatchEmailAsync(List<string> emails, string subject, string textBody, string htmlBody)
        {
            var result = new BatchEmailResult();

            try
            {
                var body = new Body();
                if (!string.IsNullOrEmpty(textBody))
                {
                    body.Text = new Content { Charset = "UTF-8", Data = textBody };
                }
                if (!string.IsNullOrEmpty(htmlBody))
                {
                    body.Html = new Content { Charset = "UTF-8", Data = htmlBody };
                }

                var sendRequest = new SendEmailRequest
                {
                    Source = $"{_config.SenderName} <{_config.SenderEmail}>",
                    Destination = new Destination { ToAddresses = emails },
                    Message = new Message
                    {
                        Subject = new Content { Charset = "UTF-8", Data = subject },
                        Body = body
                    }
                };

                var response = await _sesClient.SendEmailAsync(sendRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    result.SuccessfulSends.AddRange(emails.Select(email => new SuccessfulEmail 
                    { 
                        Email = email, 
                        MessageId = response.MessageId,
                        SentAt = DateTime.UtcNow
                    }));
                }
                else
                {
                    result.FailedSends.AddRange(emails.Select(email => new FailedEmail 
                    { 
                        Email = email, 
                        Reason = $"HTTP Status: {response.HttpStatusCode}"
                    }));
                }
            }
            catch (Exception ex)
            {
                result.FailedSends.AddRange(emails.Select(email => new FailedEmail 
                { 
                    Email = email, 
                    Reason = ex.Message
                }));
            }

            return result;
        }

        private async Task<BatchEmailResult> SendBatchTemplatedEmailAsync(List<BulkRecipient> recipients, string templateName, Dictionary<string, string> defaultTemplateData)
        {
            var result = new BatchEmailResult();

            try
            {
                var destinations = recipients.Select(recipient => new BulkEmailDestination
                {
                    Destination = new Destination { ToAddresses = new List<string> { recipient.Email } },
                    ReplacementTemplateData = JsonConvert.SerializeObject(recipient.TemplateData ?? defaultTemplateData)
                }).ToList();

                var request = new SendBulkTemplatedEmailRequest
                {
                    Source = $"{_config.SenderName} <{_config.SenderEmail}>",
                    Template = templateName,
                    DefaultTemplateData = JsonConvert.SerializeObject(defaultTemplateData),
                    Destinations = destinations
                };

                var response = await _sesClient.SendBulkTemplatedEmailAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // SES bulk templated email no devuelve MessageIds individuales
                    result.SuccessfulSends.AddRange(recipients.Select(r => new SuccessfulEmail 
                    { 
                        Email = r.Email, 
                        MessageId = "bulk-" + Guid.NewGuid().ToString("N")[..8],
                        SentAt = DateTime.UtcNow
                    }));
                }
                else
                {
                    result.FailedSends.AddRange(recipients.Select(r => new FailedEmail 
                    { 
                        Email = r.Email, 
                        Reason = $"HTTP Status: {response.HttpStatusCode}"
                    }));
                }
            }
            catch (Exception ex)
            {
                result.FailedSends.AddRange(recipients.Select(r => new FailedEmail 
                { 
                    Email = r.Email, 
                    Reason = ex.Message
                }));
            }

            return result;
        }

        private List<List<T>> CreateBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                batches.Add(items.Skip(i).Take(batchSize).ToList());
            }
            return batches;
        }

        private bool CanUserSendBulkEmails()
        {
            var cognitoManager = CognitoManager.Instance;
            if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
            {
                return false;
            }

            var userRole = cognitoManager.GetUserRole();
            return userRole switch
            {
                "super-admin" => true,
                "operators" => true,
                "students" => false, // Los estudiantes no pueden enviar emails masivos
                "usuarios-basicos" => false,
                _ => false
            };
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[SESBulkManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[SESBulkManager] {message}");
        }

        #endregion

        #region Helper Classes

        public class BulkEmailResult
        {
            public string JobId { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
            public int TotalRecipients { get; set; }
            public int ValidRecipients { get; set; }
            public List<string> InvalidEmails { get; set; } = new();
            public List<SuccessfulEmail> SuccessfulSends { get; set; } = new();
            public List<FailedEmail> FailedSends { get; set; } = new();
            public DateTime? CompletedAt { get; set; }
        }

        public class BatchEmailResult
        {
            public List<SuccessfulEmail> SuccessfulSends { get; set; } = new();
            public List<FailedEmail> FailedSends { get; set; } = new();
        }

        public class BulkEmailJob
        {
            public string JobId { get; set; }
            public BulkEmailStatus Status { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public int TotalRecipients { get; set; }
            public int TotalBatches { get; set; }
            public int ProcessedBatches { get; set; }
            public int SuccessfulSends { get; set; }
            public int FailedSends { get; set; }
            public List<string> InvalidEmails { get; set; } = new();
            
            public double ProgressPercentage => TotalBatches > 0 ? (double)ProcessedBatches / TotalBatches * 100 : 0;
            public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        }

        public class BulkRecipient
        {
            public string Email { get; set; }
            public Dictionary<string, string> TemplateData { get; set; }
        }

        public class SuccessfulEmail
        {
            public string Email { get; set; }
            public string MessageId { get; set; }
            public DateTime SentAt { get; set; }
        }

        public class FailedEmail
        {
            public string Email { get; set; }
            public string Reason { get; set; }
        }

        public enum BulkEmailStatus
        {
            Pending,
            InProgress,
            Completed,
            Failed,
            Cancelled
        }

        #endregion
    }
}