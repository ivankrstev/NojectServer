using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.OptionsSetup;

public class ApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
{
    /// <summary>
    /// Configures the API behavior options to use the custom error response factory for validation errors.
    /// </summary>
    public void Configure(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = actionContext =>
            ApiErrorResponseFactory.CreateValidation(
                actionContext.HttpContext,
                actionContext.ModelState);
    }
}
