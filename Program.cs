using Microsoft.OpenApi.Models;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Repositories;
using PaycBillingWorker.Services;
using PaycBillingWorker.Workers;

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
                    // vital: Ensures IIS integration catches exceptions and logs them
                    webBuilder.CaptureStartupErrors(true);

                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddEndpointsApiExplorer();
                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PayC Billing API", Version = "v1" });
                        });

                        services.AddHttpContextAccessor();
                        services.AddHttpClient();

                        // Register Services
                        services.AddScoped<IBaseService, BaseService>();

                        services.AddHostedService<PostInvoiceWorker>();
                        services.AddHostedService<UpdateMeterReadingWorker>();
                        services.AddHttpClient<IInvoiceService, InvoiceService>();
                        services.AddScoped<IMeterReadingService, MeterReadingService>();
                        services.AddScoped<IConsumerService, ConsumerService>();
                        services.AddScoped<MeterReadingRepository>();
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseDeveloperExceptionPage();

                        // FIX: Move Swagger OUTSIDE the IsDevelopment check
                        // so it works in IIS (Production)
                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayC Billing API v1");
                            // Optional: Makes Swagger load at the root URL (localhost/)
                            // c.RoutePrefix = string.Empty; 
                        });

                        // Standard Middleware
                        app.UseRouting();

                        // Add Authorization if you have it, otherwise endpoints might fail if they expect it
                        // app.UseAuthorization(); 

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }
}