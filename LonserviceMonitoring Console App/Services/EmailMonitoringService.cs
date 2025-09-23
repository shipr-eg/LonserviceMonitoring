using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using LonserviceMonitoring.Models;

namespace LonserviceMonitoring.Services
{
    public interface IEmailMonitoringService
    {
        Task StartMonitoringAsync(CancellationToken cancellationToken);
        event EventHandler<string> CsvFileDetected;
    }

    public class EmailMonitoringService : IEmailMonitoringService
    {
        private readonly EmailSettings _emailSettings;
        private readonly MonitoringSettings _monitoringSettings;
        private readonly ILogger<EmailMonitoringService> _logger;
        private GraphServiceClient? _graphServiceClient;
        private readonly HashSet<string> _processedMessageIds = new HashSet<string>();

        public event EventHandler<string>? CsvFileDetected;

        public EmailMonitoringService(
            IOptions<EmailSettings> emailSettings,
            IOptions<MonitoringSettings> monitoringSettings,
            ILogger<EmailMonitoringService> logger)
        {
            _emailSettings = emailSettings.Value;
            _monitoringSettings = monitoringSettings.Value;
            _logger = logger;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Office 365 email monitoring...");

            if (string.IsNullOrEmpty(_emailSettings.TenantId) || 
                string.IsNullOrEmpty(_emailSettings.ClientId) || 
                string.IsNullOrEmpty(_emailSettings.ClientSecret))
            {
                _logger.LogError("Office 365 configuration is incomplete. Please check TenantId, ClientId, and ClientSecret.");
                throw new InvalidOperationException("Office 365 configuration is incomplete");
            }

            await InitializeGraphClientAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForNewEmailsAsync();
                    await Task.Delay(TimeSpan.FromMinutes(_monitoringSettings.CheckIntervalMinutes), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during email monitoring cycle");
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken); // Wait before retrying
                }
            }
        }

        private Task InitializeGraphClientAsync()
        {
            try
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(_emailSettings.ClientId)
                    .WithClientSecret(_emailSettings.ClientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{_emailSettings.TenantId}")
                    .Build();

                var authProvider = new ClientCredentialProvider(app);
                _graphServiceClient = new GraphServiceClient(authProvider);

                _logger.LogInformation("Microsoft Graph client initialized successfully");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Microsoft Graph client");
                throw;
            }
        }

        private async Task CheckForNewEmailsAsync()
        {
            if (_graphServiceClient == null)
            {
                _logger.LogError("Graph service client is not initialized");
                return;
            }

            try
            {
                _logger.LogDebug("Checking for new emails with CSV attachments...");

                var mailboxEmail = string.IsNullOrEmpty(_emailSettings.MailboxEmail) 
                    ? "me" 
                    : _emailSettings.MailboxEmail;

                // Get messages from the last 24 hours to avoid missing any
                var dateFilter = DateTime.UtcNow.AddHours(-24).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var filter = $"receivedDateTime ge {dateFilter} and hasAttachments eq true";

                var messages = await _graphServiceClient
                    .Users[mailboxEmail]
                    .MailFolders[_emailSettings.FolderName]
                    .Messages
                    .Request()
                    .Filter(filter)
                    .OrderBy("receivedDateTime desc")
                    .GetAsync();

                foreach (var message in messages)
                {
                    if (_processedMessageIds.Contains(message.Id))
                        continue;

                    _processedMessageIds.Add(message.Id);
                    await ProcessEmailWithAttachmentsAsync(mailboxEmail, message);
                }

                _logger.LogDebug("Completed email check cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for new emails");
            }
        }

        private async Task ProcessEmailWithAttachmentsAsync(string mailboxEmail, Microsoft.Graph.Message message)
        {
            try
            {
                var attachments = await _graphServiceClient!
                    .Users[mailboxEmail]
                    .Messages[message.Id]
                    .Attachments
                    .Request()
                    .GetAsync();

                foreach (var attachment in attachments)
                {
                    if (attachment is FileAttachment fileAttachment &&
                        fileAttachment.Name?.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await DownloadCsvAttachmentAsync(fileAttachment, message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email attachments for message: {MessageId}", message.Id);
            }
        }

        private async Task DownloadCsvAttachmentAsync(FileAttachment attachment, Microsoft.Graph.Message message)
        {
            try
            {
                var timeBlock = DateTime.Now.ToString("ddMMyyyy_HHmm");
                var fileName = Path.GetFileNameWithoutExtension(attachment.Name);
                var extension = Path.GetExtension(attachment.Name);
                var newFileName = $"{fileName}_{timeBlock}{extension}";
                
                var workFolderPath = Path.Combine(_monitoringSettings.WorkFolder, timeBlock);
                System.IO.Directory.CreateDirectory(workFolderPath);
                
                var filePath = Path.Combine(workFolderPath, newFileName);

                if (attachment.ContentBytes != null)
                {
                    await System.IO.File.WriteAllBytesAsync(filePath, attachment.ContentBytes);
                    
                    _logger.LogInformation("Downloaded CSV attachment: {FileName} from email: {Subject}", 
                        newFileName, message.Subject);
                    
                    CsvFileDetected?.Invoke(this, filePath);
                }
                else
                {
                    _logger.LogWarning("Attachment {AttachmentName} has no content bytes", attachment.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading CSV attachment: {AttachmentName}", attachment.Name);
            }
        }
    }
}
