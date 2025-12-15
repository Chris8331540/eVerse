using eVerse.ViewModels;
using eVerse.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace eVerse.Services
{
    public class ProjectionService
    {
        private ProjectionWindow? _window;
        private readonly ProjectionSettings _settings;

        public ProjectionService(ProjectionSettings settings)
        {
            _settings = settings;

            // Ensure projection window is closed when application exits
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Exit += (_, __) =>
                {
                    try { CloseProjection(); } catch { }
                };
            }
        }

        // Muestra (o actualiza) la proyección con un nuevo texto
        public void ShowProjection(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                EnsureWindowCreated();
                _window!.SetProjectedText(text);
                _window!.Activate(); // lleva al frente
            });
        }

        // Cierra la ventana si está abierta
        public void CloseProjection()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (_window != null)
                {
                    _window.Close();
                    _window = null;
                }
            });
        }

        private void EnsureWindowCreated()
        {
            if (_window != null) return;

            _window = new ProjectionWindow(_settings);

            // Set owner so it closes with main window
            try
            {
                if (System.Windows.Application.Current?.MainWindow != null)
                    _window.Owner = System.Windows.Application.Current.MainWindow;
            }
            catch { }

            // --- Obtener pantalla de proyección ---
            var screens = System.Windows.Forms.Screen.AllScreens;
            var target = screens.Length > 1 ? screens[1] : screens[0];

            // --- Convertir a coordenadas WPF correctas (DPI aware) ---
            var source = System.Windows.PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
            double dpiX = 1.0, dpiY = 1.0;

            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            var bounds = target.Bounds;

            // --- Aplicar dimensiones corrigiendo DPI ---
            _window.WindowStartupLocation = WindowStartupLocation.Manual;
            _window.Left = bounds.Left / dpiX;
            _window.Top = bounds.Top / dpiY;
            _window.Width = bounds.Width / dpiX;
            _window.Height = bounds.Height / dpiY;

            // --- Estilo ventana ---
            _window.WindowStyle = WindowStyle.None;
            _window.ResizeMode = ResizeMode.NoResize;
            _window.Topmost = true;

            // --- MOSTRAR ventana ---
            _window.Show();

            // 🔥 Mantener referencia válida
            _window.Activate();

            // 🔥 Cuando la ventana se cierre manualmente o por ESC
            _window.Closed += (_, __) =>
            {
                _window = null;  // permite recrearla
            };
        }

    }
}
