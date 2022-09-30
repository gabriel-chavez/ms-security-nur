
using Security.Infrastructure.Security;
using Serilog;

namespace Inventario.WebApi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                IWebHostEnvironment env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                SecurityInitializer securityInitializer = scope.ServiceProvider.GetRequiredService<SecurityInitializer>();

                ConfigureLogs(env);

                string contentRootPath = env.ContentRootPath;
                var permissionJsonFilePath = contentRootPath + "/DataFiles/permissions.json";
                var securityInitializationJsonFilePath = contentRootPath + "/DataFiles/initializer.json";

                await securityInitializer.Initialize(permissionJsonFilePath, securityInitializationJsonFilePath);

            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseSerilog()
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseStartup<Startup>();
               });


        private static void ConfigureLogs(IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                var log = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }
        }
    }
}