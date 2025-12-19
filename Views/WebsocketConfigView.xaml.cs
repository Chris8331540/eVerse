using eVerse.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace eVerse.Views
{
 public partial class WebsocketConfigView : System.Windows.Controls.UserControl
 {
 public WebsocketConfigView() : this(App.ServiceProvider.GetRequiredService<WebsocketConfigViewModel>())
 {
 }

 public WebsocketConfigView(WebsocketConfigViewModel viewModel)
 {
 InitializeComponent();
 DataContext = viewModel;
 }
 }
}
