using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.DependencyInjection;
using NojectServer.Hubs;
using NojectServer.Middlewares;
using NojectServer.Modules.Identity;
using NojectServer.OptionsSetup;
using NojectServer.Shared.Application.Persistence;
using NojectServer.Shared.Infrastructure.Persistence;

namespace NojectServer;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string dbConnectionString = builder.Configuration.GetConnectionString("DBConnection") ?? throw new InvalidOperationException("Database connection string is not configured.");
        // Register the database context
        builder.Services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(dbConnectionString)
                .UseSnakeCaseNamingConvention()
        );

        // Register the shared unit of work for coordinating multi-repository saves
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Configure application options using the extension method
        builder.Services.AddAppOptions(builder.Configuration);

        builder.Services.AddIdentityModule();

        // Add filter for verifying project access to the Tasks SignalR hub
        // builder.Services.AddSignalR().AddHubOptions<TasksHub>(options =>
        // {
        //     options.AddFilter<VerifyProjectAccessHub>();
        // });

        // Add a controller and the API explorer
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        // Register Swagger generator for API documentation and testing
        builder.Services.AddSwaggerGen();

        // Register the global exception handler
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails(); // Recommended for structured error responses

        // Add CORS services
        builder.Services.AddCors();
        // Register the configuration class for CORS
        builder.Services.ConfigureOptions<ConfigureCorsOptions>();
        // Configure the SwaggerGen options
        builder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
        // Configure the API behavior options
        builder.Services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        if (app.Environment.IsProduction())
        {
            // Make sure the database is set up, on production start
            app.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();
        }

        // Use the CORS policy
        app.UseCors("CorsPolicy");
        // Use the global exception handler
        app.UseExceptionHandler();
        // Use HTTPS redirection
        //app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<SharedProjectsHub>("/SharedProjectsHub");
        app.MapHub<TasksHub>("/TasksHub");

        app.Run();
    }
}
