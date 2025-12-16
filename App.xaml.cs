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

            // ProjectionSettings requiere SettingsService en constructor
            services.AddSingleton<ProjectionSettings>();
            services.AddSingleton<ProjectionService>();

            // Registrar ViewModels
            services.AddTransient<CreateSongViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<SongListViewModel>();

            // Registrar Views
            services.AddTransient<CreateSongView>();
            services.AddTransient<SongListView>();
            services.AddTransient<MainWindow>();
            services.AddTransient<EditSongListView>();
            

            ServiceProvider = services.BuildServiceProvider();

            // Abrir la ventana principal
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }

    }

}
