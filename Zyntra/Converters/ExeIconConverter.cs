using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Zyntra.Converters;

public class ExeIconConverter : IValueConverter
{
    private static readonly Dictionary<string, ImageSource?> _cache = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        if (_cache.TryGetValue(path, out var cached))
            return cached;

        try
        {
            if (!File.Exists(path))
                return null;

            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".ico")
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 64;
                bitmap.EndInit();
                bitmap.Freeze();
                _cache[path] = bitmap;
                return bitmap;
            }

            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon == null) return null;

            var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bitmapSource.Freeze();
            _cache[path] = bitmapSource;
            return bitmapSource;
        }
        catch
        {
            _cache[path] = null;
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ExeHasIconToVisibilityConverter : IValueConverter
{
    private readonly ExeIconConverter _inner = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var icon = _inner.Convert(value, targetType, parameter, culture);
        return icon != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ExeHasIconToInverseVisibilityConverter : IValueConverter
{
    private readonly ExeIconConverter _inner = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var icon = _inner.Convert(value, targetType, parameter, culture);
        return icon != null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
