using eVerse.Data;
using eVerse.Services;
using eVerse.ViewModels;
using eVerse.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;

namespace eVerse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // Registrar DbContext
            services.AddDbContext<AppDbContext>();

            // Registrar servicios
            services.AddSingleton<SongService>();
            services.AddSingleton<SettingsService>();

            // Create and register kestrel websocket service instance
            var kestrelWs = new KestrelWebSocketService(port:5000, token: "secret-token");
            services.AddSingleton<IWebSocketService>(kestrelWs);

            // ProjectionSettings requiere SettingsService en constructor
            services.AddSingleton<ProjectionSettings>();
            // ProjectionService ahora requiere IWebSocketService
            services.AddSingleton<ProjectionService>();

            // Registrar ViewModels
            services.AddTransient<CreateSongViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<SongListViewModel>();
            services.AddTransient<WebsocketConfigViewModel>();

            // Registrar Views
            services.AddTransient<CreateSongView>();
            services.AddTransient<SongListView>();
            services.AddTransient<MainWindow>();
            services.AddTransient<EditSongListView>();
            services.AddTransient<WebsocketConfigView>();

            ServiceProvider = services.BuildServiceProvider();

            // Abrir la ventana principal
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }

    }

}
