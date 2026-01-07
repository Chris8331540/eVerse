using eVerse.Data;
using eVerse.Models;
using System;
using System.Windows;

namespace eVerse.Views
{
    public partial class CreateBookWindow : Window
    {
        public Book? CreatedBook { get; private set; }

        public CreateBookWindow()
        {
            InitializeComponent();
        }

        private void BookNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CreateButton.IsEnabled = !string.IsNullOrWhiteSpace(BookNameTextBox.Text);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var title = BookNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                CreateButton.IsEnabled = false;
                return;
            }

            using var context = new AppDbContext();
            var book = new Book { Title = title };
            context.Books.Add(book);
            context.SaveChanges();

            CreatedBook = book;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
