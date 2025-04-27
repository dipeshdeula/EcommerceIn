using Application.Common.Helper;
using System.Net.Mail;

namespace Infrastructure.Persistence.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
           

        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                //Credentials = new System.Net.NetworkCredential("deuladipesh94@gmail.com", "sdgw pgxy yzeg expx"),
                Credentials = new System.Net.NetworkCredential("getinstantmart.contactus@gmail.com", "pzev elzr lpsk fwuh"),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var fromAddress = new MailAddress("getinstantmart.contactus@gmail.com", "GetInstantMart");
            var toAddress = new MailAddress(to);

            var mailMessage = new MailMessage()
            {
                // From = new MailAddress("getinstantmart.contactus@gmail.com"),
                From = fromAddress,
                Subject = subject,
                Body = BuildEmailBody(body),
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);
        }

        private static string BuildEmailBody(string bodyContent)
        {
            return $@"
    <!DOCTYPE html>
    <html lang='en'>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Email Notification</title>
    </head>
    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9f9f9; padding: 20px;'>
        <div style='max-width: 600px; margin: auto; background: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>

            <h2 style='color: #007bff;'>GetInstantMart</h2>

            <p>Dear Customer,</p>

            <p>{bodyContent}</p>

            <p>Best regards,<br/>The GetInstantMart Team</p>

            <hr/>
            <small style='color: #999;'>If you have any questions, contact us at support@getinstantmart.com</small>
        </div>
    </body>
    </html>";
        }
    }
}
