using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using GR.Core.Abstractions;
using GR.Core.Extensions;
using Microsoft.AspNetCore.Identity.UI.Services;
using SmtpApp.ViewModels;

namespace SmtpApp.Services
{
    public class EmailSender : IEmailSender
    {
        /// <summary>
        /// Email settings
        /// </summary>
        private readonly IWritableOptions<EmailSettingsViewModel> _options;

        public EmailSender(IWritableOptions<EmailSettingsViewModel> options)
        {
            if (options.Value == null) throw new Exception("Email settings not register in appsettings file");
            _options = options;
        }

        /// <summary>
        /// Send email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (!_options.Value.Enabled || !email.IsValidEmail()) return;
            var settings = _options.Value;
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Port = settings.Port;
                    client.Host = settings.Host;
                    client.EnableSsl = settings.EnableSsl;
                    client.Timeout = settings.Timeout;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(settings.NetworkCredential.Email, settings.NetworkCredential.Password);

                    var mailMessage = new MailMessage
                    {
                        BodyEncoding = Encoding.UTF8,
                        DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure,
                        From = new MailAddress(settings.NetworkCredential.Email),
                        Subject = subject,
                        Body = htmlMessage,
                        Priority = MailPriority.High,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
