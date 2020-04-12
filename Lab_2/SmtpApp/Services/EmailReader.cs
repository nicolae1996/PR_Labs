using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GR.Core.Abstractions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using SmtpApp.Abstractions;
using SmtpApp.ViewModels;

namespace SmtpApp.Services
{
    public class EmailReader : IEmailReader
    {
        /// <summary>
        /// Imap settings
        /// </summary>
        private readonly ImapSettingsViewModel _options;

        public EmailReader(IWritableOptions<ImapSettingsViewModel> options)
        {
            _options = options.Value ?? throw new Exception("Email settings not register in appsettings file");
        }

        /// <summary>
        /// Get unread mails
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<MimeMessage>> GetUnreadMailsAsync()
        {
            var messages = new List<MimeMessage>();
            using var client = new ImapClient();
            await client.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl);

            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism.
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            await client.AuthenticateAsync(_options.NetworkCredential.Email, _options.NetworkCredential.Password);

            // The Inbox folder is always available on all IMAP servers...
            var inbox = client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            var results = inbox.Search(SearchOptions.All, SearchQuery.Not(SearchQuery.Seen));
            foreach (var uniqueId in results.UniqueIds)
            {
                //Mark message as read
                //inbox.AddFlags(uniqueId, MessageFlags.Seen, true);

                var message = inbox.GetMessage(uniqueId);
                messages.Add(message);
            }

            await client.DisconnectAsync(true);
            return messages;
        }

        /// <summary>
        /// Get all emails
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<MimeMessage>> GetAllMailsAsync()
        {
            var messages = new List<MimeMessage>();
            using var client = new ImapClient();
            await client.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl);

            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism.
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            await client.AuthenticateAsync(_options.NetworkCredential.Email, _options.NetworkCredential.Password);

            // The Inbox folder is always available on all IMAP servers...
            var inbox = client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            var results = await inbox.SearchAsync(SearchOptions.All, SearchQuery.All);
            foreach (var uniqueId in results.UniqueIds)
            {
                var message = inbox.GetMessage(uniqueId);

                //Mark message as read
                //inbox.AddFlags(uniqueId, MessageFlags.Seen, true);
                messages.Add(message);
            }

            await client.DisconnectAsync(true);
            return messages;
        }
    }
}
