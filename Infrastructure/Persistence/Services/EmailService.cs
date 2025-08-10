using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Persistence.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Sending email to : {Email}, Subject: {Subject}", to, subject);

                var emailSettings = _configuration.GetSection("EmailSettings");
                var smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var fromEmail = emailSettings["FromEmail"] ?? "getinstantmart.contactus@gmail.com";
                var fromPassword = emailSettings["FromPassword"] ?? "pzev elzr lpsk fwuh";
                var fromName = emailSettings["FromName"] ?? "GetInstantMart";
                var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");
                var timeout = int.Parse(emailSettings["Timeout"] ?? "30000");

                //  BUSINESS SETTINGS for company info
                var businessSettings = _configuration.GetSection("BusinessSettings");
                var companyName = businessSettings["CompanyName"] ?? "GetInstantMart";
                var companyTagline = businessSettings["CompanyTagline"] ?? "Fast delivery, great service, every time.";
                var supportEmail = businessSettings["SupportEmail"] ?? "support@getinstantmart.com";
                var supportPhone = businessSettings["SupportPhone"] ?? "+977-XXX-XXXX";

                using var smtpClient = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = timeout,
                };

                var fromAddress = new MailAddress(fromEmail, fromName);
                var toAddress = new MailAddress(to);

                var mailMessage = new MailMessage()
                {
                    From = fromAddress,
                    Subject = subject,
                    Body = BuildGetInstantMartEmailBody(body, companyName, companyTagline, supportEmail, supportPhone),
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                mailMessage.To.Add(toAddress);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("GetInstantMart email sent successfully to: {Email}", to);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send GetInstantMart email to: {Email}, Subject: {Subject}", to, subject);
                throw; // Re-throw to let caller handle the error
            }
        }

        /* private static string BuildEmailBody(string bodyContent)
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
         }*/
        /// <summary>
        ///  BUILD GETINSTANTMART BRANDED EMAIL TEMPLATE
        /// </summary>
        private static string BuildGetInstantMartEmailBody(string bodyContent, string companyName, string tagline, string supportEmail, string supportPhone)
        {
            return $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>{companyName} Notification</title>
                    <style>
                        body {{ 
                            margin: 0; 
                            padding: 0; 
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            background-color: #f5f5f5;
                        }}
                        .email-container {{ 
                            max-width: 600px; 
                            margin: 0 auto; 
                            background-color: #ffffff;
                            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                        }}
                        .header {{ 
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                            color: white; 
                            padding: 20px; 
                            text-align: center;
                        }}
                        .logo {{ 
                            font-size: 32px; 
                            font-weight: bold; 
                            margin-bottom: 5px;
                        }}
                        .tagline {{ 
                            font-size: 14px; 
                            opacity: 0.9; 
                            margin: 0;
                        }}
                        .content {{ 
                            padding: 0; 
                        }}
                        .footer {{ 
                            background: #f8f9fa; 
                            padding: 30px 20px; 
                            text-align: center; 
                            border-top: 1px solid #e9ecef;
                        }}
                        .footer-logo {{ 
                            font-size: 24px; 
                            font-weight: bold; 
                            color: #667eea; 
                            margin-bottom: 10px;
                        }}
                        .footer-info {{ 
                            color: #6c757d; 
                            font-size: 14px; 
                            line-height: 1.6;
                        }}
                        .social-links {{ 
                            margin: 15px 0; 
                        }}
                        .social-links a {{ 
                            color: #667eea; 
                            text-decoration: none; 
                            margin: 0 10px; 
                            font-weight: bold;
                        }}
                        .divider {{ 
                            height: 1px; 
                            background: #e9ecef; 
                            margin: 20px 0;
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-container'>
                        <!-- HEADER -->
                        <div class='header'>
                            <div class='logo'>🛒 {companyName}</div>
                            <p class='tagline'>{tagline}</p>
                        </div>
                        
                        <!-- CONTENT -->
                        <div class='content'>
                            {bodyContent}
                        </div>
                        
                        <!-- FOOTER -->
                        <div class='footer'>
                            <div class='footer-logo'>🛒 {companyName}</div>
                            <div class='footer-info'>
                                <p><strong>Contact Us:</strong></p>
                                <p>📧 {supportEmail} | 📞 {supportPhone}</p>
                                <div class='divider'></div>
                                <div class='social-links'>
                                    <a href='#'>Facebook</a> |
                                    <a href='#'>Instagram</a> |
                                    <a href='#'>Twitter</a>
                                </div>
                                <div class='divider'></div>
                                <p style='font-size: 12px; color: #999;'>
                                    © 2024 {companyName}. All rights reserved.<br>
                                    You received this email because you have an account with us.<br>
                                    If you no longer wish to receive these emails, please contact us.
                                </p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}
