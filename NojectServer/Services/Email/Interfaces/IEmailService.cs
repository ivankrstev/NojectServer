using NojectServer.Models;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Email.Interfaces;

public interface IEmailService
{
    Task SendVerificationLinkAsync(User user);

    Task SendResetPasswordLinkAsync(User user);
}
