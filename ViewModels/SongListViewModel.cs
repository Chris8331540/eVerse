using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eVerse.Models;
using eVerse.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace eVerse.ViewModels
{
    public partial class SongListViewModel : ObservableObject
    {
        private readonly ProjectionService _projectionService;
        private readonly ProjectionSettings _projectionSettings;

        // Exponer settings para binding
        public ProjectionSettings ProjectionSettings => _projectionSettings;

        private readonly SongService _songService;
        private readonly IServiceProvider _serviceProvider;

        private ICollectionView? _songsView;
        public ICollectionView? SongsView
        {
            get => _songsView!;
            private set => SetProperty(ref _songsView, value);
        }

        //Propiedad observable para buscar segun numero de cancion
        [ObservableProperty]
        private string searchText = string.Empty;

        // Lista observable de canciones
        [ObservableProperty]
        private ObservableCollection<Song> songs = new();

        // Canción seleccionada en la UI
        [ObservableProperty]
        private Song? selectedSong;

        // Versos de la canción seleccionada (colección observable para editar visualmente)
        private ObservableCollection<Verse>? _selectedSongVerses;
        public ObservableCollection<Verse>? SelectedSongVerses
        {
            get => _selectedSongVerses;
            private set => SetProperty(ref _selectedSongVerses, value);
        }

        // Lista de fuentes disponibles (simple ejemplo)
        public ObservableCollection<string> AvailableFonts { get; } = new ObservableCollection<string>
    {
        "Segoe UI", "Arial", "Times New Roman", "Calibri", "Verdana"
    };

        public SongListViewModel(SongService songService, ProjectionService projectionService, ProjectionSettings settings, IServiceProvider serviceProvider)
        {
            _songService = songService;
            _projectionService = projectionService;
            _projectionSettings = settings;
            _serviceProvider = serviceProvider;

            LoadSongs();
        }

        partial void OnSelectedSongChanged(Song? oldValue, Song? newValue)
        {
            // Update ProjectionSettings SongId so per-song settings are loaded/saved
            _projectionSettings.SongId = newValue?.Id;

            // Populate observable verses collection for editing in the UI
            if (newValue == null)
            {
                SelectedSongVerses = null;
            }
            else
            {
                var ordered = newValue.Verses?.OrderBy(v => v.Order).ToList() ?? new List<Verse>();
                SelectedSongVerses = new ObservableCollection<Verse>(ordered);
            }

            // Force command CanExecute re-evaluation so AddVerse button updates enabled state
            try
            {
                if (AddVerseCommand is IRelayCommand relay)
                    relay.NotifyCanExecuteChanged();
            }
            catch { }
        }

        // Cargar canciones desde la BD
        [RelayCommand]
        private void LoadSongs()
        {
            Songs = new ObservableCollection<Song>(_songService.GetAllSongs());
            SongsView = CollectionViewSource.GetDefaultView(Songs);
            SongsView.Filter = SongsFilter;
        }

        // Eliminar canción seleccionada
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void DeleteSong()
        {
            if (SelectedSong == null)
                return;

            _songService.DeleteSong(SelectedSong.Id);
            LoadSongs();
        }

        private bool CanDelete()
        {
            return SelectedSong != null;
        }

        // Añadir estrofa visualmente a la canción seleccionada
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void AddVerse()
        {
            if (SelectedSong == null) return;
            if (SelectedSongVerses == null) SelectedSongVerses = new ObservableCollection<Verse>();
            SelectedSongVerses.Add(new Verse { Text = string.Empty, Order = (SelectedSongVerses.Count +1) });
        }

        // Eliminar estrofa visualmente (no persiste hasta guardar)
        [RelayCommand]
        private void RemoveVerse(Verse verse)
        {
            if (SelectedSongVerses == null || verse == null) return;
            SelectedSongVerses.Remove(verse);
            // Recompute orders
            for (int i =0; i < SelectedSongVerses.Count; i++)
                SelectedSongVerses[i].Order = i +1;
        }

        [RelayCommand]
        private void ShowVerse(string verseText)
        {
            if (string.IsNullOrWhiteSpace(verseText)) return;
            _projectionService.ShowProjection(verseText);
        }

        // También puedes añadir comando para cerrar proyección
        [RelayCommand]
        private void CloseProjection()
        {
            _projectionService.CloseProjection();
        }

        // Nuevo: comando para guardar cambios en una canción. Recibe un objeto Song que contiene el título y las estrofas (Verses).
        [RelayCommand]
        private void SaveSong(Song? song)
        {
            if (song == null) return;

            // Si usamos SelectedSongVerses, sincronizar antes de guardar
            if (SelectedSongVerses != null)
            {
                song.Verses = SelectedSongVerses.Select(v => new Verse
                {
                    Id = v.Id,
                    Text = v.Text,
                    Order = v.Order,
                    SongId = song.Id
                }).ToList();
            }

            // Usa el servicio para actualizar la entidad en la base de datos
            _songService.UpdateSong(song);

            // Recargar la lista para reflejar cambios (opcional)
            LoadSongs();

            // Mantener la selección en la canción guardada, si existe
            var kept = Songs.FirstOrDefault(s => s.Id == song.Id);
            if (kept != null)
                SelectedSong = kept;

            // Informar al usuario
            System.Windows.MessageBox.Show("Cambios guardados correctamente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private bool CanEdit()
        {
            return SelectedSong != null;
        }
        private bool SongsFilter(object obj)
        {
            if (obj is not Song song)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true; // Sin filtro muestra todo

            // Parsear SearchText para obtener los números
            var separators = new char[] { ',', ' ' };
            var parts = SearchText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var numbers = new HashSet<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int n))
                    numbers.Add(n);
            }

            // Filtrar por SongNumber
            return numbers.Contains(song.SongNumber);
        }

        partial void OnSearchTextChanged(string oldValue, string newValue)
        {
            SongsView?.Refresh();
        }

    }
}
