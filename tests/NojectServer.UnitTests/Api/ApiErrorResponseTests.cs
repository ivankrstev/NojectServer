using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NojectServer.OptionsSetup;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Api;

public sealed class ApiErrorResponseTests
{
    [Fact]
    public void CreateFailure_ReturnsProblemDetailsWithApplicationMetadata()
    {
        using ServiceProvider services = CreateServices();
        DefaultHttpContext httpContext = CreateHttpContext(
            services,
            pathBase: "/api",
            path: "/auth/login",
            traceIdentifier: "trace-123");
        var error = new ErrorDetails(
            "Login.InvalidCredentials",
            "The credentials are invalid.",
            StatusCodes.Status401Unauthorized);

        ObjectResult result = ApiErrorResponseFactory.CreateFailure(
            httpContext,
            error);

        ProblemDetails problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("The credentials are invalid.", problem.Detail);
        Assert.Equal("/api/auth/login", problem.Instance);
        Assert.Equal("Login.InvalidCredentials", problem.Extensions["code"]);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.Contains("application/problem+json", result.ContentTypes);
    }

    [Fact]
    public void CreateFailure_UsesActivityIdWhenAnActivityIsActive()
    {
        using ServiceProvider services = CreateServices();
        DefaultHttpContext httpContext = CreateHttpContext(
            services,
            path: "/auth/login",
            traceIdentifier: "http-trace");
        var error = new ErrorDetails(
            "Login.Failed",
            "Login failed.",
            StatusCodes.Status500InternalServerError);
        using var activity = new Activity("ApiErrorResponseTests");
        activity.Start();

        ObjectResult result = ApiErrorResponseFactory.CreateFailure(
            httpContext,
            error);

        ProblemDetails problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(activity.Id, problem.Extensions["traceId"]);
    }

