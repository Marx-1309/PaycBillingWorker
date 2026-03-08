using Microsoft.OpenApi.Models;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Repositories;
using PaycBillingWorker.Services;
using PaycBillingWorker.Workers;
using Serilog;

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

                .UseSerilog((ctx, lc) =>
                    lc.ReadFrom.Configuration(ctx.Configuration)
                      .WriteTo.Console())

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true);

                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        services.AddEndpointsApiExplorer();

                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo
                            {
                                Title = "PayC Billing API",
                                Version = "v1"
                            });
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

                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayC Billing API v1");
                        });

                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }
}