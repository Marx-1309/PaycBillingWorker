using Microsoft.OpenApi;
using Microsoft.OpenApi.Models; // This requires Swashbuckle.AspNetCore package
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Services;

namespace PaycBillingWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddEndpointsApiExplorer();

                        // This block uses OpenApiInfo from Microsoft.OpenApi.Models
                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PayC Billing API", Version = "v1" });
                        });

                        services.AddHttpContextAccessor();
                        services.AddHttpClient();

                        // Register Services
                        services.AddScoped<IBaseService, BaseService>();
                        //services.AddHostedService<PostInvoiceWorker>();
                        services.AddHttpClient<IInvoiceService, InvoiceService>();
                        services.AddScoped<IMeterReadingService, MeterReadingService>();
                        services.AddScoped<IConsumerService, ConsumerService>();
                    });

                    webBuilder.Configure(app =>
                    {
                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI(); // Defaults to /swagger/v1/swagger.json
                        }

                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }
}