using System.Net;
using System.Net.Mail;

namespace NAWatchMVC.Helpers // Đã đổi sang namespace mới cho khớp project
{
    public class MyEmailSender
    {
        private readonly IConfiguration _configuration;

        public MyEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Lấy cấu hình từ section "EmailSettings" trong appsettings.json
            var mailSettings = _configuration.GetSection("EmailSettings");

            string host = mailSettings["MailServer"];
            int port = int.Parse(mailSettings["MailPort"] ?? "587");
            string email = mailSettings["SenderEmail"];
            string password = mailSettings["Password"];
            string displayName = mailSettings["SenderName"];

            var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email, displayName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}