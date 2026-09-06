using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

public partial class Attachment : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private long _fileSize;

    [ObservableProperty]
    private string _mimeType = string.Empty;

    [ObservableProperty]
    private string _localPath = string.Empty;

    public string FileSizeDisplay => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
