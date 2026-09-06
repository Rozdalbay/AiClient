using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

public partial class Chat : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    [ObservableProperty]
    private string _title = "New Chat";

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = [];

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime _updatedAt = DateTime.Now;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private string? _projectId;

    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _modelName = string.Empty;

    partial void OnTitleChanged(string value) => UpdatedAt = DateTime.Now;
}
