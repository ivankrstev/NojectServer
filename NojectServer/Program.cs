using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NojectServer.Data;
using NojectServer.Hubs;
using NojectServer.Middlewares;
using NojectServer.OptionsSetup;
using NojectServer.Services.Email;

namespace NojectServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<VerifyProjectOwnership>();
            builder.Services.AddScoped<VerifyProjectAccess>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSignalR().AddHubOptions<TasksHub>(options =>
            {
                options.AddFilter<VerifyProjectAccessHub>();
            });
            builder.Services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DBConnection"))
            );
            builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder.AllowAnyHeader()
                           .AllowAnyMethod()
                           .WithOrigins("http://localhost:3000")
                           .AllowCredentials();
                }));

            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    if (actionContext.ModelState.ErrorCount > 0)
                    {
                        var errorMessages = actionContext.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToArray();
                        return new BadRequestObjectResult(new { error = "Validation Failed", message = errorMessages[0] });
                    }
                    else
                    {
                        return new BadRequestResult();
                    }
                };
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options => new JwtBearerOptionsSetup().GetOptions(builder.Configuration, options));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
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
}