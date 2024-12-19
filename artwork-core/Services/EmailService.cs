using MailKit.Net.Smtp;
using MimeKit;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ArtworkCore.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task SendAsync(string toEmail, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_configuration["SMPT:SenderName"], _configuration["SMPT:SenderEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = message
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["SMPT:Host"], int.Parse(_configuration["SMPT:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["SMPT:User"], _configuration["SMPT:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
