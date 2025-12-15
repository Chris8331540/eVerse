using eVerse.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace eVerse.Views
{
    /// <summary>
    /// Lógica de interacción para CreateSongView.xaml
    /// </summary>
    public partial class CreateSongView : System.Windows.Controls.UserControl
    {
        //chained constructor
        //this is because WPF requires a parameterless constructor for XAML instantiation
        //but we want to use dependency injection to get the ViewModel
        //so create a parameterless constructor that calls the main constructor with the ViewModel from the service provider
        public CreateSongView() : this(App.ServiceProvider.GetRequiredService<CreateSongViewModel>())
        {
        }

        public CreateSongView(CreateSongViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel; // asigna el ViewModel inyectado
            viewModel.ShowMessage = msg => System.Windows.MessageBox.Show(msg, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}