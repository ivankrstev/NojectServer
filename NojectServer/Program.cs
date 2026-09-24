using Microsoft.EntityFrameworkCore;
using NojectServer.DependencyInjection;
using NojectServer.Modules.Identity;
using NojectServer.Modules.Identity.Infrastructure.Persistence;

namespace NojectServer;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAppOptions(builder.Configuration);

        // Feature modules own their services and are responsible for registering them.
        builder.Services.AddIdentityModule(builder.Configuration);

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
            IdentityDataContext database = scope.ServiceProvider.GetRequiredService<IdentityDataContext>();
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
