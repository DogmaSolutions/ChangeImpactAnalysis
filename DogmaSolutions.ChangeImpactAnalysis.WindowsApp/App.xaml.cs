using System;
using System.Windows;
using DogmaSolutions.SlidingWindowLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DogmaSolutions.ChangeImpactAnalysis.WindowsApp
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            var builder = Host.CreateDefaultBuilder().
                ConfigureAppConfiguration(
                    (context, builder) =>
                    {
                        builder.AddJsonFile("appsettings.json", optional: false);
                        builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                    }).
                ConfigureLogging(
                    logging =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Debug);
                        logging.AddLog4Net();
                        logging.AddConsole();
                        logging.AddEventSourceLogger();
                    }).
                ConfigureServices(
                    (context, services) =>
                    {
                        services.AddSingleton<MainWindow>();
                        services.AddSingleton<ISlidingWindowLoggerProvider, SlidingWindowLoggerProvider>();
                        services.AddSingleton<ILoggerProvider>(
                            sp =>
                            {
                                return sp.GetRequiredService<ISlidingWindowLoggerProvider>();
                            });
                    });

            _host = builder.Build();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            await _host.StartAsync().ConfigureAwait(false);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }
#pragma warning restore VSTHRD100 // Avoid async void methods
    }
}
