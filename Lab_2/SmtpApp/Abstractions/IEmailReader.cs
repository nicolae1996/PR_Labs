using System.Collections.Generic;
using System.Threading.Tasks;
using MimeKit;

namespace SmtpApp.Abstractions
{
    public interface IEmailReader
    {
        /// <summary>
        /// Get all emails
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<MimeMessage>> GetAllMailsAsync();
    }
}
