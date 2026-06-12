using System.Windows;

namespace Fracture.Services;

public static class ThemeService
{
    public static readonly (string Name, string Hex)[] AccentPresets =
    [
        ("Blue", "#FF709BFF"),
        ("Purple", "#FF9B6DFF"),
        ("Pink", "#FFFF6DB5"),
        ("Red", "#FFFF6B6B"),
        ("Orange", "#FFFF9F43"),
        ("Yellow", "#FFFFD93D"),
        ("Green", "#FF6BCB77"),
        ("Teal", "#FF4ECDC4"),
        ("Cyan", "#FF54A0FF"),
    ];

    public static void ApplyAccentColor(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            var app = Application.Current;
            if (app == null) return;

            // Update the theme dictionary
            foreach (ResourceDictionary dict in app.Resources.MergedDictionaries)
            {
                if (dict.Contains("AccentColor"))
                {
                    dict["AccentColor"] = color;
                    dict["AccentBrush"] = brush;
                    return;
                }
            }

            // Fallback: update in top-level resources
            app.Resources["AccentColor"] = color;
            app.Resources["AccentBrush"] = brush;
        }
        catch { }
    }
}
