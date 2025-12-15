using System.Windows;
using System.Windows.Controls;

namespace eVerse
{
 public static class SidebarItemHelper
 {
 public static readonly DependencyProperty IsSelectedProperty =
 DependencyProperty.RegisterAttached(
 "IsSelected",
 typeof(bool),
 typeof(SidebarItemHelper),
 new PropertyMetadata(false));

 public static bool GetIsSelected(DependencyObject obj) => (bool)obj.GetValue(IsSelectedProperty);
 public static void SetIsSelected(DependencyObject obj, bool value) => obj.SetValue(IsSelectedProperty, value);
 }
}
