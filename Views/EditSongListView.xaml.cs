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
    /// Lógica de interacción para EditSongListView.xaml
    /// </summary>
    public partial class EditSongListView : System.Windows.Controls.UserControl
    {
        public EditSongListView()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider.GetRequiredService<SongListViewModel>();
        }
    }
}
