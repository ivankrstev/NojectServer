using NojectServer.Models;

namespace NojectServer.Services.Email
{
    public interface IEmailService
    {
        void SendVerificationLink(User user);
    }
}