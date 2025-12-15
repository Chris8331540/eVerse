using eVerse.Models;
using eVerse.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Wpf.Ui.Input;

namespace eVerse.ViewModels
{
    public class CreateSongViewModel : INotifyPropertyChanged
    {
        private readonly SongService _songService;
        public ObservableCollection<VerseViewModel> Verses { get; set; }

        private string songName = string.Empty;
        private int songNumber;
        public string SongName
        {
            get => songName;
            set { songName = value; OnPropertyChanged(); }
        }
        public int SongNumber
        {
            get => songNumber;
            set { songNumber = value; OnPropertyChanged(); }
        }
        public ICommand AddVerseCommand { get; }
        public ICommand SaveSongCommand { get; }
        public ICommand RemoveVerseCommand { get; }

        public Action<string>? ShowMessage { get; set; }

        public CreateSongViewModel(SongService songService)
        {
            _songService = songService;
            // Por defecto agregamos 1 bloque de estrofa
            Verses = new ObservableCollection<VerseViewModel>
            {
            new VerseViewModel(new Verse()) // Bloque inicial
            };

            AddVerseCommand = new RelayCommand(AddVerse);
            RemoveVerseCommand = new RelayCommand<VerseViewModel>(RemoveVerse);
            SaveSongCommand = new RelayCommand(SaveSong);
        }

        private void AddVerse()
        {
            Verses.Add(new VerseViewModel(new Verse()));
        }

        private void SaveSong()
        {
            //first validate and create the song object
            if (string.IsNullOrWhiteSpace(SongName)) { 
                ShowMessage?.Invoke("El título de la canción no puede estar vacío.");
                return;
            }
            var song = new Song
            {
                Title = this.SongName,
                SongNumber = this.SongNumber,
                Verses = this.Verses
                .Select((vm, index) => new Verse
                {
                    Text = vm.Text,
                    Order = index + 1
                })
                .ToList()
            };
            _songService.CreateSong(song);


            //then save it using your data access layer
        }

        public void LoadFromSong(Song song)
        {
            if (song == null)
                return;

            // Asignar propiedades principales
            SongName = song.Title;
            SongNumber = song.SongNumber;

            // Limpiar estrofas anteriores si existían
            Verses.Clear();

            // Insertar las estrofas cargadas desde la BD
            foreach (var verse in song.Verses.OrderBy(v => v.Order))
            {
                Verses.Add(new VerseViewModel(new Verse())
                {
                    Text = verse.Text
                });
            }

            // Si por algún motivo no hubiera estrofas, creamos al menos 1 vacía
            if (Verses.Count == 0)
                AddVerse();
        }


        private void RemoveVerse(VerseViewModel verse)
        {
            if (verse != null)
                Verses.Remove(verse);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // RelayCommand básico
    public class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool>? canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T)parameter!) ?? true;

        public void Execute(object? parameter) => _execute((T)parameter!);
    }

}
