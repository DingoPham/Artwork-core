using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace ArtworkCore.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;

        public EmailService(IConfiguration configuration)
        {
            // Cấu hình từ biến môi trường
            _smtpHost = configuration["SMTP:Host"];
            _smtpPort = int.Parse(configuration["SMTP:Port"]);
            _smtpUser = configuration["SMTP:User"];
            _smtpPassword = configuration["SMTP:Password"];
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_smtpHost, _smtpPort))
                {
                    client.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                    client.EnableSsl = true;

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_smtpUser, "Artwork Site");
                        message.To.Add(new MailAddress(toEmail));
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        await client.SendMailAsync(message);
                    }
                }

                Console.WriteLine("Email sent successfully to: " + toEmail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                throw;
            }
        }
    }
}
