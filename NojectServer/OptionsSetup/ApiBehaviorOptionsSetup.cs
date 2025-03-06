using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NojectServer.OptionsSetup;

public class ApiBehaviorOptionsSetup : IConfigureOptions<ApiBehaviorOptions>
{
    public void Configure(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = actionContext =>
        {
            if (actionContext.ModelState.ErrorCount <= 0) return new BadRequestResult();

            var errorMessages = actionContext.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToArray();
            return new BadRequestObjectResult(new
            {
                error = "Validation Failed",
                message = errorMessages[0]
            });
        };
    }
}