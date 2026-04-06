using System.Net;
using System.Net.Mail;

namespace HOA_Management_System.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpSettings = _config.GetSection("SmtpSettings");

            var message = new MailMessage();
            message.From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"]))
            {
                Credentials = new NetworkCredential(smtpSettings["SenderEmail"], smtpSettings["Password"]),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}