using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiDesktopClient.Models;

public enum MessageRole
{
    User,
    Assistant,
    System
}

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    [ObservableProperty]
    private MessageRole _role;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _rawContent = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private int _inputTokens;

    [ObservableProperty]
    private int _outputTokens;

    [ObservableProperty]
    private int _totalTokens;

    [ObservableProperty]
    private double _responseTimeMs;

    [ObservableProperty]
    private double _cost;

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Attachment> _attachments = [];

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isError;
}
