using eVerse.ViewModels;
using System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace eVerse.Views
{
    /// <summary>
    /// Lógica de interacción para ProjectionWindow.xaml
    /// </summary>
    public partial class ProjectionWindow : Window
    {
        private readonly ProjectionSettings _settings;
        private PropertyChangedEventHandler _handler;


        public ProjectionWindow(ProjectionSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            _handler = (_, __) => ApplySettings();
            _settings.PropertyChanged += _handler;

            // Apply current settings immediately so FontFamily/FontSize are set
            ApplySettings();
        }

        private void ApplySettings()
        {
            // Aplicar familia y tamaño (Viewbox escala, pero dejamos referencia)
            ProjectedText.FontFamily = new System.Windows.Media.FontFamily(_settings.FontFamily);
            ProjectedText.FontSize = _settings.FontSize;

            // Ajustar comportamiento del Viewbox según AutoFit
            if (_settings.AutoFit)
            {
                RootViewbox.Stretch = Stretch.Uniform;
                RootViewbox.StretchDirection = StretchDirection.Both; // allow scaling up/down
            }
            else
            {
                RootViewbox.Stretch = Stretch.Uniform;
                RootViewbox.StretchDirection = StretchDirection.DownOnly; // prevent scaling up, only down
            }
        }

        // Llamado por ProjectionService para actualizar texto
        public void SetProjectedText(string text)
        {
            // Ensure the latest settings are applied before updating text
            ApplySettings();

            if (_settings.UseFade)
                DoFadeChange(text, _settings.FadeMs);
            else
                ProjectedText.Text = text;
        }

        private void DoFadeChange(string newText, int ms)
        {
            // Fade out, change text, fade in
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(ms / 2.0));
            fadeOut.Completed += (_, __) =>
            {
                ProjectedText.Text = newText;
                var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(ms / 2.0));
                ProjectedText.BeginAnimation(OpacityProperty, fadeIn);
            };
            ProjectedText.BeginAnimation(OpacityProperty, fadeOut);
        }

        // Cerrar con ESC
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Debug.WriteLine("VENTANA CERRADA — OnClosed ejecutado");
            if (_handler != null)
                _settings.PropertyChanged -= _handler;
        }
    }
}
