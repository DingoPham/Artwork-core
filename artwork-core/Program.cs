using ArtworkCore.Class;
using ArtworkCore.FilterAttribute;
using ArtworkCore.Services;
using ArtworkCore.Services.DBconnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data.Common;
using System.Text;

namespace ArtworkCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("local.setting.json",
                                    optional: true,
                                    reloadOnChange: true);
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                    };
                });

            builder.Services.AddControllers();

            var list_cors = builder.Configuration["ListHost"].Split(";");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                builder =>
                {
                    builder.WithOrigins(list_cors)
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            PostgresSQL_Connection db_connect = new PostgresSQL_Connection(builder.Configuration["DBconnect"]);
            builder.Services.AddScoped<IPostgresSQL_Connection>(container =>
            {
                return db_connect;
            });

            builder.Services.AddScoped<ArtworkCore.Services.EmailService>();
            builder.Services.AddScoped<ArtworkCore.Class.AgeCaculator>();
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddSingleton<AgeCaculator>();
            builder.Services.AddScoped<JwtService>();
            builder.Services.AddScoped(container =>
            {
                return new CustomFilter();
            });

            var app = builder.Build();

            app.UseCors("AllowSpecificOrigin");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
