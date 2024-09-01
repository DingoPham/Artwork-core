
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
                options.AddPolicy("Dingo_Pham2003",
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
            var app = builder.Build();

            app.UseCors("Dingo_Pham2003");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
