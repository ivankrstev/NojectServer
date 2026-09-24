using Microsoft.Extensions.Options;
using NojectServer.Configurations.Email;

namespace NojectServer.UnitTests.Configuration;

public sealed class EmailOptionsValidatorTests
{
    private readonly EmailOptionsValidator _validator = new();

    [Theory]
    [InlineData(1, false, "http://localhost:3000")]
    [InlineData(65535, true, "https://client.example.com")]
    public void Validate_WithValidOptions_ReturnsSuccess(
        int port,
        bool useSsl,
        string clientUrl)
    {
        EmailOptions options = CreateValidOptions();
        options.Port = port;
        options.UseSsl = useSsl;
        options.ClientUrl = clientUrl;

        ValidateOptionsResult result = _validator.Validate(
            Options.DefaultName,
            options);

        Assert.True(result.Succeeded);
        Assert.Null(result.Failures);
    }

    [Fact]
    public void Validate_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _validator.Validate(null, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankEmailId_ReturnsRequiredError(
        string? emailId)
    {
        EmailOptions options = CreateValidOptions();
        options.EmailId = emailId!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:EmailId is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("sender@")]
    [InlineData("Sender Name <sender@example.com>")]
    [InlineData("sender@example.com;other@example.com")]
    public void Validate_WithMalformedEmailId_ReturnsFormatError(string emailId)
    {
        EmailOptions options = CreateValidOptions();
        options.EmailId = emailId;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:EmailId must be a valid email address.");
    }

    [Theory]
    [InlineData(" sender@example.com ")]
    [InlineData("SENDER@EXAMPLE.COM")]
    public void Validate_WithNormalizedEmailId_ReturnsSuccess(string emailId)
    {
        EmailOptions options = CreateValidOptions();
        options.EmailId = emailId;

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankName_ReturnsRequiredError(string? name)
    {
        EmailOptions options = CreateValidOptions();
        options.Name = name!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:Name is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankHost_ReturnsRequiredError(string? host)
    {
        EmailOptions options = CreateValidOptions();
        options.Host = host!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:Host is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankUserName_ReturnsRequiredError(string? userName)
    {
        EmailOptions options = CreateValidOptions();
        options.UserName = userName!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:UserName is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankPassword_ReturnsRequiredError(string? password)
    {
        EmailOptions options = CreateValidOptions();
        options.Password = password!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:Password is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_WithPortOutsideAllowedRange_ReturnsRangeError(int port)
    {
        EmailOptions options = CreateValidOptions();
        options.Port = port;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:Port must be between 1 and 65535.");
    }

    [Fact]
    public void Validate_WithUnconfiguredUseSsl_ReturnsConfigurationError()
    {
        EmailOptions options = CreateValidOptions();
        options.UseSsl = null;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:UseSsl must be configured.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Validate_WithBlankClientUrl_ReturnsRequiredError(string? clientUrl)
    {
        EmailOptions options = CreateValidOptions();
        options.ClientUrl = clientUrl!;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:ClientUrl is required.");
    }

    [Theory]
    [InlineData("client.example.com")]
    [InlineData("ftp://client.example.com")]
    [InlineData("mailto:client@example.com")]
    public void Validate_WithNonHttpClientUrl_ReturnsFormatError(string clientUrl)
    {
        EmailOptions options = CreateValidOptions();
        options.ClientUrl = clientUrl;

        ValidateOptionsResult result = _validator.Validate(null, options);

        AssertFailures(
            result,
            $"{EmailOptions.SectionName}:ClientUrl must be an absolute HTTP or HTTPS URL.");
    }

    [Fact]
    public void Validate_WithMultipleInvalidValues_ReturnsAllErrorsInOrder()
    {
        var options = new EmailOptions
        {
            EmailId = "not-an-email",
            Name = " ",
            Host = "",
            UserName = "\t",
            Password = "\r\n",
            Port = 0,
            UseSsl = null,
            ClientUrl = "ftp://client.example.com"
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(
            [
                $"{EmailOptions.SectionName}:EmailId must be a valid email address.",
                $"{EmailOptions.SectionName}:Name is required.",
                $"{EmailOptions.SectionName}:Host is required.",
                $"{EmailOptions.SectionName}:UserName is required.",
                $"{EmailOptions.SectionName}:Password is required.",
                $"{EmailOptions.SectionName}:Port must be between 1 and 65535.",
                $"{EmailOptions.SectionName}:UseSsl must be configured.",
                $"{EmailOptions.SectionName}:ClientUrl must be an absolute HTTP or HTTPS URL."
            ],
            result.Failures);
    }

    private static EmailOptions CreateValidOptions()
    {
        return new EmailOptions
        {
            EmailId = "sender@example.com",
            Name = "NojectServer",
            Host = "smtp.example.com",
            UserName = "smtp-user",
            Password = "smtp-password",
            Port = 587,
            UseSsl = true,
            ClientUrl = "https://client.example.com"
        };
    }

    private static void AssertFailures(
        ValidateOptionsResult result,
        string expectedError)
    {
        Assert.True(result.Failed);
        Assert.Equal(expectedError, Assert.Single(result.Failures!));
    }
}
