using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BOnlineHomeAssignement.Server.Domain.Entities;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;

namespace BOnlineHomeAssignement.Server.Application.Services
{
    /// <summary>
    /// Provider interface for sending messages through different channels
    /// Allows pluggable implementations (Email, SMS, WhatsApp, Voice, etc.)
    /// </summary>
    public interface ICommunicationProvider
    {
        string ChannelName { get; }
        Task<bool> SendAsync(CommunicationMessage message);
        Task<string> GetStatusAsync(string providerMessageId);
    }

    /// <summary>
    /// Default email provider (placeholder - real implementation would use SendGrid, SMTP, etc.)
    /// </summary>
    public class EmailCommunicationProvider : ICommunicationProvider
    {
        public string ChannelName => "Email";

        public async Task<bool> SendAsync(CommunicationMessage message)
        {
            // Placeholder: In production, integrate with SendGrid, AWS SES, SMTP, etc.
            // For demo: log and mark as sent
            Console.WriteLine($"[EMAIL] To: {message.Recipient}, Subject: {message.Subject}");
            await Task.Delay(100); // Simulate I/O
            return true;
        }

        public async Task<string> GetStatusAsync(string providerMessageId)
        {
            // Placeholder: Query provider for delivery status
            await Task.Delay(50);
            return "Delivered";
        }
    }

    /// <summary>
    /// Default SMS provider (placeholder - real implementation would use Twilio, Vonage, etc.)
    /// </summary>
    public class SmsCommunicationProvider : ICommunicationProvider
    {
        public string ChannelName => "SMS";

        public async Task<bool> SendAsync(CommunicationMessage message)
        {
            // Placeholder: In production, integrate with Twilio, AWS SNS, Vonage, etc.
            Console.WriteLine($"[SMS] To: {message.Recipient}, Body: {message.MessageBody.Substring(0, Math.Min(50, message.MessageBody.Length))}...");
            await Task.Delay(100);
            return true;
        }

        public async Task<string> GetStatusAsync(string providerMessageId)
        {
            // Placeholder: Query provider for delivery status
            await Task.Delay(50);
            return "Delivered";
        }
    }

    /// <summary>
    /// WhatsApp provider (placeholder)
    /// </summary>
    public class WhatsAppCommunicationProvider : ICommunicationProvider
    {
        public string ChannelName => "WhatsApp";

        public async Task<bool> SendAsync(CommunicationMessage message)
        {
            // Placeholder: Would integrate with WhatsApp Business API
            Console.WriteLine($"[WHATSAPP] To: {message.Recipient}, Body: {message.MessageBody.Substring(0, Math.Min(50, message.MessageBody.Length))}...");
            await Task.Delay(100);
            return true;
        }

        public async Task<string> GetStatusAsync(string providerMessageId)
        {
            await Task.Delay(50);
            return "Delivered";
        }
    }

    /// <summary>
    /// Voice/Call provider (placeholder)
    /// </summary>
    public class VoiceCommunicationProvider : ICommunicationProvider
    {
        public string ChannelName => "Voice";

        public async Task<bool> SendAsync(CommunicationMessage message)
        {
            // Placeholder: Would integrate with Twilio Voice, AWS Connect, etc.
            Console.WriteLine($"[VOICE] Call: {message.Recipient}");
            await Task.Delay(200);
            return true;
        }

        public async Task<string> GetStatusAsync(string providerMessageId)
        {
            await Task.Delay(50);
            return "Completed";
        }
    }

    /// <summary>
    /// Service for queuing and sending communications through multiple channels
    /// Abstracts away provider details; supports Email, SMS, WhatsApp, Voice, InApp
    /// </summary>
    public class CommunicationService
    {
        private readonly AppDbContext _dbContext;
        private readonly Dictionary<string, ICommunicationProvider> _providers;

        public CommunicationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _providers = InitializeProviders();
        }

        /// <summary>
        /// Initializes available communication providers
        /// </summary>
        private Dictionary<string, ICommunicationProvider> InitializeProviders()
        {
            return new Dictionary<string, ICommunicationProvider>
            {
                { "Email", new EmailCommunicationProvider() },
                { "SMS", new SmsCommunicationProvider() },
                { "WhatsApp", new WhatsAppCommunicationProvider() },
                { "Voice", new VoiceCommunicationProvider() }
            };
        }

        /// <summary>
        /// Queues a message for sending through specified channel
        /// </summary>
        public async Task<CommunicationMessage> QueueMessageAsync(
            Guid tenantId,
            Guid patientPathwayId,
            string channel,
            string recipient,
            string messageBody,
            string? subject = null,
            string? messageType = null,
            DateTime? scheduledFor = null,
            string? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            var message = new CommunicationMessage
            {
                TenantId = tenantId,
                PatientPathwayId = patientPathwayId,
                Channel = channel,
                Recipient = recipient,
                Subject = subject,
                MessageBody = messageBody,
                MessageType = messageType,
                Status = "Queued",
                ScheduledFor = scheduledFor ?? DateTime.UtcNow,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                AttemptCount = 0,
                MaxRetries = 3
            };

            _dbContext.CommunicationMessages.Add(message);
            return message;
        }

