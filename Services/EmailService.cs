//using System.Net;
//using System.Net.Mail;
//using Microsoft.Extensions.Configuration;
//using System.Threading.Tasks;

//namespace AutomationBackend.Services
//{
//    public class EmailService : IEmailService
//    {
//        private readonly IConfiguration _configuration;

//        public EmailService(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        public async Task SendEmailAsync(string toEmail, string subject, string body)
//        {
//            var emailSettings = _configuration.GetSection("EmailSettings");
//            var smtpServer = emailSettings["SmtpServer"];
//            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
//            var smtpUsername = emailSettings["SmtpUsername"];
//            var smtpPassword = emailSettings["SmtpPassword"];
//            var senderEmail = emailSettings["SenderEmail"];
//            var senderName = emailSettings["SenderName"];



//            using (var message = new MailMessage())
//            {
//                message.From = new MailAddress(senderEmail!, senderName);
//                message.To.Add(new MailAddress(toEmail));
//                message.Subject = subject;
//                message.Body = body;
//                message.IsBodyHtml = true;

//                using (var client = new SmtpClient(smtpServer, smtpPort))
//                {
//                    client.EnableSsl = true;
//                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
//                    await client.SendMailAsync(message);
//                }
//            }
//        }
//    }
//}
using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AutomationBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var smtpUsername = emailSettings["SmtpUsername"];
                var smtpPassword = emailSettings["SmtpPassword"];
                var senderEmail = emailSettings["SenderEmail"];
                var senderName = emailSettings["SenderName"];

                using var message = new MailMessage();
                message.From = new MailAddress(senderEmail!, senderName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpServer, smtpPort);

                client.EnableSsl = true;
                client.UseDefaultCredentials = false;

                client.Credentials = new NetworkCredential(
                    smtpUsername,
                    smtpPassword
                );

                await client.SendMailAsync(message);

                Console.WriteLine("EMAIL SENT SUCCESSFULLY");
            }
catch (Exception ex)
{
    Console.WriteLine("================================");
    Console.WriteLine("EMAIL ERROR");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("================================");

    throw;
}
        }
    }
}
