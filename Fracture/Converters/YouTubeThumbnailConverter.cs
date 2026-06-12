using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Fracture.Converters;

public class YouTubeThumbnailConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? videoId = value as string;
        if (string.IsNullOrWhiteSpace(videoId) || videoId.Length != 11)
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri($"https://img.youtube.com/vi/{videoId}/mqdefault.jpg", UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
