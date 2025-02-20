using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NojectServer.Models;

namespace NojectServer.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly SmtpClient _smtp;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _smtp = new();
            _smtp.Connect(_config.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
            _smtp.Authenticate(_config.GetSection("EmailUsername").Value, _config.GetSection("EmailPassword").Value);
        }

        public void SendVerificationLink(User user)
        {
            MimeMessage email = new();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("EmailFrom").Value));
            email.To.Add(MailboxAddress.Parse(user.Email));
            email.Subject = "Email Verification";
            string verificationLink = $"{_config.GetSection("EmailClientUrl").Value}/verify-email?email={user.Email}&token={user.VerificationToken}";
            TextPart textPart = new("plain")
            {
                Text = $"Dear {user.FullName},\n Please click on the following link to verify your email: {verificationLink}"
            };
            TextPart htmlPart = new("html")
            {
                Text = $"<p style=\"font-size:16px;color:#333;margin-bottom:10px;\">Dear {user.FullName},</p><p style=\"font-size:14px;color:#555;margin-bottom:20px;\">Please click on the following link to verify your email: </p><a style=\"display:inline-block;padding:10px 20px;background-color:#337ab7;color:#fff;text-decoration:none;border-radius:4px;font-size:14px;\" href=\"{verificationLink}\">Verify Email</a>"
            };
            Multipart multipart = new("alternative")
            {
                textPart,
                htmlPart
            };
            email.Body = multipart;

            _smtp.Send(email);
            _smtp.Disconnect(true);
        }

        public void SendResetPasswordLink(User user)
        {
            MimeMessage email = new();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("EmailFrom").Value));
            email.To.Add(MailboxAddress.Parse(user.Email));
            email.Subject = "Password Reset Request";
            string resetPasswordLink = $"{_config.GetSection("EmailClientUrl").Value}/reset-password?token={user.PasswordResetToken}";
            TextPart textPart = new("plain")
            {
                Text = $"Dear {user.FullName},\n Please click on the following link to reset your password: {resetPasswordLink}"
            };
            TextPart htmlPart = new("html")
            {
                Text = $"<p style=\"font-size:16px;color:#333;margin-bottom:10px;\">Dear {user.FullName},</p><p style=\"font-size:14px;color:#555;margin-bottom:20px;\">Please click on the following link to reset your password:</p><a style=\"display:inline-block;padding:10px 20px;background-color:#337ab7;color:#fff;text-decoration:none;border-radius:4px;font-size:14px;\" href=\"{resetPasswordLink}\">Reset Password</a>"
            };
            Multipart multipart = new("alternative")
            {
                textPart,
                htmlPart
            };
            email.Body = multipart;

            _smtp.Send(email);
            _smtp.Disconnect(true);
        }
    }
}