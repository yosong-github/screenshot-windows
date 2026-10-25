using Pegline.Core.Abstractions;
using Pegline.Core.Domain;

namespace Pegline.Platform.Files;

public sealed class CaptureStore : ICaptureStore
{
    public CaptureStore()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        Folder = CaptureFolders.Resolve(local, pictures);
    }

    public string Folder { get; }

    public string Save(byte[] png, DateTimeOffset takenAt)
    {
        Directory.CreateDirectory(Folder);
        var index = 1;
        string path;
        do
        {
            path = Path.Combine(Folder, CaptureFileNames.Create(takenAt, index));
            index++;
        }
        while (File.Exists(path));

        File.WriteAllBytes(path, png);
        return path;
    }
}
