using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.DependencyInjection;
using NojectServer.Modules.Identity;

namespace NojectServer;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddAppOptions(builder.Configuration);

        // Feature modules own their services and authentication/authorization policies.
        builder.Services.AddIdentityModule();

        builder.Services.AddApi();

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Environment.IsProduction())
        {
            // Keep automatic migrations limited to Production, including exclusion of Staging.
            using IServiceScope scope = app.Services.CreateScope();
            DataContext database = scope.ServiceProvider.GetRequiredService<DataContext>();
            database.Database.Migrate();
        }

        // Shared request pipeline; exception handling wraps the downstream API middleware.
        app.UseExceptionHandler();

        app.UseRouting();
        app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
