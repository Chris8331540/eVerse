using CommunityToolkit.Mvvm.ComponentModel;

namespace eVerse.ViewModels
{
    public partial class VerseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string text = string.Empty;
    }
}