        /// <summary>
        /// Sends all queued messages that are ready (for background job)
        /// </summary>
        public async Task ProcessQueuedMessagesAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;

            var pendingMessages = await _dbContext.CommunicationMessages
                .Where(cm => cm.TenantId == tenantId
                    && cm.Status == "Queued"
                    && cm.ScheduledFor <= now)
                .ToListAsync();

            foreach (var message in pendingMessages)
            {
                await SendMessageAsync(message);
            }
        }

        /// <summary>
        /// Sends a single message through the appropriate provider
        /// </summary>
        public async Task SendMessageAsync(CommunicationMessage message)
        {
            try
            {
                message.Status = "Sent";
                message.SentAt = DateTime.UtcNow;
                message.AttemptCount++;

                // Get provider for channel
                if (!_providers.TryGetValue(message.Channel, out var provider))
                {
                    message.Status = "Failed";
                    message.ErrorMessage = $"Unknown channel: {message.Channel}";
                    _dbContext.CommunicationMessages.Update(message);
                    await _dbContext.SaveChangesAsync();
                    return;
                }

                // Attempt to send
                bool success = await provider.SendAsync(message);

                if (success)
                {
                    message.Status = "Delivered";
                    message.DeliveredAt = DateTime.UtcNow;
                }
                else
                {
                    if (message.AttemptCount >= message.MaxRetries)
                    {
                        message.Status = "Failed";
                        message.ErrorMessage = "Max retries exceeded";
                    }
                    else
                    {
                        message.Status = "Queued";
                        message.ScheduledFor = DateTime.UtcNow.AddMinutes(Math.Pow(2, message.AttemptCount)); // Exponential backoff
                    }
                }

                _dbContext.CommunicationMessages.Update(message);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                message.ErrorMessage = ex.Message;
                message.AttemptCount++;

                if (message.AttemptCount >= message.MaxRetries)
                {
                    message.Status = "Failed";
                }
                else
                {
                    message.Status = "Queued";
                    message.ScheduledFor = DateTime.UtcNow.AddMinutes(Math.Pow(2, message.AttemptCount));
                }

                _dbContext.CommunicationMessages.Update(message);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Processes failed messages that can be retried
        /// </summary>
        public async Task ProcessFailedMessagesForRetryAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;

            var failedMessages = await _dbContext.CommunicationMessages
                .Where(cm => cm.TenantId == tenantId
                    && cm.Status == "Queued"
                    && cm.ScheduledFor <= now
                    && cm.AttemptCount < cm.MaxRetries)
                .ToListAsync();

            foreach (var message in failedMessages)
            {
                await SendMessageAsync(message);
            }
        }

        /// <summary>
        /// Gets all communication messages for a patient pathway (timeline/audit)
        /// </summary>
        public async Task<List<CommunicationMessage>> GetPathwayMessagesAsync(
            Guid tenantId,
            Guid patientPathwayId)
        {
            return await _dbContext.CommunicationMessages
                .Where(cm => cm.TenantId == tenantId && cm.PatientPathwayId == patientPathwayId)
                .OrderByDescending(cm => cm.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all communication messages by status (for monitoring)
        /// </summary>
        public async Task<List<CommunicationMessage>> GetMessagesByStatusAsync(
            Guid tenantId,
            string status)
        {
            return await _dbContext.CommunicationMessages
                .Where(cm => cm.TenantId == tenantId && cm.Status == status)
                .OrderByDescending(cm => cm.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets communication summary for a tenant (dashboard metrics)
        /// </summary>
        public async Task<Dictionary<string, int>> GetCommunicationSummaryAsync(Guid tenantId)
        {
            var messages = await _dbContext.CommunicationMessages
                .Where(cm => cm.TenantId == tenantId)
                .GroupBy(cm => cm.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return messages.ToDictionary(m => m.Status, m => m.Count);
        }

        /// <summary>
        /// Registers a custom communication provider
        /// </summary>
        public void RegisterProvider(ICommunicationProvider provider)
        {
            _providers[provider.ChannelName] = provider;
        }

        /// <summary>
        /// Updates message status (e.g., when webhook callback received from provider)
        /// </summary>
        public async Task UpdateMessageStatusAsync(string providerMessageId, string status)
        {
            var message = await _dbContext.CommunicationMessages
                .FirstOrDefaultAsync(cm => cm.ProviderMessageId == providerMessageId);

            if (message != null)
            {
                message.Status = status;
                if (status == "Delivered")
                {
                    message.DeliveredAt = DateTime.UtcNow;
                }

                _dbContext.CommunicationMessages.Update(message);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}