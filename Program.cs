
using ArtworkCore.Services;
using ArtworkCore.Services.DBconnect;

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

            PostgresSQL_Connection db_connect = new PostgresSQL_Connection(builder.Configuration["DBconnect"]);

            builder.Services.AddControllers();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            builder.Services.AddScoped<IPostgresSQL_Connection>(container =>
            {
                return db_connect;
            });

            builder.Services.AddScoped<ArtworkCore.Services.EmailService>();

            builder.Services.AddSingleton<EmailService>();

            var app = builder.Build();

            app.UseCors("AllowAllOrigins");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
