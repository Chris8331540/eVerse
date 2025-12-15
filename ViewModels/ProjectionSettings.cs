using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace eVerse.ViewModels
{
    public class ProjectionSettings : ObservableObject
    {
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
