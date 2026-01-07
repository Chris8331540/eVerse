using eVerse.Data;
using eVerse.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace eVerse.Views
{
    public partial class OpenBookWindow : Window
    {
        public ObservableCollection<Book> Books { get; } = new();
        public Book? SelectedBook { get; private set; }

        public OpenBookWindow()
        {
            InitializeComponent();
            DataContext = this;
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
                // Persist selected book id into AppConfig (single record)
                using var ctx = new AppDbContext();
                var cfg = ctx.AppConfigs.FirstOrDefault();
                if (cfg == null)
                {
                    cfg = new AppConfig { LastOpenedBook = SelectedBook.Id };
                    ctx.AppConfigs.Add(cfg);
                }
                else
                {
                    cfg.LastOpenedBook = SelectedBook.Id;
                    ctx.AppConfigs.Update(cfg);
                }
                ctx.SaveChanges();

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
