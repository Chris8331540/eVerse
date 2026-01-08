using eVerse.Services;
using eVerse.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.ComponentModel;
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
        private static MainWindow? _currentInstance;

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

            _currentInstance = this;

            var projectionService = serviceProvider.GetRequiredService<ProjectionService>();

            DataContext = new MainWindowViewModel(serviceProvider, projectionService);

            // Inicializar DP acorde al estado inicial
            IsSidebarExpanded = _sidebarExpanded;
            
            // Subscribe to VM property changes so we can update sidebar selection when CurrentView changes
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;

                // Initialize sidebar selection according to initial CurrentView
                try
                {
                    UpdateSidebarSelection(vm.CurrentView);
                }
                catch { }
            }
        }

        internal static void RequestSidebarSelectionUpdate(object? currentView)
        {
            _currentInstance?.Dispatcher.Invoke(() => _currentInstance.UpdateSidebarSelection(currentView));
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

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentView) && sender is MainWindowViewModel vm)
            {
                // Ensure UI thread
                Dispatcher.Invoke(() => UpdateSidebarSelection(vm.CurrentView));
            }
        }

        private void UpdateSidebarSelection(object? currentView)
        {
            // Determine which sidebar item corresponds to the current view
            if (currentView is Views.CreateSongView)
            {
                SelectSidebarItem(CrearBorder);
            }
            else if (currentView is Views.SongListView)
            {
                SelectSidebarItem(ListaBorder);
            }
            else if (currentView is Views.EditSongListView)
            {
                SelectSidebarItem(EditListaBorder);
            }
            else if (currentView is Views.WebsocketConfigView)
            {
                SelectSidebarItem(WsConfigBorder);
            }
            else
            {
                // No matching view - clear selection
                if (_selectedItem != null)
                {
                    SidebarItemHelper.SetIsSelected(_selectedItem, false);
                    _selectedItem = null;
                }
            }
        }

        private void SelectSidebarItem(Border border)
        {
            if (border == null) return;

            if (_selectedItem != null && _selectedItem != border)
            {
                SidebarItemHelper.SetIsSelected(_selectedItem, false);
            }

            SidebarItemHelper.SetIsSelected(border, true);
            _selectedItem = border;
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