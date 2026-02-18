using Microsoft.OpenApi.Models;
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
                        services.AddHostedService<PostInvoiceWorker>(); // Be careful with this in IIS (see notes below)
                        services.AddHttpClient<IInvoiceService, InvoiceService>();
                        services.AddScoped<IMeterReadingService, MeterReadingService>();
                        services.AddScoped<IConsumerService, ConsumerService>();
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