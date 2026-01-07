using eVerse.Services;
using eVerse.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using eVerse.Animations;
using eVerse.Views;

namespace eVerse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private bool _sidebarExpanded = true;
        private Border _selectedItem;

        // Nueva DP para que XAML pueda enlazarse al estado del sidebar
        public static readonly DependencyProperty IsSidebarExpandedProperty =
            DependencyProperty.Register(nameof(IsSidebarExpanded), typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public bool IsSidebarExpanded
        {
            get => (bool)GetValue(IsSidebarExpandedProperty);
            set => SetValue(IsSidebarExpandedProperty, value);
        }

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            var projectionService = serviceProvider.GetRequiredService<ProjectionService>();

            DataContext = new MainWindowViewModel(serviceProvider, projectionService);

            // Inicializar DP acorde al estado inicial
            IsSidebarExpanded = _sidebarExpanded;
        }
        private void ToggleSidebar()
        {
            double from = _sidebarExpanded ? 220 : 60;
            double to = _sidebarExpanded ? 60 : 220;

            var anim = new GridLengthAnimation
            {
                From = new GridLength(from, GridUnitType.Pixel),
                To = new GridLength(to, GridUnitType.Pixel),
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase()
            };

            SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, anim);

            // Ocultar o mostrar textos de las opciones (incluida la opción de editar canciones)
            TextCrear.Visibility = _sidebarExpanded ? Visibility.Collapsed : Visibility.Visible;
            TextLista.Visibility = _sidebarExpanded ? Visibility.Collapsed : Visibility.Visible;
            EditTextLista.Visibility = _sidebarExpanded ? Visibility.Collapsed : Visibility.Visible;

            // Actualizar estado y DP enlazada
            _sidebarExpanded = !_sidebarExpanded;
            IsSidebarExpanded = _sidebarExpanded;
        }

        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleSidebar();
        }

        private void ToggleSidebarContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToggleSidebar();
        }

        private void SidebarItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b)
            {
                // Clear previous
                if (_selectedItem != null)
                {
                    SidebarItemHelper.SetIsSelected(_selectedItem, false);
                }

                // Set new
                SidebarItemHelper.SetIsSelected(b, true);
                _selectedItem = b;
            }
        }

        private void OpenNotebookMenu_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenBookWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.SelectedBook != null)
            {
                // After selecting book, persist is already done in the dialog.
                // Now instruct VM to load songs for the chosen book.
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.LoadSongsForBook(dialog.SelectedBook.Id);
                }
            }
        }

        private void CreateNotebookMenu_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateBookWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.CreatedBook != null)
            {
                System.Windows.MessageBox.Show($"Cuaderno '{dialog.CreatedBook.Title}' creado correctamente.",
                    "Cuadernos", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void TextsMenu_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextSettingsWindow
            {
                Owner = this
            };

            dialog.ShowDialog();
        }
    }
}