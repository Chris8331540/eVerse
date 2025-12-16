using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using eVerse.Services;
using eVerse.Models;

namespace eVerse.ViewModels
{
    public class ProjectionSettings : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private bool _suppressSave = false;

        public ProjectionSettings(SettingsService settingsService)
        {
            _settingsService = settingsService;
            this.PropertyChanged += ProjectionSettings_PropertyChanged;
        }

        // Current song id for which settings should be persisted
        private int? _songId;
        public int? SongId
        {
            get => _songId;
            set
            {
                if (EqualityComparer<int?>.Default.Equals(_songId, value))
                    return;

                // Suppress saves while we update SongId and load existing settings
                _suppressSave = true;
                try
                {
                    SetProperty(ref _songId, value);
                    // Load settings for the new song id
                    LoadForSong(value);
                }
                finally
                {
                    _suppressSave = false;
                }
            }
        }

        private void LoadForSong(int? songId)
        {
            if (songId == null) return;

            var existing = _settingsService.GetBySongId(songId.Value);
            if (existing != null)
            {
                FontFamily = existing.FontFamily;
                FontSize = existing.FontSize;
                AutoFit = existing.AutoFit;
                UseFade = existing.UseFade;
                FadeMs = existing.FadeMs;
            }
            else
            {
                // If no settings exist for this song, keep current ProjectionSettings values (which may be defaults)
                // Do not overwrite DB here; save will occur only when the user changes a setting property explicitly.
            }
        }

        private void ProjectionSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_suppressSave) return;

            // don't attempt to save until we have a SongId to persist to
            if (SongId == null) return;

            // Persist current settings for the current song
            var model = new Setting
            {
                SongId = SongId.Value,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                AutoFit = this.AutoFit,
                UseFade = this.UseFade,
                FadeMs = this.FadeMs
            };

            try
            {
                _settingsService.UpsertSetting(model);
            }
            catch
            {
                // swallow errors to avoid crashing UI on save failures
            }
        }

        // Fuente (familia de fuentes)
        private string fontFamily = "Segoe UI";
        public string FontFamily
        {
            get => fontFamily;
            set => SetProperty(ref fontFamily, value);
        }

        // Tamaño de fuente en puntos
        private double fontSize = 56;
        public double FontSize
        {
            get => fontSize;
            set => SetProperty(ref fontSize, value);
        }

        // ¿Usar ajuste automático (fit) del texto dentro de la pantalla?
        private bool autoFit = true; // default enabled
        public bool AutoFit
        {
            get => autoFit;
            set => SetProperty(ref autoFit, value);
        }

        // ¿Usar fade al cambiar?
        private bool useFade = true;
        public bool UseFade
        {
            get => useFade;
            set => SetProperty(ref useFade, value);
        }

        // Duración del fade en ms
        private int fadeMs = 350;
        public int FadeMs
        {
            get => fadeMs;
            set => SetProperty(ref fadeMs, value);
        }
    }
}
