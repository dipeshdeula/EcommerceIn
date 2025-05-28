namespace Application.Common.Helper
{
    public class EmailBodyHelper
    {
        public  static string BuildEmailBody(string bodyContent)
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
