using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pegline.App.Imaging;

public static class ImageLoader
{
    private static ImageSource? _placeholder;

    public static ImageSource FromFile(string path, int decodeWidth)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            if (decodeWidth > 0)
            {
                image.DecodePixelWidth = decodeWidth;
            }

            image.UriSource = new Uri(Path.GetFullPath(path));
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return Placeholder();
        }
    }

    public static ImageSource FromFrame(Pegline.Core.Domain.FrozenMonitor frame)
    {
        var bitmap = BitmapSource.Create(
            frame.PixelWidth,
            frame.PixelHeight,
            96,
            96,
            PixelFormats.Bgr32,
            null,
            frame.Bgra32,
            frame.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private static ImageSource Placeholder()
    {
        if (_placeholder is not null)
        {
            return _placeholder;
        }

        var pixels = new byte[16 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 220;
            pixels[i + 1] = 220;
            pixels[i + 2] = 220;
            pixels[i + 3] = 255;
        }

        var bitmap = BitmapSource.Create(4, 4, 96, 96, PixelFormats.Bgra32, null, pixels, 16);
        bitmap.Freeze();
        _placeholder = bitmap;
        return bitmap;
    }
}
