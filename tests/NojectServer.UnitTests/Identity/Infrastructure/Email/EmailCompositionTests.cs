using Microsoft.Extensions.Options;
using MimeKit;
using NojectServer.Configurations.Email;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Infrastructure.Email;

namespace NojectServer.UnitTests.Identity.Infrastructure.Email;

public sealed class EmailCompositionTests
{
    [Fact]
    public async Task SendVerificationLinkAsync_ComposesMultipartMessageWithEncodedLink()
    {
        const string email = "alice+tag@example.com";
        const string fullName = "Alice <Admin> & Co";
        const string token = "token/with?&";
        const string verificationLink =
            "https://app.example.test/verify-email?"
            + "email=alice%2Btag%40example.com&token=token%2Fwith%3F%26";

        var sender = new RecordingEmailSender();
        EmailService service = CreateService(
            sender,
            clientUrl: "https://app.example.test///");

        await service.SendVerificationLinkAsync(
            email,
            fullName,
            token,
            TestContext.Current.CancellationToken);

        Assert.NotNull(sender.Message);
        MimeMessage message = sender.Message!;
        Assert.Equal("Email Verification", message.Subject);
        AssertMailbox(message.From, "Identity Service", "no-reply@example.test");
        AssertMailbox(message.To, fullName, email);

        MultipartAlternative body = Assert.IsType<MultipartAlternative>(
            message.Body);
        Assert.Equal(2, body.Count);

        TextPart plainPart = Assert.IsType<TextPart>(body[0]);
        Assert.Equal(
            $"Dear {fullName},{Environment.NewLine}{Environment.NewLine}"
            + "Please use the following link to verify your email:"
            + $"{Environment.NewLine}{verificationLink}",
            plainPart.Text);

        TextPart htmlPart = Assert.IsType<TextPart>(body[1]);
        Assert.Contains(
            "<p>Dear Alice &lt;Admin&gt; &amp; Co,</p>",
            htmlPart.Text);
        Assert.Contains(
            $"<a href=\"{verificationLink.Replace("&", "&amp;", StringComparison.Ordinal)}\">"
            + "verify your email</a>",
            htmlPart.Text);
        Assert.DoesNotContain("<Admin>", htmlPart.Text);
    }

    [Fact]
    public async Task SendResetPasswordLinkAsync_ComposesResetMessageUsingConfiguredClientUrl()
    {
        const string email = "bob@example.test";
        const string fullName = "Bob";
        const string token = "reset-token";
        const string resetLink =
            "https://client.example.test/reset-password?"
            + "email=bob%40example.test&token=reset-token";

        var sender = new RecordingEmailSender();
        EmailService service = CreateService(
            sender,
            clientUrl: "https://client.example.test");

        await service.SendResetPasswordLinkAsync(
            email,
            fullName,
            token,
            TestContext.Current.CancellationToken);

        Assert.NotNull(sender.Message);
        MimeMessage message = sender.Message!;
        Assert.Equal("Password Reset Request", message.Subject);
        AssertMailbox(message.From, "Identity Service", "no-reply@example.test");
        AssertMailbox(message.To, fullName, email);

        MultipartAlternative body = Assert.IsType<MultipartAlternative>(
            message.Body);
        TextPart plainPart = Assert.IsType<TextPart>(body[0]);
        TextPart htmlPart = Assert.IsType<TextPart>(body[1]);

        Assert.Contains(resetLink, plainPart.Text);
        Assert.Contains(
            $"<a href=\"{resetLink.Replace("&", "&amp;", StringComparison.Ordinal)}\">"
            + "reset your password</a>",
            htmlPart.Text);
        Assert.Contains("Please use the following link to reset your password:",
            plainPart.Text);
    }

    [Fact]
    public async Task SendVerificationLinkAsync_ForwardsCancellationTokenToSender()
    {
        var sender = new RecordingEmailSender();
        EmailService service = CreateService(sender);

        await service.SendVerificationLinkAsync(
            "alice@example.test",
            "Alice",
            "verification-token",
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TestContext.Current.CancellationToken,
            sender.CancellationToken);
    }

    [Fact]
    public async Task SendVerificationLinkAsync_PropagatesSenderFailure()
    {
        var sender = new RecordingEmailSender
        {
            Failure = new InvalidOperationException("delivery failed")
        };
        EmailService service = CreateService(sender);

        InvalidOperationException exception = await Assert.ThrowsAsync<
            InvalidOperationException>(() => service.SendVerificationLinkAsync(
                "alice@example.test",
                "Alice",
                "verification-token",
                TestContext.Current.CancellationToken));

        Assert.Equal("delivery failed", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void SendVerificationLinkAsync_WithBlankRequiredInput_ThrowsArgumentException(
        string blankValue)
    {
        var sender = new RecordingEmailSender();
        EmailService service = CreateService(sender);

        Assert.Throws<ArgumentException>(() =>
        {
            _ = service.SendVerificationLinkAsync(
                blankValue,
                "Alice",
                "verification-token",
                TestContext.Current.CancellationToken);
        });
        Assert.Null(sender.Message);
    }

    [Fact]
    public void SendVerificationLinkAsync_WithNullRequiredInput_ThrowsArgumentNullException()
    {
        var sender = new RecordingEmailSender();
        EmailService service = CreateService(sender);

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = service.SendVerificationLinkAsync(
                null!,
                "Alice",
                "verification-token",
                TestContext.Current.CancellationToken);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = service.SendVerificationLinkAsync(
                "alice@example.test",
                null!,
                "verification-token",
                TestContext.Current.CancellationToken);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = service.SendVerificationLinkAsync(
                "alice@example.test",
                "Alice",
                null!,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public void SendResetPasswordLinkAsync_WithBlankToken_ThrowsArgumentException()
    {
        var sender = new RecordingEmailSender();
        EmailService service = CreateService(sender);

        Assert.Throws<ArgumentException>(() =>
        {
            _ = service.SendResetPasswordLinkAsync(
                "bob@example.test",
                "Bob",
                " ",
                TestContext.Current.CancellationToken);
        });
        Assert.Null(sender.Message);
    }

    private static EmailService CreateService(
        RecordingEmailSender sender,
        string clientUrl = "https://app.example.test")
    {
        var options = Options.Create(
            new EmailOptions
            {
                EmailId = "no-reply@example.test",
                Name = "Identity Service",
                ClientUrl = clientUrl
            });

        return new EmailService(options, sender);
    }

    private static void AssertMailbox(
        InternetAddressList addresses,
        string expectedName,
        string expectedAddress)
    {
        MailboxAddress mailbox = Assert.Single(addresses.Mailboxes);

        Assert.Equal(expectedName, mailbox.Name);
        Assert.Equal(expectedAddress, mailbox.Address);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public MimeMessage? Message { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Exception? Failure { get; init; }

        public Task SendAsync(
            MimeMessage message,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            CancellationToken = cancellationToken;

            return Failure is null
                ? Task.CompletedTask
                : Task.FromException(Failure);
        }
    }
}
