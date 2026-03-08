using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using PaycBillingWorker.Interfaces;
using PaycBillingWorker.Repositories;
using PaycBillingWorker.Services;
using PaycBillingWorker.Workers;
using PaycBillingWorker.Models;
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
                    webBuilder.ConfigureServices((context, services) =>
                    {
                        services.AddControllers();
                        services.AddEndpointsApiExplorer();

                        // Bind Scheduler Settings for Hot-Reloading
                        services.Configure<SchedulerSettings>("MeterSchedulerSettings",
                            context.Configuration.GetSection("MeterSchedulerSettings"));
                        services.Configure<SchedulerSettings>("InvoiceSchedulerSettings",
                            context.Configuration.GetSection("InvoiceSchedulerSettings"));

                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PayC Billing API", Version = "v1" });
                        });

                        services.AddCors(o =>
                        {
                            o.AddPolicy("AllowAll", a => a.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
                        });

                        services.AddHttpContextAccessor();
                        services.AddHttpClient();

                        services.AddScoped<IBaseService, BaseService>();
                        services.AddHostedService<PostInvoiceWorker>();
                        services.AddHostedService<UpdateMeterReadingWorker>();

                        services.AddHttpClient<IInvoiceService, InvoiceService>();
                        services.AddScoped<IMeterReadingService, MeterReadingService>();
                        services.AddScoped<IConsumerService, ConsumerService>();
                        services.AddSingleton<IWorkerHealthService, WorkerHealthService>();
                        services.AddScoped<MeterReadingRepository>();
                        services.AddHealthChecks();
                    });

                    webBuilder.Configure(app =>
                    {
                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

                        app.UseSwagger();
                        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayC Billing API v1"));
                        app.UseRouting();
                        app.UseCors("AllowAll");
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapHealthChecks("/healthz");
                        });
                    });
                });
    }
}