    [Fact]
    public void CreateValidation_WithApplicationErrors_ReturnsAllErrorsAndMetadata()
    {
        using ServiceProvider services = CreateServices();
        DefaultHttpContext httpContext = CreateHttpContext(
            services,
            path: "/auth/register",
            traceIdentifier: "validation-trace");
        var error = new ErrorDetails(
            "Registration.InvalidInput",
            "The registration data is invalid.",
            StatusCodes.Status422UnprocessableEntity);
        IReadOnlyDictionary<string, string[]> validationErrors =
            new Dictionary<string, string[]>
            {
                ["Email"] = ["Email is required.", "Email is invalid."],
                ["Password"] = ["Password is required."]
            };

        ObjectResult result = ApiErrorResponseFactory.CreateValidation(
            httpContext,
            error,
            validationErrors);

        ValidationProblemDetails problem =
            Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problem.Status);
        Assert.Equal("The registration data is invalid.", problem.Title);
        Assert.Equal("/auth/register", problem.Instance);
        Assert.Equal("Registration.InvalidInput", problem.Extensions["code"]);
        Assert.Equal("validation-trace", problem.Extensions["traceId"]);
        Assert.Equal(
            ["Email is required.", "Email is invalid."],
            problem.Errors["Email"]);
        Assert.Equal(["Password is required."], problem.Errors["Password"]);
        Assert.Contains("application/problem+json", result.ContentTypes);
    }

    [Fact]
    public void CreateValidation_WithModelState_UsesStandardValidationErrorDetails()
    {
        using ServiceProvider services = CreateServices();
        DefaultHttpContext httpContext = CreateHttpContext(
            services,
            path: "/auth/register",
            traceIdentifier: "model-state-trace");
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required.");
        modelState.AddModelError("Password", "Password is required.");

        ObjectResult result = ApiErrorResponseFactory.CreateValidation(
            httpContext,
            modelState);

        ValidationProblemDetails problem =
            Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("One or more validation errors occurred.", problem.Title);
        Assert.Equal("Validation", problem.Extensions["code"]);
        Assert.Equal("model-state-trace", problem.Extensions["traceId"]);
        Assert.Equal(["Email is required."], problem.Errors["Email"]);
        Assert.Equal(["Password is required."], problem.Errors["Password"]);
    }

    [Fact]
    public void ResultFailure_MapsToFailureProblemDetails()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(
            services,
            path: "/auth/login",
            traceIdentifier: "failure-trace");
        Result result = Result.Failure(
            "Login.InvalidCredentials",
            "The credentials are invalid.",
            StatusCodes.Status401Unauthorized);

        ActionResult actionResult = result.ToActionResult(controller);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        Assert.Equal("Login.InvalidCredentials", problem.Extensions["code"]);
    }

    [Fact]
    public void ResultValidationFailure_MapsToValidationProblemDetails()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(
            services,
            path: "/auth/register");
        Result result = Result.ValidationFailure(
            new Dictionary<string, string[]>
            {
                ["Email"] = ["Email is required."],
                ["Password"] = ["Password is required."]
            });

        ActionResult actionResult = result.ToActionResult(controller);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ValidationProblemDetails problem =
            Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("Validation", problem.Extensions["code"]);
        Assert.Equal(["Email is required."], problem.Errors["Email"]);
        Assert.Equal(["Password is required."], problem.Errors["Password"]);
    }

    [Fact]
    public void ResultSuccess_WithCallback_UsesCallbackResult()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");
        var expected = new OkObjectResult("created");
        var callbackCallCount = 0;

        ActionResult result = Result.Success().ToActionResult(
            controller,
            () =>
            {
                callbackCallCount++;
                return expected;
            });

        Assert.Same(expected, result);
        Assert.Equal(1, callbackCallCount);
    }

    [Fact]
    public void ResultSuccess_WithoutPayload_MapsToNoContent()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        ActionResult result = Result.Success().ToActionResult(controller);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void ResultSuccess_WithoutPayload_MapsToSpecifiedStatusCode()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        ActionResult result = Result.Success().ToActionResult(
            controller,
            StatusCodes.Status201Created);

        StatusCodeResult statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
    }

    [Fact]
    public void GenericResultSuccess_MapsValueToOkResponse()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        ActionResult result = Result.Success("item-123").ToActionResult(controller);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("item-123", okResult.Value);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public void GenericResultSuccess_MapsValueToSpecifiedStatusCode()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        ActionResult result = Result.Success("item-123").ToActionResult(
            controller,
            StatusCodes.Status201Created);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Equal("item-123", objectResult.Value);
    }

    [Fact]
    public void GenericResultFailure_MapsToFailureProblemDetails()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(
            services,
            path: "/items/item-123");
        Result<string> result = Result.Failure<string>(
            "Items.NotFound",
            "The item was not found.",
            StatusCodes.Status404NotFound);

        ActionResult actionResult = result.ToActionResult(controller);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Items.NotFound", problem.Extensions["code"]);
    }

    [Fact]
    public void GenericResultValidationFailure_MapsToValidationProblemDetails()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");
        Result<string> result = Result.ValidationFailure<string>(
            new Dictionary<string, string[]>
            {
                ["Name"] = ["Name is required."]
            });

        ActionResult actionResult = result.ToActionResult(controller);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ValidationProblemDetails problem =
            Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal(["Name is required."], problem.Errors["Name"]);
    }

    [Fact]
    public void ApiBehaviorOptionsSetup_UsesCustomModelStateResponseFactory()
    {
        using ServiceProvider services = CreateServices();
        DefaultHttpContext httpContext = CreateHttpContext(
            services,
            path: "/auth/register",
            traceIdentifier: "api-behavior-trace");
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required.");
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);
        var options = new ApiBehaviorOptions();

        new ApiBehaviorOptionsSetup().Configure(options);

        IActionResult actionResult =
            options.InvalidModelStateResponseFactory(actionContext);
        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
        ValidationProblemDetails problem =
            Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("Validation", problem.Extensions["code"]);
        Assert.Equal("/auth/register", problem.Instance);
        Assert.Equal("api-behavior-trace", problem.Extensions["traceId"]);
        Assert.Equal(["Email is required."], problem.Errors["Email"]);
    }

    [Fact]
    public void ResultExtensions_WithUnsupportedNonGenericResult_Throws()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new UnsupportedResult().ToActionResult(controller));

        Assert.Equal(
            "Unsupported result type: UnsupportedResult.",
            exception.Message);
    }

    [Fact]
    public void ResultExtensions_WithUnsupportedGenericResult_Throws()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new UnsupportedGenericResult().ToActionResult(controller));

        Assert.Equal("Unknown generic result type.", exception.Message);
    }

    [Fact]
    public void ResultExtensions_RejectsNullArguments()
    {
        using ServiceProvider services = CreateServices();
        TestController controller = CreateController(services, "/items");

        Assert.Throws<ArgumentNullException>(() =>
            ResultExtensions.ToActionResult(
                null!,
                controller,
                () => controller.Ok()));
        Assert.Throws<ArgumentNullException>(() =>
            Result.Success().ToActionResult(
                null!,
                () => controller.Ok()));
        Assert.Throws<ArgumentNullException>(() =>
            Result.Success().ToActionResult(
                controller,
                null!));
        Assert.Throws<ArgumentNullException>(() =>
            ResultExtensions.ToActionResult<string>(
                null!,
                controller,
                _ => controller.Ok()));
        Assert.Throws<ArgumentNullException>(() =>
            Result.Success("value").ToActionResult(
                null!,
                _ => controller.Ok()));
        Assert.Throws<ArgumentNullException>(() =>
            Result.Success("value").ToActionResult(
                controller,
                null!));
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.AddProblemDetails();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(
        IServiceProvider services,
        string? pathBase = null,
        string? path = null,
        string? traceIdentifier = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };

        if (pathBase is not null)
        {
            httpContext.Request.PathBase = pathBase;
        }

        if (path is not null)
        {
            httpContext.Request.Path = path;
        }

        if (traceIdentifier is not null)
        {
            httpContext.TraceIdentifier = traceIdentifier;
        }

        return httpContext;
    }

    private static TestController CreateController(
        IServiceProvider services,
        string path,
        string? pathBase = null,
        string? traceIdentifier = null)
    {
        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(
                    services,
                    pathBase,
                    path,
                    traceIdentifier)
            }
        };
    }

    private sealed class TestController : ControllerBase;

    private sealed class UnsupportedResult : Result
    {
        public override bool IsSuccess => false;

        public override ErrorDetails? Error => null;
    }

    private sealed class UnsupportedGenericResult : Result<string>
    {
        public override bool IsSuccess => false;

        public override ErrorDetails? Error => null;
    }
}
