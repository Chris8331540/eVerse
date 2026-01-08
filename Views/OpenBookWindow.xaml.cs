using eVerse.Data;
using eVerse.Models;
using eVerse.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace eVerse.Views
{
    public partial class OpenBookWindow : Window
    {
        private readonly AppConfigService _appConfigService;
        
        public ObservableCollection<Book> Books { get; } = new();
        public Book? SelectedBook { get; private set; }

        public OpenBookWindow()
        {
            InitializeComponent();
            DataContext = this;
            _appConfigService = App.ServiceProvider.GetRequiredService<AppConfigService>();
            LoadBooks();
        }

        private void LoadBooks()
        {
            using var context = new AppDbContext();
            var storedBooks = context.Books
                .OrderBy(b => b.Title)
                .ToList();

            Books.Clear();
            foreach (var book in storedBooks)
            {
                Books.Add(book);
            }
        }

        private void BooksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpenButton.IsEnabled = BooksList.SelectedItem != null;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedBook = BooksList.SelectedItem as Book;
            if (SelectedBook != null)
            {
                // Persist selected book id into AppConfig using service
                _appConfigService.SetLastOpenedBookId(SelectedBook.Id);

                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
