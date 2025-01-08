namespace BookProject.Models
{
    using System.Net;
    using System.Net.Mail;
    using System.Threading.Tasks;

    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;

        public EmailService(string smtpServer, int smtpPort, string fromEmail, string fromPassword)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _fromEmail = fromEmail;
            _fromPassword = fromPassword;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var client = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Password Reset Request",
                Body = $@"
                <h2>Password Reset Request</h2>
                <p>We received a request to reset your password.</p>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you didn't request this, please ignore this email.</p>
                <p>The link will expire in 1 hour.</p>",
                IsBodyHtml = true
            };
        
            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        
        public async Task SendWelcomeEmailAsync(string toEmail, string username)
        {
            var client = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Welcome to Our Book Library!",
                Body = $@"
            <h2>Welcome {username}!</h2>
            <p>Thank you for registering to our book library system.</p>
            <p>You can now start browsing and borrowing books from our collection.</p>
            <p>Enjoy reading!</p>",
                IsBodyHtml = true
            };
    
            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        
        public async Task SendBookAvailableEmailAsync(string toEmail, string bookTitle)
        {
            var client = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Book Available for Borrowing!",
                Body = $@"
                <h2>Book Available for Borrowing!</h2>
                <p>Hello,</p>
                <p>The book {bookTitle} that you requested is now available!</p>
                <p>You have 24 hours to log into the website and borrow the book.</p>
                <p>After 24 hours, the option will pass to the next user on the waiting list.</p>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        
        
        public async Task SendPurchaseConfirmationEmailAsync(string toEmail, string orderDetails)
        {
            var client = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true,
            };

var mailMessage = new MailMessage
{
   From = new MailAddress(_fromEmail),
   Subject = "Order Confirmation",
   Body = $@"
<h2>Order Confirmation</h2>
<p>Thank you for your purchase!</p>
<h3>Order Details:</h3>
<p>{orderDetails}</p>
<p>Thank you for shopping with us!</p>",
   IsBodyHtml = true
};

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
        
    }
}