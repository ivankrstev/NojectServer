using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Configurations;
using NojectServer.Data;
using NojectServer.DependencyInjection;
using NojectServer.Hubs;
using NojectServer.Middlewares;
using NojectServer.OptionsSetup;

namespace NojectServer;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

        // Configure the EmailSettings and RouteOptions from the environment configuration
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        // Register the database context
        builder.Services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnection"))
        );

        // Add application services using the extension methods
        builder.Services.AddServices();

        // Add filter for verifying project access to the Tasks SignalR hub
            builder.Services.AddSignalR().AddHubOptions<TasksHub>(options =>
            {
                options.AddFilter<VerifyProjectAccessHub>();
            });

        // Add a controller and the API explorer
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            string[] origins = builder.Configuration["Cors:Origins"]?.Split(",") ?? Array.Empty<string>();
            builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder.AllowAnyHeader()
                           .AllowAnyMethod()
                           .WithOrigins(origins)
                           .AllowCredentials();
                }));

        // Add CORS services
        builder.Services.AddCors();
        // Register the configuration class for CORS
        builder.Services.ConfigureOptions<ConfigureCorsOptions>();
        // Configure the SwaggerGen options
        builder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
        // Configure the API behavior options
        builder.Services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        // Register the JWT bearer authentication scheme
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => new JwtBearerOptionsSetup().GetOptions(builder.Configuration, options));

            var app = builder.Build();

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
            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<SharedProjectsHub>("/SharedProjectsHub");
            app.MapHub<TasksHub>("/TasksHub");

            app.Run();
        }
    }
