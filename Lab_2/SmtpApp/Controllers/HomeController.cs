using System.Diagnostics;
using System.Threading.Tasks;
using GR.Core.Helpers.Responses;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using SmtpApp.Abstractions;
using SmtpApp.Models;

namespace SmtpApp.Controllers
{
    public class HomeController : Controller
    {
        #region Injectable

        /// <summary>
        /// Inject email reader
        /// </summary>
        private readonly IEmailReader _emailReader;

        /// <summary>
        /// Inject email sender
        /// </summary>
        private readonly IEmailSender _emailSender;

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="emailReader"></param>
        /// <param name="emailSender"></param>
        public HomeController(IEmailReader emailReader, IEmailSender emailSender)
        {
            _emailReader = emailReader;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/[controller]/[action]")]
        public async Task<JsonResult> SendEmail(string to, string subject, string message)
        {
            await _emailSender.SendEmailAsync(to, subject, message);
            return Json(new SuccessResultModel<string>("Message sent"));
        }
    }
}
