using Microsoft.OpenApi;
using Microsoft.OpenApi;
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
                // Switch to ConfigureWebHostDefaults to support Controllers & Swagger
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        // 1. Add API Controllers
                        services.AddControllers();
                        // 3. Register your Background Worker and Services
                        services.AddHttpContextAccessor();
                        services.AddHttpClient();
                        services.AddScoped</*IBaseService, */BaseService>();
                        services.AddHostedService<PostInvoiceWorker>();
                        services.AddHttpClient<IInvoiceService, InvoiceService>();
                    });

                    // 4. Configure the HTTP Pipeline (Middleware)
                    webBuilder.Configure(app =>
                    {
                        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

                        // Enable Swagger UI in Development (or always, if you prefer)
                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI();
                        }

                        app.UseRouting();

                        //app.UseAuthentication();
                        //app.UseAuthorization();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }
}