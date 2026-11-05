using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using GlassDesk.Settings;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var window = new SettingsWindow();
        foreach (string name in new[] { "LauncherSectionHeading", "LauncherRowsSlider", "LauncherSettingsStatus" })
        {
            var control = (FrameworkElement)typeof(SettingsWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            Console.WriteLine(name);
            for (FrameworkElement? element = control; element != null; element = element.Parent as FrameworkElement)
            {
                Console.WriteLine("  " + element.GetType().Name + " " + element.Name);
                if (element is Panel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        if (child is FrameworkElement item)
                        {
                            Console.WriteLine("    " + item.GetType().Name + " " + item.Name + " " + (item is TextBlock text ? text.Text : ""));
                        }
                    }
                }
                if (element.Name == "SettingsScroll") break;
            }
        }
        window.Close();
    }
}
