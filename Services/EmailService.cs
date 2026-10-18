using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace InventarioSaaS.Api.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            
            // El operador "!" le dice a C# que estamos seguros de que estos valores no son nulos
            email.From.Add(new MailboxAddress(_config["EmailSettings:SenderName"]!, _config["EmailSettings:SenderEmail"]!));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            // CORRECCIÓN DEL ERROR ROJO: Usar ToMessageBody()
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            
            // Silenciamos las advertencias amarillas con "!"
            await smtp.ConnectAsync(_config["EmailSettings:SmtpServer"]!, int.Parse(_config["EmailSettings:SmtpPort"]!), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:SenderEmail"]!, _config["EmailSettings:SenderPassword"]!);
            
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}